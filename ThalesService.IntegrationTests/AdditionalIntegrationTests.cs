using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System.Net.Http.Json;

namespace ThalesService.IntegrationTests
{
    [TestFixture]
    public class AdditionalIntegrationTests
    {
        private IHost BuildHost(int port)
        {
            Environment.SetEnvironmentVariable("THALES_SERVICE_PORT", port.ToString());
            var builder = Host.CreateDefaultBuilder().ConfigureServices((ctx, services) => { services.AddHostedService<ThalesService.ThalesTcpService>(); });
            return builder.Build();
        }

        private async Task<string> SendAndReceiveAsync(int port, string payload)
        {
            // For these integration tests we always communicate directly over TCP
            // to the hosted ThalesTcpService instance started by the test.
            using var clientTcp = new TcpClient();
            await clientTcp.ConnectAsync("127.0.0.1", port);
            using var stream = clientTcp.GetStream();
            var request = Encoding.ASCII.GetBytes(payload);
            await stream.WriteAsync(request, 0, request.Length);
            var buffer = new byte[1024];
            var read = await stream.ReadAsync(buffer, 0, buffer.Length);
            if (read <= 0) return string.Empty;
            return Encoding.ASCII.GetString(buffer, 0, read);
        }

        [Test]
        public async Task EchoCommand_Returns00()
        {
            var temp = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
            temp.Start();
            var port = ((System.Net.IPEndPoint)temp.LocalEndpoint).Port;
            temp.Stop();

            using var host = BuildHost(port);
            await host.StartAsync();
            try
            {
                var resp = await SendAndReceiveAsync(port, "B2");
                Assert.IsTrue(resp.StartsWith("00"), $"Echo did not return success: {resp}");
            }
            finally
            {
                await host.StopAsync();
            }
        }

        [Test]
        public async Task UnknownCommand_Returns91()
        {
            var temp = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
            temp.Start();
            var port = ((System.Net.IPEndPoint)temp.LocalEndpoint).Port;
            temp.Stop();

            using var host = BuildHost(port);
            await host.StartAsync();
            try
            {
                var resp = await SendAndReceiveAsync(port, "ZZ");
                Assert.IsTrue(resp.StartsWith("91"), $"Unknown command did not return 91: {resp}");
            }
            finally
            {
                await host.StopAsync();
            }
        }

        [Test]
        public async Task Service_SeedsStore_And_VerifyPinWorks()
        {
            var temp = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
            temp.Start();
            var port = ((System.Net.IPEndPoint)temp.LocalEndpoint).Port;
            temp.Stop();

            // use a temp folder for store
            var storePath = Path.Combine(Path.GetTempPath(), "thales_integ_test_store" + Guid.NewGuid().ToString("N"));
            Environment.SetEnvironmentVariable("THALES_STORE", "json");
            Environment.SetEnvironmentVariable("THALES_STORE_PATH", storePath);

            using var host = BuildHost(port);
            await host.StartAsync();
            try
            {
                // wait for seeding to complete (poll until account appears or timeout)
                var store = ThalesCore.Storage.StoreFactory.CreateFromEnvironment();
                await ThalesCore.Storage.Migrations.EnsureInitializedAsync(store);

                ThalesCore.Storage.AccountRecord? acct = null;
                int waited = 0;
                const int maxWait = 5000; // ms
                const int interval = 200; // ms
                while (waited < maxWait)
                {
                    acct = await store.GetAccountAsync("acct-0001");
                    if (acct != null) break;
                    await Task.Delay(interval);
                    waited += interval;
                }

                Assert.IsNotNull(acct, "Seeded account acct-0001 not found");
                var ok = await store.VerifyPinAsync("acct-0001", "1234");
                Assert.IsTrue(ok, "VerifyPin failed for seeded account acct-0001");
            }
            finally
            {
                await host.StopAsync();
            }
        }
    }
}
