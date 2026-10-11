using System.Collections.Concurrent;
using Microsoft.Agents.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sarsoo.Agent.CLI.UI.Chat;
using Sarsoo.Agent.CLI.UI.Chat.Message;
using Sarsoo.Agent.Session;
using Sarsoo.Agent.Session.Orchestration;

namespace Sarsoo.Agent.CLI.Session;

public class AgentLifecycleManager: BackgroundService, IAsyncDisposable
{
    private readonly ConcurrentDictionary<string, AgentSessionUIContext> _agentSessionContexts = new();
    private readonly IServiceProvider _serviceProvider;

    public AgentLifecycleManager(
        IServiceProvider serviceProvider
    )
    {
        _serviceProvider = serviceProvider;
    }
    
    public async Task StartAgent(string agentId, AIAgent agent)
    {
        var agentContext = new AgentSessionContext(agent);
        
        var agentLifetime = _serviceProvider.GetRequiredService<AgentInteractiveLifetime>();
        agentLifetime.Agent = agentContext;

        var messageView = new MessageView(_serviceProvider.GetRequiredService<ILogger<MessageView>>());
        messageView.Title = agentId;

        var uiBinding = new SessionUiBinding(
            agentInteractiveLifetime: agentLifetime,
            context: agentContext,
            messageView: messageView,
            logger: _serviceProvider.GetRequiredService<ILogger<SessionUiBinding>>()
        );

        var context = new AgentSessionUIContext
        {
            Loop = agentLifetime,
            Context = agentContext,
            MessageView = messageView,
            SessionUiBinding = uiBinding
        };

        if (_agentSessionContexts.TryAdd(agentId, context))
        {
            _serviceProvider.GetRequiredService<ChatView>().AgentTabs.Add(context.MessageView);
            context.Execute();
        }
        else
        {
            
        }
    }

    public async Task<bool> RemoveAgent(string agentId)
    {
        if (_agentSessionContexts.TryRemove(agentId, out var value))
        {
            value.Cancel();
            await value.DisposeAsync();
            return true;
        }

        return false;
    }

    public void PassMessage(string message)
    {
        foreach(var agent in _agentSessionContexts.Values)
        {
            var chatView = _serviceProvider.GetRequiredService<ChatView>();
            if (agent.MessageView == chatView.AgentTabs.Focused)
            {
                agent.Loop.PassMessage(message);
                break;
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var context in _agentSessionContexts.Values)
        {
            await context.DisposeAsync();
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        
    }
}