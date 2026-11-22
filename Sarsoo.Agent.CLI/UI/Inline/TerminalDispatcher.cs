using System.Threading.Channels;
using Microsoft.Extensions.Hosting;
using Spectre.Console;

namespace Sarsoo.Agent.CLI.UI.Inline;

public record struct AssistantTextUpdate(string Text);
public record struct AssistantReasoningUpdate(string Text);
public record struct AssistantStopUpdate();
public record struct UserMessageUpdate(string Text);

public union UIUpdate(
    AssistantTextUpdate,
    AssistantReasoningUpdate,
    UserMessageUpdate
)
{
    public WritingMode GetWritingMode() => this switch
    {
        AssistantReasoningUpdate => WritingMode.Reasoning,
        AssistantTextUpdate => WritingMode.Assistant,
        UserMessageUpdate => WritingMode.User,
        _ => throw new ArgumentOutOfRangeException()
    };
}

public enum WritingMode
{
    Assistant, User, Reasoning, Tool
}

public class TerminalDispatcher(IHostApplicationLifetime applicationLifetime)
{

    private readonly Channel<UIUpdate> _updates = Channel.CreateUnbounded<UIUpdate>(new()
    {
        SingleReader = true
    });
    public ChannelWriter<UIUpdate> Updates => _updates.Writer;

    private WritingMode _currentMode = WritingMode.Assistant;

    public async Task StartUIThread()
    {
        var figlet = new FigletText("Reeve");
        AnsiConsole.Write(figlet);

        await foreach (var update in _updates.Reader.ReadAllAsync(applicationLifetime.ApplicationStopping).ConfigureAwait(false))
        {
            CheckMode(update);

            switch (update)
            {
                case AssistantTextUpdate assistantTextUpdate:
                    WriteAssistant(assistantTextUpdate.Text);
                    break;
                case AssistantReasoningUpdate assistantReasoningUpdate:
                    WriteReasoning(assistantReasoningUpdate.Text);
                    break;
                case UserMessageUpdate userMessage:
                    WriteUser(userMessage.Text);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(update));
            }
        }

        AnsiConsole.WriteLine("Stopping...");
    }

    private void CheckMode(UIUpdate update)
    {
        var updateMode = update.GetWritingMode();
        if (updateMode != _currentMode)
        {
            _currentMode = updateMode;
            ChangeMode(_currentMode);
        }
    }

    private void ChangeMode(WritingMode mode)
    {
        AnsiConsole.WriteLine();

        AnsiConsole.Markup(mode switch
        {
            WritingMode.Assistant => "",
            WritingMode.User => "",
            WritingMode.Reasoning => "",
            WritingMode.Tool => "",
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
        });

        if (mode is WritingMode.Assistant)
        {
            AnsiConsole.Status()
                .Spinner(Spinner.Known.Star)
                .Start("Loading resources...", ctx =>
                {
                    Thread.Sleep(2000);
                    AnsiConsole.MarkupLine("[green]Resources loaded![/]");
                });
        }
    }

    private void WriteAssistant(string content)
    {
        AnsiConsole.MarkupInterpolated($"[red]{content}[/]");
    }

    private void WriteReasoning(string content)
    {
        AnsiConsole.MarkupInterpolated($"[italic dim]{content}[/]");
    }

    private void WriteUser(string content)
    {
        AnsiConsole.MarkupInterpolated($"[bold blue]{content}[/]");
    }
}