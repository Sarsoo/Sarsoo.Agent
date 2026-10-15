using System.Diagnostics;

namespace Sarsoo.Agent.Observability;

public static class Tracing
{
    public static ActivitySource Source = new("Sarsoo.Agent");

    public static void SetExceptionHandlers()
    {
        AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
        {
            var ex = args.ExceptionObject as Exception;
            var activity = Activity.Current;

            bool first = true;
            while (activity != null)
            {
                if (first)
                {
                    activity.SetStatus(ActivityStatusCode.Error);
                }
                if(ex is {})
                    activity.AddException(ex);
                
                activity.Dispose();
                activity = activity.Parent;
                first = false;
            }
        };
        
        TaskScheduler.UnobservedTaskException += (sender, args) =>
        {
            var activity = Activity.Current;

            bool first = true;
            while (activity != null)
            {
                if (first)
                {
                    activity.SetStatus(ActivityStatusCode.Error);
                }
                activity.AddException(args.Exception);
                activity.Dispose();
                activity = activity.Parent;
                first = false;
            }
        };
    }
}