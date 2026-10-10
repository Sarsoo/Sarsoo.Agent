using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog.Config;
using NLog.Extensions.Logging;
using NLog.Targets;
using Sarsoo.Agent.CLI.Extensions;
using Sarsoo.Agent.IO;
using Terminal.Gui.App;
using Terminal.Gui.Configuration;

namespace Sarsoo.Agent.CLI.UI;

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
            .AddTuiAppDefaults();
        
        var nlogConfig = new LoggingConfiguration();
        var fileTarget = new FileTarget();
        fileTarget.FileName = Path.Combine(Config.GetConfigDir(), "reeve.log");
        nlogConfig.AddTarget("file", fileTarget);
        nlogConfig.AddRule(NLog.LogLevel.Info, NLog.LogLevel.Fatal, fileTarget);
        
        host.Logging.ClearProviders()
            .AddNLog(nlogConfig);

        host.Services.AddUi().AddAgentSessions(host.Configuration);

        // Add the TUI init service last because it blocks the main thread
        // host.Services;
        return host.Build();
    }
}