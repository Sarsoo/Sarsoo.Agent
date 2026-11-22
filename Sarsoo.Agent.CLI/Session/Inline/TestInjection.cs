using Microsoft.Extensions.Hosting;
using Sarsoo.Agent.CLI.UI.Inline;

namespace Sarsoo.Agent.CLI.Session.Inline;

public class TestInjection(TerminalDispatcher dispatcher): BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await dispatcher.Updates.WriteAsync(new UserMessageUpdate("test?"), stoppingToken);
            await Task.Delay(1000, stoppingToken);
            await dispatcher.Updates.WriteAsync(new AssistantReasoningUpdate("test?"), stoppingToken);
            await Task.Delay(1000, stoppingToken);
            await dispatcher.Updates.WriteAsync(new AssistantReasoningUpdate(" test!"), stoppingToken);
            await Task.Delay(1000, stoppingToken);
            await dispatcher.Updates.WriteAsync(new AssistantTextUpdate("test!"), stoppingToken);
            await Task.Delay(1000, stoppingToken);
        }
    }
}