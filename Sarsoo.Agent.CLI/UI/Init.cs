using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sarsoo.Agent.CLI.Observability;
using Sarsoo.Agent.CLI.Session;
using Terminal.Gui;
using Terminal.Gui.App;
using Terminal.Gui.Configuration;
using Terminal.Gui.Input;

namespace Sarsoo.Agent.CLI.UI;

public class TuiService(
    IApplication app, 
    MainWindow mainWindow, 
    IHostApplicationLifetime lifetime, 
    AgentWorkspace workspace,
    ILogger<TuiService> logger, 
    ILogger<IApplication> tuiLogger
): IHostedService
{
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private Thread? _appThread;
    private readonly ManualResetEvent _appThreadExited = new(false);
    
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _appThread = new Thread(StartTui)
        {
            Name = "UI",
            IsBackground = false,
            Priority = ThreadPriority.Highest
        };

        logger.LogInformation("Starting TUI Thread");
        _appThread.Start();
        
        return Task.CompletedTask;
    }

    private void StartTui()
    {
        try
        {
            Terminal.Gui.App.Logging.Logger = tuiLogger;
            Terminal.Gui.Tracing.Trace.Backend = new TraceBackend();
            Terminal.Gui.Tracing.Trace.EnabledCategories = TraceCategory.All;
            
            logger.LogInformation("Initialising TUI from thread {}", Environment.CurrentManagedThreadId);
            app.Init();

            _cancellationTokenSource.Token.Register(app.RequestStop);

            logger.LogInformation("Running TUI");
            
            app.Keyboard.KeyBindings.Remove(Key.Esc);
            app.Keyboard.KeyBindings.Add(Key.C.WithCtrl, Command.Quit);

            Task.Run(workspace.LaunchConfigured);

            app.Run(mainWindow);

            logger.LogInformation("TUI Session Ended");

            lifetime.StopApplication();
            _appThreadExited.WaitOne();
        }
        catch (Exception e)
        {
            logger.LogCritical(e, "TUI Crashed");
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Requesting TUI shutdown");
        await _cancellationTokenSource.CancelAsync();
        _appThreadExited?.Set();
        _appThread?.Join();
        logger.LogInformation("TUI Completed shutdown");
    }
}