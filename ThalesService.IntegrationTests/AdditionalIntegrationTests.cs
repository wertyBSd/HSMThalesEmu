using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

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
            using var client = new TcpClient();
            await client.ConnectAsync("127.0.0.1", port);
            using var stream = client.GetStream();
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
    }
}
