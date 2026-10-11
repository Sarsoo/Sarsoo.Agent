using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sarsoo.Agent.CLI.UI.Chat.Message;
using Sarsoo.Agent.CLI.UI.Stats;
using Sarsoo.Agent.Session;
using Sarsoo.Agent.Session.Orchestration;

namespace Sarsoo.Agent.CLI.Session;

public class SessionUiBinding(
    AgentInteractiveLifetime agentInteractiveLifetime, 
    AgentSessionContext context,
    MessageView messageView,
    // StatsSideBarView sideBarView,
    ILogger<SessionUiBinding> logger
): IDisposable
{
    public Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Initialising UI event bindings");
        
        agentInteractiveLifetime.Agent = context;
        
        agentInteractiveLifetime.UserMessage += OnUserMessage;
        agentInteractiveLifetime.AgentMessage += OnAgentMessage;
        agentInteractiveLifetime.Agent.Stats.Updated += OnUsageStats;
        
        return Task.CompletedTask;
    }

    private void OnUserMessage(object? sender, ChatMessage userMessage)
    {
        if (!string.IsNullOrWhiteSpace(userMessage.Text))
        {
            messageView.AddMessage(new UserMessage(userMessage));
            messageView.SetNeedsDraw();
        }
    }
    
    private void OnAgentMessage(object? sender, AgentResponseUpdate agentResponseUpdate)
    {
        messageView.AddAgentMessage(agentResponseUpdate);
        messageView.SetNeedsDraw();
    }
    
    private void OnUsageStats(object? sender, EventArgs eventArgs)
    {
        // sideBarView.SetNeedsDraw();
    }

    public void Dispose()
    {
        agentInteractiveLifetime.UserMessage -= OnUserMessage;
        agentInteractiveLifetime.AgentMessage -= OnAgentMessage;
        agentInteractiveLifetime.Agent?.Stats.Updated -= OnUsageStats;
    }
}