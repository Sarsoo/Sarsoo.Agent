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

namespace Sarsoo.Agent.CLI.UI.Inline;

public static class InitInline
{
    public static IHost GetInlineHost(string[] args)
    {
        var settings = new HostApplicationBuilderSettings()
        {
            Args = args,
            ApplicationName = "Sarsoo Agent"
        };
        
        var host = Host.CreateEmptyApplicationBuilder(settings);
        
        host.Configuration
            .AddJsonFile(Config.GetConfigFile(), optional: true)
            .AddEnvironmentVariables();
        
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
                Tracing.SetExceptionHandlers();
            })
            .WithMetrics(o =>
            {
                o.AddMeter("Sarsoo.*");
                o.AddRuntimeInstrumentation();
            });

        host.Services.AddInlineUi().AddAgentSessions(host.Configuration);

        // Add the TUI init service last because it blocks the main thread
        // host.Services;
        return host.Build();
    }

    public static async Task CommandAction(string[] args, ParseResult pr, CancellationToken ct)
    {
        var host = GetInlineHost(args);

        ct.Register(() =>
        {
            host.StopAsync(ct);
        });

        await host.RunAsync(ct);
    }
}