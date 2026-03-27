using NUnit.Framework;
using System;
using System.Text;

namespace ThalesService.IntegrationTests
{
    [SetUpFixture]
    public class AssemblySetup
    {
        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            // Integration tests run via the HTTP proxy. Require HSM_API_URL
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
                    Assert.Ignore($"HSM proxy at {api} not responding as expected (status {resp.StatusCode}). Start the proxy and retry.");
                }
            }
            catch (Exception ex)
            {
                Assert.Ignore($"HSM proxy at {api} unreachable: {ex.Message}. Start the proxy and retry.");
            }
        }
    }
}
