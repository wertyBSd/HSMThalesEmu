using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HostCommands;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ThalesCore;
using ThalesCore.HostCommands;
using ThalesCore.Message;

namespace ThalesService
{
    public class ThalesTcpService : BackgroundService
    {
        private readonly ILogger<ThalesTcpService> _logger;
        private TcpListener _listener;
        private readonly int _port;

        public ThalesTcpService(ILogger<ThalesTcpService> logger)
        {
            _logger = logger;
            var env = Environment.GetEnvironmentVariable("THALES_SERVICE_PORT");
            if (!int.TryParse(env, out _port))
                _port = 1500;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("ThalesTcpService starting, listening on port {port}", _port);

            // initialize Thales environment
            var thales = new ThalesCore.ThalesMain();
            string[] candidates = new[] {
                Path.Combine("..", "ThalesCore", "ThalesParameters.xml"),
                Path.Combine("..", "..", "ThalesCore", "ThalesParameters.xml"),
                Path.Combine("..", "..", "..", "ThalesCore", "ThalesParameters.xml") };
            string paramFile = candidates.FirstOrDefault(File.Exists) ?? Path.Combine("ThalesCore", "ThalesParameters.xml");
            try { thales.StartUpWithoutTCP(paramFile); }
            catch { _logger.LogWarning("Could not load Thales parameters from {file}", paramFile); }

            // initialize storage and seed test data if missing
            try
            {
                var store = ThalesCore.Storage.StoreFactory.CreateFromEnvironment();
                await ThalesCore.Storage.Migrations.EnsureInitializedAsync(store);
                await ThalesCore.Storage.SeedData.EnsureSeedAsync(store);
                _logger.LogInformation("Key store initialized and seed data ensured.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to initialize or seed key store");
            }

            _listener = new TcpListener(IPAddress.Loopback, _port);
            _listener.Start();

            var explorer = new HostCommands.CommandExplorer();

            while (!stoppingToken.IsCancellationRequested)
            {
                TcpClient client = null;
                try
                {
                    client = await _listener.AcceptTcpClientAsync(stoppingToken);
                    _ = HandleClientAsync(client, explorer, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Listener exception");
                    client?.Close();
                }
            }

            _listener.Stop();
        }

        private async Task HandleClientAsync(TcpClient client, HostCommands.CommandExplorer explorer, CancellationToken ct)
        {
            using var stream = client.GetStream();
            var buffer = new byte[8192];
            try
            {
                // Read incoming request fully. Some clients send large payloads;
                // loop until no more data is available for a short grace period.
                using var ms = new MemoryStream();
                var read = 0;
                // initial read (respect cancellation)
                read = await stream.ReadAsync(buffer, ct);
                if (read <= 0) return;
                ms.Write(buffer, 0, read);

                // attempt to drain remaining available data with short timeouts
                try
                {
                    while (stream.DataAvailable)
                    {
                        read = await stream.ReadAsync(buffer, ct);
                        if (read <= 0) break;
                        ms.Write(buffer, 0, read);
                    }
                }
                catch (OperationCanceledException) { }

                var reqBytes = ms.ToArray();

                var msg = new ThalesCore.Message.Message(reqBytes);
                _logger.LogInformation("Received: {req}", msg.MessageData);

                int headerLen = 4;
                try { headerLen = Convert.ToInt32(Resources.GetResource(Resources.HEADER_LENGTH)); } catch { headerLen = 4; }

                // If headerLen looks invalid for this message, treat message as having no header
                if (headerLen < 0 || headerLen > msg.MessageData.Length - 2)
                    headerLen = 0;

                string commandCode;
                if (msg.MessageData.Length >= headerLen + 2)
                {
                    if (headerLen > 0)
                    {
                        msg.AdvanceIndex(headerLen);
                    }
                    commandCode = msg.GetSubstring(2);
                    msg.AdvanceIndex(2);
                }
                else
                {
                    commandCode = msg.MessageData.Length >= 2 ? msg.MessageData.Substring(0, 2) : "";
                }

                var cc = explorer.GetLoadedCommand(commandCode);
                if (cc == null)
                {
                    var resp = "91"; // unknown
                    var respBytes = Encoding.ASCII.GetBytes(resp);
                    await stream.WriteAsync(respBytes, ct);
                    return;
                }

                var hostCmd = (AHostCommand)Activator.CreateInstance(cc.DeclaringType);
                hostCmd.AcceptMessage(msg);
                var response = hostCmd.ConstructResponse();
                var outBytes = Encoding.ASCII.GetBytes(response.MessageData);
                try
                {
                    _logger.LogInformation("Writing {len} bytes back to client", outBytes.Length);
                    await stream.WriteAsync(outBytes, ct);
                    _logger.LogInformation("Write complete, wrote {len} bytes", outBytes.Length);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error writing response to client");
                    throw;
                }
                hostCmd.Terminate();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Client handling error");
            }
            finally
            {
                client.Close();
            }
        }
    }
}
