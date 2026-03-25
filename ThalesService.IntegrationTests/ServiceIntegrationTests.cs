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
    public class ServiceIntegrationTests
    {
        [Test]
        public async Task ConsoleService_RespondsToSimpleRequest()
        {
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
                using var client = new TcpClient();
                await client.ConnectAsync("127.0.0.1", port);
                using var stream = client.GetStream();
                var request = Encoding.ASCII.GetBytes("00");
                await stream.WriteAsync(request, 0, request.Length);

                var buffer = new byte[1024];
                var read = await stream.ReadAsync(buffer, 0, buffer.Length);
                Assert.Greater(read, 0, "No response received from service");
                var resp = Encoding.ASCII.GetString(buffer, 0, read);
                Assert.IsTrue(resp.StartsWith("00") || resp.StartsWith("91"), $"Unexpected response: {resp}");
            }
            finally
            {
                await host.StopAsync();
            }
        }
    }
}
