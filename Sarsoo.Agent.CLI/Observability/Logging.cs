using NLog;
using Sarsoo.Agent.IO;

namespace Sarsoo.Agent.CLI.UI;

public static class Logging
{
    public static void InitLogging()
    {
        LogManager.Setup().LoadConfiguration(builder =>
        {
            builder.ForLogger().FilterMinLevel(LogLevel.Trace).WriteToFile(Path.Combine(Config.GetConfigDir(), "reeve.log"));
        });

        var logger = LogManager.GetCurrentClassLogger();
        AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
        {
            logger.Error((Exception)args.ExceptionObject, "Unhandled exception occurred");
        };
        TaskScheduler.UnobservedTaskException += (sender, args) =>
        {
            logger.Error(args.Exception, "Unobserved task exception occurred");
        };
    }
}