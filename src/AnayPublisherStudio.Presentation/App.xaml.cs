using System.IO;
using System.Windows;
using AnayPublisherStudio.Composition;
using AnayPublisherStudio.Presentation.ViewModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AnayPublisherStudio.Presentation;

/// <summary>
/// WPF application entry point. Builds the generic host, configures logging and
/// dependency injection (via the shared composition root), and shows the shell.
/// </summary>
public partial class App : System.Windows.Application
{
    private IHost? _host;

    /// <inheritdoc/>
    protected override void OnStartup(StartupEventArgs e)
    {
        var environment = Environment.GetEnvironmentVariable("APS_ENVIRONMENT") ?? "Production";

        _host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((context, config) =>
            {
                config.Sources.Clear();
                config.SetBasePath(AppContext.BaseDirectory);
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                config.AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true);
                config.AddEnvironmentVariables(prefix: "APS_");

                if (e.Args.Length > 0)
                    config.AddCommandLine(e.Args);
            })
            .ConfigureServices((context, services) =>
            {
                services.AddAnayPublisherStudio(context.Configuration);
                services.AddSingleton<MainViewModel>();
                services.AddSingleton<MainWindow>();
            })
            .Build();

        _host.Start();
        _host.Services.GetRequiredService<MainWindow>().Show();
        base.OnStartup(e);
    }

    /// <inheritdoc/>
    protected override void OnExit(ExitEventArgs e)
    {
        _host?.Dispose();
        base.OnExit(e);
    }
}
