using System.Text;
using Microsoft.Extensions.AI;
using Sarsoo.Agent.CLI.UI.TUI.Chat.Message.Agent;

namespace Sarsoo.Agent.Tests;

/// <summary>
///     Oversized tool results are rendered as a bounded head/tail excerpt. Rendering the whole result
///     makes <c>Dim.Auto</c> and the draw pass format megabytes of text synchronously on the UI thread,
///     which stalls the application for seconds.
/// </summary>
public class FunctionResultMessageSnippetTests
{
    [Fact]
    public void SmallResult_IsRenderedVerbatim ()
    {
        var payload = "line one\nline two\nline three";
        var message = new FunctionResultMessage (new FunctionResultContent ("call", payload));

        Assert.Equal (payload, message.Text);
    }

    [Fact]
    public void LargeResult_KeepsHeadAndTailWithinTheRenderBudget ()
    {
        var payload = BuildPayload (lines: 20_000, lineLength: 50);
        var message = new FunctionResultMessage (new FunctionResultContent ("call", payload));

        Assert.True (message.Text.Length <= FunctionResultMessage.MaxRenderedChars + 200,
                     $"Rendered {message.Text.Length} chars for a {payload.Length} char result.");

        // The excerpt must still show the beginning and the end, and say what it hid.
        Assert.StartsWith (payload[..200], message.Text);
        Assert.EndsWith (payload[^200..], message.Text);
        Assert.Contains ("elided", message.Text);

        // The agent still sees the whole result; only the rendering is bounded.
        Assert.Equal (payload, message.Content.Result);
    }

    [Fact]
    public void LargeResult_TrimsToWholeLines ()
    {
        var payload = BuildPayload (lines: 20_000, lineLength: 50);
        var message = new FunctionResultMessage (new FunctionResultContent ("call", payload));

        var lines = message.Text.Split ('\n').Where (line => !line.Contains ("elided")).ToList ();

        // Every retained line must be a complete line from the payload, not a mid-line cut.
        Assert.All (lines, line => Assert.Contains (line, payload));
    }

    [Fact]
    public void ResultAtExactlyTheBudget_IsRenderedVerbatim ()
    {
        var payload = new string ('x', FunctionResultMessage.MaxRenderedChars);
        var message = new FunctionResultMessage (new FunctionResultContent ("call", payload));

        Assert.Equal (payload, message.Text);
    }

    private static string BuildPayload (int lines, int lineLength)
    {
        var sb = new StringBuilder (lines * (lineLength + 1));

        for (var i = 0; i < lines; i++)
        {
            sb.Append ($"line {i:D5} ").Append ('x', Math.Max (0, lineLength - 11)).Append ('\n');
        }

        return sb.ToString ();
    }
}
