using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

var builder = Host.CreateDefaultBuilder();
builder.ConfigureServices(services => { services.AddHostedService<ThalesService.ThalesTcpService>(); });
builder.UseWindowsService();
var host = builder.Build();
await host.RunAsync();
