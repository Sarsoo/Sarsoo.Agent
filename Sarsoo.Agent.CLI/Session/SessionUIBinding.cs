using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sarsoo.Agent.CLI.UI.Chat.Message;
using Sarsoo.Agent.Session;
using Sarsoo.Agent.Session.Orchestration;

namespace Sarsoo.Agent.CLI.Session;

public class SessionUiBinding(
    AgentSessionRealtimeLifetime agentSessionRealtimeLifetime, 
    SAgent agent,
    MessageView messageView,
    ILogger<SessionUiBinding> logger
): BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Initialising UI event bindings");
        
        agentSessionRealtimeLifetime.Agent = new(agent);
        
        agentSessionRealtimeLifetime.UserMessage += OnUserMessage;
        agentSessionRealtimeLifetime.AgentMessage += OnAgentMessage;
        
        return Task.CompletedTask;
    }

    private void OnUserMessage(object? sender, ChatMessage userMessage)
    {
        messageView.AddMessage(new UserMessage(userMessage));
        messageView.SetNeedsDraw();
    }
    
    private void OnAgentMessage(object? sender, AgentResponseUpdate agentResponseUpdate)
    {
        messageView.AddAgentMessage(agentResponseUpdate);
        messageView.SetNeedsDraw();
    }

    public override void Dispose()
    {
        base.Dispose();
        
        agentSessionRealtimeLifetime.UserMessage -= OnUserMessage;
        agentSessionRealtimeLifetime.AgentMessage -= OnAgentMessage;
    }
}