using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Terminal.Gui.App;

namespace Sarsoo.Agent.CLI.Extensions;

public static class LoggingExtensions
{
    extension(ILogger logger)
    {
        public void LogThreadWarning(IApplication? app)
        {
            if (app is null)
            {
                logger.LogWarning("Provided application instance is null: {}", new StackTrace());
            }
            else
            {
                if (Environment.CurrentManagedThreadId == app.MainThreadId)
                {
                    logger.LogWarning("Action taken from main UI thread: {}", new StackTrace());
                }
            }
        }
    }
}