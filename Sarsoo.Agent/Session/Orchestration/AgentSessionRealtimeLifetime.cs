using System.Threading.Channels;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sarsoo.Agent.Gate;
using Sarsoo.Agent.Integrations;

namespace Sarsoo.Agent.Session.Orchestration;

public class AgentSessionRealtimeLifetime(
    IEnumerable<IStateChangeReceiver> stateChangeReceivers,
    IToolGateEnforcer toolGateEnforcer,
    ILogger<AgentSessionRealtimeLifetime> logger
): BackgroundService
{
    private readonly Channel<ChatMessage> _userMessages = Channel.CreateUnbounded<ChatMessage>();
    private readonly Channel<AgentResponseUpdate> _agentMessages = Channel.CreateUnbounded<AgentResponseUpdate>();

    public AgentSessionContext? Agent
    {
        get;
        set
        {
            if (field is not null && !ReferenceEquals(value, field))
            {
                throw new InvalidOperationException("Agent session context cannot be changed once set.");
            }

            field = value;
            _agentSet.SetResult();
        }
    } = null;

    private readonly TaskCompletionSource _agentSet = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public AgentState State
    {
        get;
        private set
        {
            field = value;
            StateChanged?.Invoke(this, State);
            _ = UpdateStateReceivers(State);
        }
    } = AgentState.Idle;

    public event EventHandler<ChatMessage>? UserMessage;
    public event EventHandler<AgentResponseUpdate>? AgentMessage;
    public event EventHandler<AgentState>? StateChanged;
    public event EventHandler<ToolApprovalRequestContent>? ToolRequest;

    private async Task UpdateStateReceivers(AgentState state)
    {
        try
        {
            if (!stateChangeReceivers.Any())
            {
                return;
            }

            await Task.Run(async () =>
            {
                await Parallel.ForEachAsync(stateChangeReceivers,
                        async (receiver, token) => { await receiver.StateChanged(State, token).ConfigureAwait(false); })
                    .ConfigureAwait(false);
            });
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error updating state receivers");
        }
    }
    
    public void PassMessage(string message) => PassMessage(new ChatMessage(ChatRole.User, message));
    public void PassMessage(ChatMessage message)
    {
        logger.LogInformation("Passing message to agent: {message}", message.Text);
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
            logger.LogInformation("Starting user message loop");

            while (Agent is null)
            {
                logger.LogInformation("Agent has not been provided, waiting before starting user message processing");
                await Task.WhenAny(_agentSet.Task, Task.Delay(100, stoppingToken));
            }
            
            logger.LogInformation("Creating agent session");
            await Agent.InitSession(stoppingToken).ConfigureAwait(false);
            
            logger.LogInformation("Waiting for first user message");
            await foreach (var userMessage in _userMessages.Reader.ReadAllAsync(stoppingToken).ConfigureAwait(false))
            {
                logger.LogInformation("Iterating agent with user message: {message}", userMessage);
                
                UserMessage?.Invoke(this, userMessage);
                
                State = AgentState.Busy;
                await foreach (var agentResponse in Agent.Agent
                                   .RunStreamingAsync(userMessage, session: Agent.Session, cancellationToken: stoppingToken)
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
                AgentMessage?.Invoke(this, agentResponseUpdate);

                foreach (var content in agentResponseUpdate.Contents)
                {
                    if (content is UsageContent usageContent)
                    {
                        Agent!.Stats.Add(usageContent.Details);
                    }
                    else if (content is ToolApprovalRequestContent toolApprovalRequestContent)
                    {
                        State = AgentState.Blocked;
                        ToolRequest?.Invoke(this, toolApprovalRequestContent);
                        var response = await toolGateEnforcer.Request(toolApprovalRequestContent, stoppingToken).ConfigureAwait(false);
                        State = AgentState.Idle;
                        _userMessages.Writer.TryWrite(new ChatMessage(ChatRole.System, [response]));
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