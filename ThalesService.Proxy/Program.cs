using System.Net.Sockets;
using System.Text;
using System.Diagnostics;
using System.Threading;
using System.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);
// Ensure console logging is enabled
builder.Logging.AddConsole();

builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.PropertyNameCaseInsensitive = true;
});

// Ensure the HSM port used by the proxy is propagated to the hosted HSM service
var configuredHsmPort = Environment.GetEnvironmentVariable("HSM_PORT") ?? Environment.GetEnvironmentVariable("THALES_SERVICE_PORT") ?? "50000";
Environment.SetEnvironmentVariable("THALES_SERVICE_PORT", configuredHsmPort);

// Start the Thales TCP HSM inside the proxy process so it starts/stops with the proxy
builder.Services.AddHostedService<ThalesService.ThalesTcpService>();

// Configure listen URLs from env or default — must be applied before Build()
var urls = Environment.GetEnvironmentVariable("PROXY_URLS") ?? Environment.GetEnvironmentVariable("ASPNETCORE_URLS") ?? "http://localhost:54879";
builder.WebHost.UseUrls(urls);
Console.WriteLine("ThalesService.Proxy will listen on: " + urls);

var app = builder.Build();

// Global exception hooks for diagnostics
AppDomain.CurrentDomain.UnhandledException += (s, e) =>
{
    Console.Error.WriteLine("UnhandledException: " + (e.ExceptionObject as Exception)?.ToString());
};
TaskScheduler.UnobservedTaskException += (s, e) =>
{
    Console.Error.WriteLine("UnobservedTaskException: " + e.Exception.ToString());
};

app.MapPost("/api/hsm/command", async (HttpContext ctx) =>
{
    var logger = ctx.RequestServices.GetService<ILoggerFactory>()?.CreateLogger("ThalesService.Proxy");
    var swTotal = Stopwatch.StartNew();
    try
    {
        // Read raw request body for diagnostics (allow re-reading)
        ctx.Request.EnableBuffering();
        string rawBody = string.Empty;
        try
        {
            ctx.Request.Body.Position = 0;
            using var sr = new StreamReader(ctx.Request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
            rawBody = await sr.ReadToEndAsync();
            ctx.Request.Body.Position = 0;
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, "Failed to read raw request body for diagnostics");
        }

        logger?.LogInformation("Incoming /api/hsm/command from {remote} Content: {body}", ctx.Connection.RemoteIpAddress?.ToString() ?? "local", rawBody);

        CommandRequest payload = null;
        try
        {
            payload = await ctx.Request.ReadFromJsonAsync<CommandRequest>();
        }
        catch (System.Text.Json.JsonException jex)
        {
            logger?.LogWarning(jex, "JSON parse failed for incoming payload, attempting best-effort extraction from raw body");
            try
            {
                // best-effort: remove escape slashes and quotes then extract "Command" value after ':'
                var cleaned = (rawBody ?? string.Empty).Replace("\\", "").Replace("\"", "");
                var idx = cleaned.IndexOf("Command", System.StringComparison.OrdinalIgnoreCase);
                if (idx >= 0)
                {
                    var after = cleaned.Substring(idx + "Command".Length);
                    var colon = after.IndexOf(':');
                    if (colon >= 0)
                    {
                        var val = after.Substring(colon + 1).Trim();
                        // trim braces, commas and whitespace
                        val = val.Trim().TrimStart('{').Trim().TrimEnd('}').Trim().Trim(',').Trim();
                        // take up to first whitespace or comma
                        var sep = val.IndexOfAny(new char[] { ' ', ',', '\r', '\n' });
                        if (sep > 0) val = val.Substring(0, sep);
                        val = val.Trim().Trim('"').Trim();
                        if (!string.IsNullOrEmpty(val)) payload = new CommandRequest(val);
                    }
                }
            }
            catch (System.Exception ex2)
            {
                logger?.LogWarning(ex2, "Best-effort extraction failed");
            }
        }

        if (payload == null || string.IsNullOrEmpty(payload.Command))
        {
            logger?.LogInformation("BadRequest: missing or empty command payload");
            return Results.BadRequest(new { error = "missing command" });
        }

        var hsmHost = Environment.GetEnvironmentVariable("HSM_HOST") ?? "localhost";
        var portStr = Environment.GetEnvironmentVariable("HSM_PORT") ?? Environment.GetEnvironmentVariable("THALES_SERVICE_PORT");
        if (!int.TryParse(portStr, out var hsmPort)) hsmPort = 50000;
        var timeoutMs = 30000; // increased timeout to reduce intermittent proxy timeouts

        logger?.LogInformation("Proxying to HSM {host}:{port} with timeout {ms}ms. Command len={len}", hsmHost, hsmPort, timeoutMs, payload.Command?.Length ?? 0);

        using var tcp = new TcpClient();
        var connectSw = Stopwatch.StartNew();
        try
        {
            var connectTask = tcp.ConnectAsync(hsmHost, hsmPort);
            var t = await Task.WhenAny(connectTask, Task.Delay(timeoutMs));
            connectSw.Stop();
            if (t != connectTask || !tcp.Connected)
            {
                logger?.LogWarning("TCP connect to {host}:{port} timed out after {ms}ms", hsmHost, hsmPort, timeoutMs);
                return Results.Json(new { error = "bad_gateway", detail = "TCP connect timed out", host = hsmHost, port = hsmPort }, statusCode: StatusCodes.Status502BadGateway);
            }
            logger?.LogInformation("TCP connected to {host}:{port} in {ms}ms", hsmHost, hsmPort, connectSw.ElapsedMilliseconds);
        }
        catch (SocketException sex)
        {
            logger?.LogError(sex, "SocketException connecting to {host}:{port}", hsmHost, hsmPort);
            return Results.Json(new { error = "bad_gateway", detail = sex.Message, socketError = sex.SocketErrorCode.ToString(), host = hsmHost, port = hsmPort }, statusCode: StatusCodes.Status502BadGateway);
        }

        using var stream = tcp.GetStream();
        // Ensure Thales framing: 4-char header is expected by the TCP service.
        // If the caller already provided a 4-digit header (e.g. tests that preframe), don't add one.
        var originalCommand = payload.Command ?? string.Empty;
        var cmd = originalCommand;
        if (cmd.Length < 4 || !char.IsDigit(cmd[0]) || !char.IsDigit(cmd[1]) || !char.IsDigit(cmd[2]) || !char.IsDigit(cmd[3]))
        {
            cmd = "0000" + cmd;
        }
        var outBytes = Encoding.ASCII.GetBytes(cmd + "\r\n");
        var writeSw = Stopwatch.StartNew();
        var writeCts = new CancellationTokenSource(timeoutMs);
        try
        {
            await stream.WriteAsync(outBytes, 0, outBytes.Length, writeCts.Token);
            writeSw.Stop();
            logger?.LogInformation("Wrote {bytes} bytes to {host}:{port} in {ms}ms", outBytes.Length, hsmHost, hsmPort, writeSw.ElapsedMilliseconds);
        }
        catch (OperationCanceledException)
        {
            logger?.LogWarning("Write to {host}:{port} timed out", hsmHost, hsmPort);
            return Results.Json(new { error = "write_timeout", host = hsmHost, port = hsmPort }, statusCode: StatusCodes.Status504GatewayTimeout);
        }

        var buf = new byte[8192];
        int read = 0;
        var readSw = Stopwatch.StartNew();
        try
        {
            // Perform an async read with cooperative timeouts and short retry window to
            // tolerate small processing races between the TCP host and the proxy.
            using var readCts = new CancellationTokenSource(timeoutMs);
            const int perAttemptDelayMs = 50;

            while (!readCts.IsCancellationRequested)
            {
                try
                {
                    var readTask = stream.ReadAsync(buf.AsMemory(0, buf.Length), readCts.Token).AsTask();
                    var completed = await Task.WhenAny(readTask, Task.Delay(perAttemptDelayMs, readCts.Token));
                    if (completed == readTask)
                    {
                        read = await readTask;
                        // if we got data, break and handle it
                        if (read > 0)
                        {
                            break;
                        }
                        // read==0: remote may have not written yet or closed; wait a short while and retry
                        await Task.Delay(perAttemptDelayMs, CancellationToken.None);
                        continue;
                    }
                }
                catch (OperationCanceledException) when (readCts.IsCancellationRequested)
                {
                    break;
                }

                // continue looping until the overall timeout expires (readCts)
            }

            readSw.Stop();
            logger?.LogInformation("Read {bytes} bytes from {host}:{port} in {ms}ms", read, hsmHost, hsmPort, readSw.ElapsedMilliseconds);
        }
        catch (Exception ex) when (ex is IOException || ex is OperationCanceledException)
        {
            logger?.LogWarning(ex, "Read from {host}:{port} timed out or failed", hsmHost, hsmPort);
            return Results.Json(new { error = "read_timeout", host = hsmHost, port = hsmPort }, statusCode: StatusCodes.Status504GatewayTimeout);
        }

        if (read <= 0)
        {
            logger?.LogWarning("No data received from {host}:{port} after retries", hsmHost, hsmPort);
            return Results.Json(new { error = "no_data", host = hsmHost, port = hsmPort }, statusCode: StatusCodes.Status504GatewayTimeout);
        }

        var resp = Encoding.ASCII.GetString(buf, 0, read).Trim();
        // Prefix the original 2-char command code so responses mirror Thales-style "<CMD><RC>..." format
        var responseWithCommand = (originalCommand.Length >= 2 ? originalCommand.Substring(0, 2) : string.Empty) + resp;
        logger?.LogInformation("Proxied command to {host}:{port}; response len={len} content={resp}", hsmHost, hsmPort, resp.Length, resp);
        swTotal.Stop();
        logger?.LogInformation("Total proxy time: {ms}ms", swTotal.ElapsedMilliseconds);
        return Results.Ok(new { response = responseWithCommand });
    }
    catch (Exception ex)
    {
        logger?.LogError(ex, "Unhandled exception while proxying command");
        return Results.Problem(detail: ex.Message);
    }
});

try
{
    app.Run();
}
catch (Exception ex)
{
    var logger = app.Services.GetService<ILoggerFactory>()?.CreateLogger("ThalesService.Proxy");
    logger?.LogCritical(ex, "Proxy terminated with exception");
    Console.Error.WriteLine("Proxy terminated: " + ex);
    // keep console open briefly for diagnostics
    Thread.Sleep(2000);
    throw;
}

internal record CommandRequest(string Command);
