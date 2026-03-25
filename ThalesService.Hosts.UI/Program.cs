using System;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

[STAThread]
static async Task Main()
{
    Application.SetHighDpiMode(HighDpiMode.SystemAware);
    Application.EnableVisualStyles();
    Application.SetCompatibleTextRenderingDefault(false);

    var builder = Host.CreateDefaultBuilder();
    builder.ConfigureServices(services => { services.AddHostedService<ThalesService.ThalesTcpService>(); });

    var host = builder.Build();
    await host.StartAsync();

    using (var form = new ThalesService.Hosts.UI.ServiceUIForm(host))
    {
        Application.Run(form);
    }

    await host.StopAsync();
}
