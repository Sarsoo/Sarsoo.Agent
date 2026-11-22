using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sarsoo.Agent.CLI.Session.TUI;
using Spectre.Console;

namespace Sarsoo.Agent.CLI.UI.Inline;

public class InlineService(
    IHostApplicationLifetime lifetime,
    TerminalDispatcher dispatcher,
    ILogger<InlineService> logger
): BackgroundService
{
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await dispatcher.StartUIThread();
        }
        catch (Exception e)
        {
            logger.LogCritical(e, "TUI Crashed");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Requesting TUI shutdown");
        // await _cancellationTokenSource.CancelAsync();
        logger.LogInformation("TUI Completed shutdown");
    }
}