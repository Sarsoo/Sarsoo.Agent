using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Sarsoo.Agent.CLI.Observability;
using Sarsoo.Agent.CLI.UI.Inline;
using Sarsoo.Agent.Session;
using Sarsoo.Agent.Session.Orchestration;

namespace Sarsoo.Agent.CLI.Session.Inline;

/// <summary>
///     The UI unit for a single agent: a tab that hosts the agent's message view and binds the
///     runner's events to it. Owns the runner's disposal.
/// </summary>
public sealed class AgentTab : IAsyncDisposable
{
    // private readonly MessageView _messages;
    private readonly TerminalDispatcher _dispatcher;
    private readonly ILogger<AgentTab> _logger;
    private bool _runnerDisposed;
    private bool _viewDisposed;

    public AgentTab(AgentSessionRunner runner, TerminalDispatcher dispatcher, ILogger<AgentTab> logger)
    {
        Runner = runner;
        Session = runner.Session;
        // Stats = stats;
        // _messages = messages;
        _dispatcher = dispatcher;
        _logger = logger;

        runner.UserMessage += OnUserMessage;
        runner.AgentMessage += OnAgentMessage;
    }

    public AgentSessionRunner Runner { get; }
    public AgentSessionContext Session { get; }
    // public MessageView Messages => _messages;
    // public TokenSessionStats Stats { get; }

    private void OnUserMessage(object? sender, ChatMessage userMessage)
    {
        using var trace = CliTracing.Source.StartActivity($"{nameof(AgentTab)}::{nameof(OnUserMessage)}");
        if (!string.IsNullOrWhiteSpace(userMessage.Text))
        {
            // _messages.AddMessage(new UserMessage(userMessage));
            // _messages.SetNeedsDraw();
        }
    }

    private void OnAgentMessage(object? sender, AgentResponseUpdate update)
    {
        using var trace = CliTracing.Source.StartActivity($"{nameof(AgentTab)}::{nameof(OnAgentMessage)}");
        // _messages.AddAgentMessage(update);
        // _messages.SetNeedsDraw();
    }

    /// <summary>
    ///     Stops the runner and unsubscribes its events. Does not dispose the view, which belongs to
    ///     the UI tree; used when the workspace shuts down and the parent window owns view disposal.
    /// </summary>
    public async ValueTask DisposeRunnerAsync()
    {
        using var trace = CliTracing.Source.StartActivity($"{nameof(AgentTab)}::{nameof(DisposeRunnerAsync)}");
        if (_runnerDisposed)
        {
            return;
        }

        _runnerDisposed = true;

        Runner.UserMessage -= OnUserMessage;
        Runner.AgentMessage -= OnAgentMessage;

        await Runner.DisposeAsync().ConfigureAwait(false);
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeRunnerAsync().ConfigureAwait(false);

        if (!_viewDisposed)
        {
            _viewDisposed = true;
            // Dispose();
        }
    }
}
