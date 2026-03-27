using System;
using System.Diagnostics;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace ThalesService.IntegrationTests
{
    [TestFixture]
    public class SetHSMDelayTests
    {
        [Test]
        public async Task SetHSMDelay_AppliesConfiguredDelayToSubsequentResponses()
        {
            var api = Environment.GetEnvironmentVariable("HSM_API_URL");

            // choose a modest delay so CI stays fast but measurable
            const int configuredDelayMs = 250;
            const int allowedSlackMs = 400; // allow jitter and scheduling delays

            if (!string.IsNullOrEmpty(api))
            {
                // Run the timing test via the HTTP->TCP proxy
                using var client = new System.Net.Http.HttpClient();
                client.Timeout = TimeSpan.FromSeconds(30);

                // attempt via proxy first, but fallback to direct TCP if proxy fails
                bool proxySucceeded = false;
                try
                {
                    // send LG to set the delay (proxy will add framing if needed)
                    var setBody = new { Command = "LG" + configuredDelayMs.ToString("D3") };
                    var setResp = await client.PostAsJsonAsync(new Uri(new Uri(api), "/api/hsm/command"), setBody);
                    setResp.EnsureSuccessStatusCode();
                    var setJson = await setResp.Content.ReadFromJsonAsync<System.Text.Json.JsonElement?>();
                    var setStr = setJson.HasValue && setJson.Value.TryGetProperty("response", out var rset) ? (rset.GetString() ?? string.Empty) : string.Empty;
                    if (!setStr.Contains("00")) throw new Exception("SetHSMDelay via proxy returned non-success: " + setStr);

                    // send a simple command and measure the HTTP round-trip (includes proxy+HSM delay)
                    var sw = Stopwatch.StartNew();
                    var body = new { Command = "00" };
                    var resp = await client.PostAsJsonAsync(new Uri(new Uri(api), "/api/hsm/command"), body);
                    sw.Stop();
                    resp.EnsureSuccessStatusCode();
                    var json = await resp.Content.ReadFromJsonAsync<System.Text.Json.JsonElement?>();
                    var respStr = json.HasValue && json.Value.TryGetProperty("response", out var r) ? (r.GetString() ?? string.Empty) : string.Empty;
                    Assert.IsTrue(respStr.StartsWith("00") || respStr.StartsWith("91"), "Unexpected response: " + respStr);

                    var elapsed = (int)sw.ElapsedMilliseconds;
                    Assert.GreaterOrEqual(elapsed, configuredDelayMs, $"Elapsed {elapsed}ms should be >= configured delay {configuredDelayMs}ms");
                    // When running through an external HTTP proxy the observed round-trip includes
                    // proxy connect/overhead; do not enforce the upper bound in proxy mode.

                    proxySucceeded = true;
                }
                catch (Exception ex)
                {
                    // proxy failed — fall back to direct TCP and continue the test there
                    Console.WriteLine("Proxy attempt failed, falling back to direct TCP: " + ex.Message);
                }
                finally
                {
                    // reset any global state in the proxied HSM if proxy succeeded
                    if (proxySucceeded)
                    {
                        var resetBody = new { Command = "LG000" };
                        try { await client.PostAsJsonAsync(new Uri(new Uri(api), "/api/hsm/command"), resetBody); } catch { }
                    }
                }

                if (proxySucceeded) return;
            }

            var builder = Host.CreateDefaultBuilder().ConfigureServices((ctx, services) => { services.AddHostedService<ThalesService.ThalesTcpService>(); });

            // allocate ephemeral port
            var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
            listener.Start();
            var port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();

            Environment.SetEnvironmentVariable("THALES_SERVICE_PORT", port.ToString());

            using var host = builder.Build();
            await host.StartAsync();

            try
            {
                // send LG to set the delay
                using (var c = new TcpClient())
                {
                    await c.ConnectAsync("127.0.0.1", port);
                    using var ns = c.GetStream();
                    var framed = "0000" + "LG" + configuredDelayMs.ToString("D3");
                    var req = Encoding.ASCII.GetBytes(framed);
                    await ns.WriteAsync(req, 0, req.Length);
                    var buf = new byte[1024];
                    var read = await ns.ReadAsync(buf, 0, buf.Length);
                    var resp = Encoding.ASCII.GetString(buf, 0, Math.Max(0, read));
                    Assert.IsTrue(resp.StartsWith("00"), "SetHSMDelay response should be success: " + resp);
                }

                // small pause to ensure the configured delay is applied before the next request
                await Task.Delay(100);
                // now send a simple command and measure round-trip; this response should be delayed
                var sw = Stopwatch.StartNew();
                using (var c2 = new TcpClient())
                {
                    await c2.ConnectAsync("127.0.0.1", port);
                    using var ns2 = c2.GetStream();
                    var framed2 = "0000" + "00";
                    var req2 = Encoding.ASCII.GetBytes(framed2);
                    await ns2.WriteAsync(req2, 0, req2.Length);
                    var buf2 = new byte[1024];
                    var read2 = await ns2.ReadAsync(buf2, 0, buf2.Length);
                    sw.Stop();
                    var resp2 = Encoding.ASCII.GetString(buf2, 0, Math.Max(0, read2));
                    Assert.IsTrue(resp2.StartsWith("00") || resp2.StartsWith("91"), "Unexpected response: " + resp2);
                }

                var elapsed = (int)sw.ElapsedMilliseconds;
                Assert.GreaterOrEqual(elapsed, configuredDelayMs, $"Elapsed {elapsed}ms should be >= configured delay {configuredDelayMs}ms");
                Assert.LessOrEqual(elapsed, configuredDelayMs + allowedSlackMs, $"Elapsed {elapsed}ms is larger than allowed slack {allowedSlackMs}ms");
            }
            finally
            {
                // reset any global state
                ThalesCore.HSMSettings.ResponseDelayMs = 0;
                await host.StopAsync();
            }
        }
    }
}
