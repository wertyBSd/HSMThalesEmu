using NUnit.Framework;
using System;
using System.Text;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace ThalesService.IntegrationTests
{
    [SetUpFixture]
    public class AssemblySetup
    {
        private static Microsoft.Extensions.Hosting.IHost? _localHost;

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            // Integration tests run via the HTTP proxy. Prefer HSM_API_URL
            var api = Environment.GetEnvironmentVariable("HSM_API_URL");
            if (string.IsNullOrEmpty(api))
            {
                // default proxy URL used during local development
                api = "http://localhost:54879";
                Environment.SetEnvironmentVariable("HSM_API_URL", api);
            }

            // quick readiness check: POST an empty command and expect HTTP status (proxy will return 400 for missing command)
            try
            {
                using var client = new System.Net.Http.HttpClient();
                client.Timeout = TimeSpan.FromSeconds(2);
                var content = new System.Net.Http.StringContent("{}", System.Text.Encoding.UTF8, "application/json");
                var resp = client.PostAsync(new Uri(new Uri(api), "/api/hsm/command"), content).GetAwaiter().GetResult();
                if (!resp.IsSuccessStatusCode && resp.StatusCode != System.Net.HttpStatusCode.BadRequest && resp.StatusCode != System.Net.HttpStatusCode.BadGateway)
                {
                    // If proxy is present but not behaving as expected, fall back to starting a local TCP-only Thales service.
                    Console.WriteLine($"HSM proxy at {api} not responding as expected (status {resp.StatusCode}). Falling back to local TCP service.");
                    StartLocalThalesService();
                }
                else
                {
                    Console.WriteLine($"HSM proxy at {api} responded with {resp.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"HSM proxy at {api} unreachable: {ex.Message}. Starting local ThalesTcpService for tests.");
                StartLocalThalesService();
            }
        }

        private static void StartLocalThalesService()
        {
            // Start an in-process ThalesTcpService listening on an ephemeral port and set THALES_SERVICE_PORT
            var builder = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder().ConfigureServices((ctx, services) => { services.AddHostedService<ThalesService.ThalesTcpService>(); });

            // allocate ephemeral port
            var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
            listener.Start();
            var port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();

            Environment.SetEnvironmentVariable("THALES_SERVICE_PORT", port.ToString());

            _localHost = builder.Build();
            _localHost.StartAsync().GetAwaiter().GetResult();
            Console.WriteLine($"Started local ThalesTcpService on port {port} for integration tests.");
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            if (_localHost != null)
            {
                _localHost.StopAsync().GetAwaiter().GetResult();
                _localHost.Dispose();
                _localHost = null;
                Console.WriteLine("Stopped local ThalesTcpService after integration tests.");
            }
        }
    }
}
