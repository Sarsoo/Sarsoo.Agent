using System.Globalization;
using Microsoft.Extensions.AI;
using Terminal.Gui.Drawing;

namespace Sarsoo.Agent.CLI.UI.TUI.Chat.Message.Agent;

public class FunctionResultMessage: AgentMessage<FunctionResultContent>
{
    /// <summary>
    ///     Maximum number of characters of the tool result rendered into the view.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         A message with <see cref="Terminal.Gui.ViewBase.View.Height"/> set to <c>Dim.Auto()</c>
    ///         is measured by formatting its entire text — <c>DimAuto</c> calls
    ///         <c>TextFormatter.FormatAndGetSize</c> with an unbounded height — and the draw pass
    ///         formats it again. That cost is linear in characters and runs synchronously on the UI
    ///         thread: a 1 MB result measured ~4.3s in layout plus ~6.9s in draw, i.e. the app appears
    ///         to freeze.
    ///     </para>
    ///     <para>
    ///         Tool output is unbounded in a way streamed chat text is not, so it is truncated to a
    ///         head/tail excerpt. The full result remains in <see cref="AgentMessage{T}.Content"/>; this
    ///         only bounds what is rendered.
    ///     </para>
    /// </remarks>
    public const int MaxRenderedChars = 20_000;

    /// <summary>Characters of the result kept at the start of the rendered excerpt.</summary>
    private const int HeadChars = 12_000;

    /// <summary>Characters of the result kept at the end of the rendered excerpt.</summary>
    private const int TailChars = 8_000;

    /// <summary>How far to look for a line break when trimming the excerpt to whole lines.</summary>
    private const int LineBoundaryWindow = 4_096;

    public FunctionResultMessage(FunctionResultContent content) : base(content)
    {
        Title = "";
        BorderStyle = LineStyle.None;
        SchemeName = "FunctionResultMessage";
        
        Text = BuildRenderedText(content.Result?.ToString());
    }

    public override void SetContent(FunctionResultContent content)
    {
        // Content = content;
        //
        // Text += content.Result;
        // SetNeedsDraw();
    }

    /// <summary>
    ///     Returns <paramref name="result"/> unchanged when it fits the render budget, otherwise a
    ///     head/tail excerpt with a marker describing what was omitted.
    /// </summary>
    internal static string BuildRenderedText (string? result)
    {
        if (string.IsNullOrEmpty (result) || result.Length <= MaxRenderedChars)
        {
            return result ?? string.Empty;
        }

        int headEnd = result.LastIndexOf ('\n', HeadChars - 1, Math.Min (HeadChars, LineBoundaryWindow));

        if (headEnd < 0)
        {
            headEnd = HeadChars;
        }

        int tailSearchStart = result.Length - TailChars;
        int tailStart = result.IndexOf ('\n',
                                        tailSearchStart,
                                        Math.Min (TailChars, result.Length - tailSearchStart));

        tailStart = tailStart < 0 ? tailSearchStart : tailStart + 1;

        if (tailStart <= headEnd)
        {
            return result;
        }

        var elided = result.AsSpan (headEnd, tailStart - headEnd);
        var elidedLines = 0;

        foreach (char c in elided)
        {
            if (c == '\n')
            {
                elidedLines++;
            }
        }

        string marker = string.Format (CultureInfo.CurrentCulture,
                                       "… {0:N0} lines ({1:N0} chars) elided; full result available to the agent …",
                                       elidedLines,
                                       elided.Length);

        return $"{result[..headEnd]}\n{marker}\n{result[tailStart..]}";
    }
}
