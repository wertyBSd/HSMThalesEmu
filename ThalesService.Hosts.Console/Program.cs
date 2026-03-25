using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ThalesService;

HostApplicationBuilder builder = Host.CreateApplicationBuilder();
builder.Services.AddHostedService<ThalesTcpService>();

var host = builder.Build();
await host.RunAsync();
