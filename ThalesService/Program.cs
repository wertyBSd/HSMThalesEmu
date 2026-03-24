using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

IHostBuilder CreateHostBuilder(string[] args) => Host.CreateDefaultBuilder(args)
    .ConfigureLogging(logging => { logging.ClearProviders(); logging.AddConsole(); })
    .ConfigureServices((context, services) =>
    {
        services.AddHostedService<ThalesService.ThalesTcpService>();
    })
    .UseWindowsService();

var host = CreateHostBuilder(args).Build();
await host.RunAsync();
