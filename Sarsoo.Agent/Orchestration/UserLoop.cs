using System.Threading.Channels;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Sarsoo.Agent.Observability;

namespace Sarsoo.Agent.Orchestration;

public class UserLoop
{
    private readonly ILogger<UserLoop> _logger;
    
    private readonly Channel<ChatMessage> _userMessages = Channel.CreateUnbounded<ChatMessage>();
    private readonly Channel<AgentResponseUpdate> _agentMessages = Channel.CreateUnbounded<AgentResponseUpdate>();

    private readonly CancellationTokenSource _loopStopper = new();
    private CancellationTokenSource _perIterationStopper;
    private readonly Lock _perIterationStopperLock = new();

    public UserLoop(AIAgent agent, ILogger<UserLoop> logger)
    {
        Agent = agent;
        _logger = logger;
        
        _perIterationStopper = CancellationTokenSource.CreateLinkedTokenSource(_loopStopper.Token);
    }
    
    public AIAgent Agent { get; }
    public AgentSession? Session { get; set; }
    public Func<ChatMessage, Task<bool>>? UserMessageValidator { get; set; } = null;

    private Task? _waitFor = null;
    private readonly SemaphoreSlim _waitForLock = new(1);

    public async Task WaitFor(Task task)
    {
        using var trace = Tracing.Source.StartActivity($"{nameof(UserLoop)}::{nameof(WaitFor)}");
        await _waitForLock.WaitAsync().ConfigureAwait(false);
        try
        {
            _waitFor = task;
        }
        finally
        {
            _waitForLock.Release();
        }
    }

    public void PassMessage(ChatMessage message)
    {
        _userMessages.Writer.TryWrite(message);
    }
    
    public void CancelLoop()
    {
        using var trace = Tracing.Source.StartActivity($"{nameof(UserLoop)}::{nameof(CancelLoop)}");
        _loopStopper.Cancel();
        // _userMessagesEvent.Set();
    }

    public void CancelCurrentIteration()
    {
        using var trace = Tracing.Source.StartActivity($"{nameof(UserLoop)}::{nameof(CancelCurrentIteration)}");
        lock (_perIterationStopperLock)
        {
            _perIterationStopper.Cancel();
            _perIterationStopper.Dispose();
            _perIterationStopper = CancellationTokenSource.CreateLinkedTokenSource(_loopStopper.Token);
        }
    }

    private async Task DoWait()
    {
        using var trace = Tracing.Source.StartActivity($"{nameof(UserLoop)}::{nameof(DoWait)}");
        await _waitForLock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_waitFor is not null)
            {
                _logger.LogInformation("Waiting on provided task....");
                await _waitFor.ConfigureAwait(false);
                _logger.LogInformation("Wait completed");
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Exception thrown while awaiting blocker task, continuing");
        }
        finally
        {
            _waitFor = null;
            _waitForLock.Release();
        }
    }

    public async Task Run()
    {
        using var trace = Tracing.Source.StartActivity($"{nameof(UserLoop)}::{nameof(Run)}");
        await foreach (var userMessage in _userMessages.Reader.ReadAllAsync(_loopStopper.Token).ConfigureAwait(false))
        {
            await DoWait();
            await SingleIterateWithUserMessage(message: userMessage);
        }
    }

    private async Task SingleIterateWithUserMessage(ChatMessage message)
    {
        using var trace = Tracing.Source.StartActivity($"{nameof(UserLoop)}::{nameof(SingleIterateWithUserMessage)}");
        _logger.LogInformation("Iterating agent {agent} with user message: {message}", Agent.Name, message);

        if (UserMessageValidator is null || await UserMessageValidator(message).ConfigureAwait(false))
        {
            CancellationToken token;
            lock (_perIterationStopperLock)
            {
                token = _perIterationStopper.Token;
            }
            await SingleIteration(message, token).ConfigureAwait(false);
        }
    }

    private async Task SingleIteration(ChatMessage message, CancellationToken token = default)
    {
        using var trace = Tracing.Source.StartActivity($"{nameof(UserLoop)}::{nameof(SingleIteration)}");
        try
        {
            await foreach (var agentResponse in Agent
                               .RunStreamingAsync(message, session: Session, cancellationToken: token)
                               .ConfigureAwait(false))
            {
                _logger.LogDebug("Queuing agent update: {agent_message}", agentResponse);
                _agentMessages.Writer.TryWrite(agentResponse);
            }
        }
        catch (OperationCanceledException)
        {
            // Shutdown.
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception thrown during agent iteration, queued user message consumed");
        }
    }
}