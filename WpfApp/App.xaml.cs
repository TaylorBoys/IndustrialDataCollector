using Serilog;
using System.Windows;

namespace WpfApp;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console()
            .WriteTo.File("logs/IndustrialDataCollector-.log", rollingInterval: RollingInterval.Day)
            .CreateLogger();

        Log.Information("Application started");
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Log.Information("Application exited");
        Log.CloseAndFlush();
        base.OnExit(e);
    }
}
