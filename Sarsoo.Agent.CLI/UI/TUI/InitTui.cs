using System.CommandLine;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog.Config;
using NLog.Extensions.Logging;
using NLog.Targets;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using Sarsoo.Agent.CLI.Extensions;
using Sarsoo.Agent.IO;
using Sarsoo.Agent.Observability;
using Terminal.Gui.Configuration;
using Terminal.Gui.Drawing;

namespace Sarsoo.Agent.CLI.UI.TUI;

public static class InitTui
{
    public static IHost GetTuiHost(string[] args)
    {
        var settings = new HostApplicationBuilderSettings()
        {
            Args = args,
            ApplicationName = "Sarsoo Agent"
        };
        
        var host = Host.CreateEmptyApplicationBuilder(settings);
        
        host.Configuration
            .AddJsonFile(Config.GetConfigFile(), optional: true)
            .AddEnvironmentVariables()
            .AddTuiAppDefaults();
        
        var nlogConfig = new LoggingConfiguration();
        var fileTarget = new FileTarget();
        fileTarget.FileName = Path.Combine(Config.GetConfigDir(), "reeve.log");
        nlogConfig.AddTarget("file", fileTarget);
        nlogConfig.AddRule(NLog.LogLevel.Info, NLog.LogLevel.Fatal, fileTarget);
        
        host.Logging.ClearProviders()
            .AddNLog(nlogConfig);
        
        host.AddOpenTelemetry()
            .UseOtlpExporter()
            .WithTracing(o =>
            {
                o.AddSource("Sarsoo.*");
                o.AddSource("Terminal.Gui*");
                Tracing.SetExceptionHandlers();
            })
            .WithMetrics(o =>
            {
                o.AddMeter("Sarsoo.*");
                o.AddMeter("Terminal.Gui*");
                o.AddRuntimeInstrumentation();
            });

        host.Services.AddTui().AddAgentSessions(host.Configuration);

        // Add the TUI init service last because it blocks the main thread
        // host.Services;
        return host.Build();
    }

    public static async Task CommandAction(string[] args, Option<bool> themeOption, ParseResult pr, CancellationToken ct)
    {
        if (pr.GetValue(themeOption))
        {
            PrintThemeInfo();

            return;
        }

        var host = GetTuiHost(args);

        ct.Register(() =>
        {
            host.StopAsync(ct);
        });

        await host.RunAsync(ct);
    }

    static void PrintThemeInfo()
    {
        // Touching a Terminal.Gui type loads the assembly, which runs its module initializer and
        // applies the theme config (including this app's embedded config.json).
        Scheme baseScheme = SchemeManager.GetScheme(Schemes.Base);

        Console.WriteLine($"Theme:     {ThemeManager.GetCurrentThemeName()}");
        Console.WriteLine($"Available: {string.Join(", ", ThemeManager.GetThemeNames())}");
        Console.WriteLine($"Base:      fg={baseScheme.Normal.Foreground} bg={baseScheme.Normal.Background}");
    }
}