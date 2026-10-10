using System.Threading.Channels;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sarsoo.Agent.CLI.UI.Chat.Message;
using Sarsoo.Agent.Integrations;
using Sarsoo.Agent.Session;

namespace Sarsoo.Agent.CLI.Session;

public class AgentSessionLifetime(
    SAgent agent, 
    IEnumerable<IStateChangeReceiver> stateChangeReceivers,
    MessageView messageView,
    TableSessionStats sessionStats,
    ILogger<AgentSessionLifetime> logger
): BackgroundService
{
    private readonly Channel<string> _userMessages = Channel.CreateUnbounded<string>();
    private readonly Channel<AgentResponseUpdate> _agentMessages = Channel.CreateUnbounded<AgentResponseUpdate>();
    
    private AgentSession? _session;

    public AgentState State
    {
        get;
        private set
        {
            field = value;
            StateChanged?.Invoke(this, State);
            UpdateStateReceivers(State);
        }
    } = AgentState.Idle;

    public event EventHandler<AgentState>? StateChanged;

    private Task UpdateStateReceivers(AgentState state)
    {
        if (!stateChangeReceivers.Any())
        {
            return Task.CompletedTask;
        }
        
        return Task.Run(async () =>
        {
            await Parallel.ForEachAsync(stateChangeReceivers, async (receiver, token) =>
            {
                await receiver.StateChanged(State, token);
            });
        });
    }
    
    public void PassMessage(string message)
    {
        logger.LogInformation("Passing message to agent: {message}", message);
        _userMessages.Writer.TryWrite(message);
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Beginning agent lifetime monitor");
        return Task.WhenAll(
            ProcessUserMessages(stoppingToken), 
            ProcessAgentMessages(stoppingToken),
            UpdateStateReceivers(State)
        );
    }

    private async Task ProcessUserMessages(CancellationToken stoppingToken)
    {
        try
        {
            logger.LogInformation("Creating agent session");
            agent.Init();
            _session = await agent.Harness.CreateSessionAsync(stoppingToken).ConfigureAwait(false);

            logger.LogInformation("Waiting for first user message");
            await foreach (var userMessage in _userMessages.Reader.ReadAllAsync(stoppingToken).ConfigureAwait(false))
            {
                logger.LogInformation("Iterating agent with user message: {message}", userMessage);
                messageView.AddMessage(new UserMessage()
                {
                    Text = userMessage
                });
                messageView.SetNeedsDraw();

                State = AgentState.Busy;
                await foreach (var agentResponse in agent
                                   .RunStreamingAsync(userMessage, session: _session, cancellationToken: stoppingToken)
                                   .ConfigureAwait(false))
                {
                    logger.LogDebug("Queuing agent update: {agent_message}", agentResponse);
                    _agentMessages.Writer.TryWrite(agentResponse);
                }
                State = AgentState.Idle;
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error processing user messages");
        }
    }

    private async Task ProcessAgentMessages(CancellationToken stoppingToken)
    {
        try
        {
            await foreach (var agentResponseUpdate in _agentMessages.Reader.ReadAllAsync(stoppingToken)
                               .ConfigureAwait(false))
            {
                messageView.AddAgentMessage(agentResponseUpdate);

                foreach (var usage in agentResponseUpdate.Contents)
                {
                    if (usage is UsageContent usageContent)
                    {
                        sessionStats.Add(usageContent.Details);
                    }
                }
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error processing agent messages");
        }
    }

    public override void Dispose()
    {
        base.Dispose();
        
        try
        {
            if(_userMessages?.Writer.TryComplete() is false)
            {
                _userMessages?.Writer.Complete();
            }
            
            if(_agentMessages?.Writer.TryComplete() is false)
            {
                _agentMessages?.Writer.Complete();
            }
        }
        catch(Exception ex)
        {
            logger.LogError(ex, "Exception thrown on disposal");
        }
    }
}