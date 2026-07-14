using System.IO;
using System.Windows;
using AnayPublisherStudio.Application.Abstractions;
using AnayPublisherStudio.Application.Configuration;
using AnayPublisherStudio.Composition;
using AnayPublisherStudio.Presentation.ViewModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace AnayPublisherStudio.Presentation;

public partial class App : System.Windows.Application
{
    private IHost? _host;

    protected override void OnStartup(StartupEventArgs e)
    {
        var environment = Environment.GetEnvironmentVariable("APS_ENVIRONMENT") ?? "Production";

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File(
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", "aps-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 5,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.Debug()
            .CreateLogger();

        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            Log.Fatal(args.ExceptionObject as Exception, "Unhandled domain exception");
        };

        DispatcherUnhandledException += (_, args) =>
        {
            Log.Error(args.Exception, "Unhandled UI exception");
            args.Handled = true;
        };

        _host = Host.CreateDefaultBuilder()
            .UseSerilog()
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
                services.AddSingleton<IThemeService, ThemeService>();
                services.AddSingleton<MainViewModel>();
                services.AddSingleton<MainWindow>();
                services.AddLogging(builder =>
                {
                    builder.AddSerilog(dispose: true);
                });
            })
            .Build();

        _host.Start();

        var localization = _host.Services.GetRequiredService<ILocalizationService>();
        var settings = _host.Services.GetRequiredService<ISettingsService>();
        localization.SetCulture(settings.Options.App.UICulture);

        _host.Services.GetRequiredService<MainWindow>().Show();
        Log.Information("Application started");
        base.OnStartup(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Log.Information("Application shutting down");
        Log.CloseAndFlush();
        _host?.Dispose();
        base.OnExit(e);
    }
}
