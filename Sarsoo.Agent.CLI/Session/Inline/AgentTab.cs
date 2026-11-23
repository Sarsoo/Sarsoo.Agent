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
    private readonly TerminalDispatcher _dispatcher;
    private readonly ILogger<AgentTab> _logger;
    private bool _runnerDisposed;

    public AgentTab(AgentSessionRunner runner, TerminalDispatcher dispatcher, ILogger<AgentTab> logger)
    {
        Runner = runner;
        Session = runner.Session;
        _dispatcher = dispatcher;
        _logger = logger;

        runner.UserMessage += OnUserMessage;
        runner.AgentMessage += OnAgentMessage;
        runner.StateChanged += OnStateChange;
    }

    public AgentSessionRunner Runner { get; }
    public AgentSessionContext Session { get; }

    private void OnStateChange(object? sender, AgentState userMessage)
    {
        using var trace = CliTracing.Source.StartActivity($"{nameof(AgentTab)}::{nameof(OnUserMessage)}");

        // _dispatcher.Updates.WriteAsync(new UserMessageUpdate());
    }

    private void OnUserMessage(object? sender, ChatMessage userMessage)
    {
        using var trace = CliTracing.Source.StartActivity($"{nameof(AgentTab)}::{nameof(OnUserMessage)}");
        if (!string.IsNullOrWhiteSpace(userMessage.Text))
        {
            _dispatcher.Updates.WriteAsync(new UserMessageUpdate(userMessage.Text));
        }
    }

    private void OnAgentMessage(object? sender, AgentResponseUpdate update)
    {
        using var trace = CliTracing.Source.StartActivity($"{nameof(AgentTab)}::{nameof(OnAgentMessage)}");

        foreach (var content in update.Contents)
        {
            switch (content)
            {
                case AlwaysApproveToolApprovalResponseContent alwaysApproveToolApprovalResponseContent:
                    break;
                case CodeInterpreterToolCallContent codeInterpreterToolCallContent:
                    break;
                case CodeInterpreterToolResultContent codeInterpreterToolResultContent:
                    break;
                case DataContent dataContent:
                    break;
                case ErrorContent errorContent:
                    break;
                case FunctionCallContent functionCallContent:
                    break;
                case FunctionResultContent functionResultContent:
                    break;
                case HostedFileContent hostedFileContent:
                    break;
                case HostedVectorStoreContent hostedVectorStoreContent:
                    break;
                case ImageGenerationToolCallContent imageGenerationToolCallContent:
                    break;
                case ImageGenerationToolResultContent imageGenerationToolResultContent:
                    break;
                case ToolApprovalRequestContent toolApprovalRequestContent:
                    break;
                case InputRequestContent inputRequestContent:
                    break;
                case ToolApprovalResponseContent toolApprovalResponseContent:
                    break;
                case InputResponseContent inputResponseContent:
                    break;
                case McpServerToolCallContent mcpServerToolCallContent:
                    break;
                case McpServerToolResultContent mcpServerToolResultContent:
                    break;
                case TextContent textContent:
                    _dispatcher.Updates.WriteAsync(new AssistantReasoningUpdate(textContent.Text));
                    break;
                case TextReasoningContent textReasoningContent:
                    _dispatcher.Updates.WriteAsync(new AssistantReasoningUpdate(textReasoningContent.ToString()));
                    break;
                case WebSearchToolCallContent webSearchToolCallContent:
                    break;
                case ToolCallContent toolCallContent:
                    break;
                case WebSearchToolResultContent webSearchToolResultContent:
                    break;
                case ToolResultContent toolResultContent:
                    break;
                case UriContent uriContent:
                    break;
                case UsageContent usageContent:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(content));
            }
        }

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
    }
}
