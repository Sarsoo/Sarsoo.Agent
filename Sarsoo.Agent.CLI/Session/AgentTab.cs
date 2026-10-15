using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Sarsoo.Agent.CLI.Extensions;
using Sarsoo.Agent.CLI.Observability;
using Sarsoo.Agent.CLI.UI.Chat.Message;
using Sarsoo.Agent.Session;
using Sarsoo.Agent.Session.Orchestration;
using Terminal.Gui.ViewBase;

namespace Sarsoo.Agent.CLI.Session;

/// <summary>
///     The UI unit for a single agent: a tab that hosts the agent's message view and binds the
///     runner's events to it. Owns the runner's disposal.
/// </summary>
public sealed class AgentTab : View, IAsyncDisposable
{
    private readonly MessageView _messages;
    private readonly ILogger<AgentTab> _logger;
    private bool _runnerDisposed;
    private bool _viewDisposed;

    public AgentTab(AgentSessionRunner runner, MessageView messages, TokenSessionStats stats, ILogger<AgentTab> logger)
    {
        Runner = runner;
        Session = runner.Session;
        Stats = stats;
        _messages = messages;
        _logger = logger;

        Title = runner.Name;
        CanFocus = true;

        messages.Width = Dim.Fill();
        messages.Height = Dim.Fill();
        Add(messages);

        runner.UserMessage += OnUserMessage;
        runner.AgentMessage += OnAgentMessage;
    }

    public AgentSessionRunner Runner { get; }
    public AgentSessionContext Session { get; }
    public MessageView Messages => _messages;
    public TokenSessionStats Stats { get; }

    private void OnUserMessage(object? sender, ChatMessage userMessage)
    {
        _logger.LogThreadWarning(App);
        using var trace = CliTracing.Source.StartActivity($"{nameof(AgentTab)}::{nameof(OnUserMessage)}");
        if (!string.IsNullOrWhiteSpace(userMessage.Text))
        {
            _messages.AddMessage(new UserMessage(userMessage));
            _messages.SetNeedsDraw();
        }
    }

    private void OnAgentMessage(object? sender, AgentResponseUpdate update)
    {
        _logger.LogThreadWarning(App);
        using var trace = CliTracing.Source.StartActivity($"{nameof(AgentTab)}::{nameof(OnAgentMessage)}");
        _messages.AddAgentMessage(update);
        _messages.SetNeedsDraw();
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
            Dispose();
        }
    }

    protected override void Dispose(bool disposing)
    {
        _viewDisposed = true;
        base.Dispose(disposing);
    }
}
