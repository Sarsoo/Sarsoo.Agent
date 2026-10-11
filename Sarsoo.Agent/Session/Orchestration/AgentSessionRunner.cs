using System.Threading.Channels;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Sarsoo.Agent.Gate;
using Sarsoo.Agent.Integrations;

namespace Sarsoo.Agent.Session.Orchestration;

/// <summary>
///     Owns the execution lifetime of a single agent session: the user/agent message pumps, the
///     agent state machine, tool-approval handling and cancellation. Construct it with its session;
///     there is no late-bound agent handshake.
/// </summary>
public sealed class AgentSessionRunner : IAsyncDisposable
{
    private readonly Channel<ChatMessage> _userMessages = Channel.CreateUnbounded<ChatMessage>();
    private readonly Channel<AgentResponseUpdate> _agentMessages = Channel.CreateUnbounded<AgentResponseUpdate>();
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly IToolGateEnforcer _toolGateEnforcer;
    private readonly IEnumerable<IStateChangeReceiver> _stateChangeReceivers;
    private readonly ILogger<AgentSessionRunner> _logger;
    private Task? _runTask;
    private bool _disposed;

    public AgentSessionRunner(
        AgentSessionContext session,
        IToolGateEnforcer toolGateEnforcer,
        IEnumerable<IStateChangeReceiver> stateChangeReceivers,
        ILogger<AgentSessionRunner> logger
    )
    {
        Session = session;
        _toolGateEnforcer = toolGateEnforcer;
        _stateChangeReceivers = stateChangeReceivers;
        _logger = logger;
    }

    public AgentSessionContext Session { get; }
    public string Id => Session.Id;
    public string Name => Session.Name;

    public AgentState State
    {
        get;
        private set
        {
            if (field == value)
            {
                return;
            }

            field = value;
            StateChanged?.Invoke(this, State);
            _ = UpdateStateReceivers(State);
        }
    } = AgentState.Idle;

    public event EventHandler<ChatMessage>? UserMessage;
    public event EventHandler<AgentResponseUpdate>? AgentMessage;
    public event EventHandler<AgentState>? StateChanged;

    public void Post(string message) => Post(new ChatMessage(ChatRole.User, message));

    public void Post(ChatMessage message)
    {
        _logger.LogInformation("Passing message to agent {agent}: {message}", Name, message.Text);
        _userMessages.Writer.TryWrite(message);
    }

    /// <summary>
    ///     Starts the message pumps. Idempotent; subsequent calls return the same task.
    /// </summary>
    public Task RunAsync()
    {
        return _runTask ??= Task.WhenAll(
            ProcessUserMessages(_cancellationTokenSource.Token),
            ProcessAgentMessages(_cancellationTokenSource.Token)
        );
    }

    private async Task ProcessUserMessages(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("Starting user message loop for {agent}", Name);

            await Session.TryInitSession(stoppingToken).ConfigureAwait(false);

            await foreach (var userMessage in _userMessages.Reader.ReadAllAsync(stoppingToken).ConfigureAwait(false))
            {
                _logger.LogInformation("Iterating agent {agent} with user message: {message}", Name, userMessage);

                UserMessage?.Invoke(this, userMessage);

                State = AgentState.Busy;
                await foreach (var agentResponse in Session.Agent
                                   .RunStreamingAsync(userMessage, session: Session.Session, cancellationToken: stoppingToken)
                                   .ConfigureAwait(false))
                {
                    _logger.LogDebug("Queuing agent update: {agent_message}", agentResponse);
                    _agentMessages.Writer.TryWrite(agentResponse);
                }

                State = AgentState.Idle;
            }
        }
        catch (OperationCanceledException)
        {
            // Shutdown.
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error processing user messages for {agent}", Name);
        }
    }

    private async Task ProcessAgentMessages(CancellationToken stoppingToken)
    {
        try
        {
            await foreach (var agentResponseUpdate in _agentMessages.Reader.ReadAllAsync(stoppingToken).ConfigureAwait(false))
            {
                AgentMessage?.Invoke(this, agentResponseUpdate);

                foreach (var content in agentResponseUpdate.Contents)
                {
                    if (content is UsageContent usageContent)
                    {
                        Session.Stats.Add(usageContent.Details);
                    }
                    else if (content is ToolApprovalRequestContent toolApprovalRequestContent)
                    {
                        // The requesting agent's name is forwarded so the gate can attribute the prompt.
                        State = AgentState.Blocked;
                        var response = await _toolGateEnforcer
                            .Request(toolApprovalRequestContent, Name, stoppingToken)
                            .ConfigureAwait(false);
                        State = AgentState.Idle;
                        _userMessages.Writer.TryWrite(new ChatMessage(ChatRole.System, [response]));
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Shutdown.
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error processing agent messages for {agent}", Name);
        }
    }

    private async Task UpdateStateReceivers(AgentState state)
    {
        try
        {
            if (!_stateChangeReceivers.Any())
            {
                return;
            }

            await Parallel.ForEachAsync(_stateChangeReceivers,
                    async (receiver, token) => await receiver.StateChanged(Name, state, token).ConfigureAwait(false))
                .ConfigureAwait(false);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error updating state receivers for {agent}", Name);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        try
        {
            await _cancellationTokenSource.CancelAsync().ConfigureAwait(false);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error cancelling agent session {agent}", Name);
        }

        _userMessages.Writer.TryComplete();
        _agentMessages.Writer.TryComplete();

        if (_runTask is { } runTask)
        {
            try
            {
                await runTask.ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error awaiting agent session {agent} shutdown", Name);
            }
        }

        _cancellationTokenSource.Dispose();
        await Session.DisposeAsync().ConfigureAwait(false);
    }
}
