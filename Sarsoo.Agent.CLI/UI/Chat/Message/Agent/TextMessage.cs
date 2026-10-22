using Microsoft.Extensions.AI;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Sarsoo.Agent.CLI.UI.Chat.Message.Agent;

public class TextMessage: AgentMessage<TextContent>
{
    private readonly Markdown _markdown = new()
    {
        Text = "",
        Width = Dim.Fill(),
        Height = Dim.Fill(),
        BorderStyle = LineStyle.None,

        // Messages are display-only; focus stays on the MessageView so arrow/page keys scroll
        // the message list rather than scrolling a message internally.
        CanFocus = false
    };
    
    public TextMessage(TextContent content) : base(content)
    {
        Title = "Agent";
        Text = string.Empty;
        BorderStyle = LineStyle.Single;

        // The base constructor seeds the accumulator with the first streamed chunk (MessageView
        // creates the message from it rather than calling SetContent), so render from there.
        // Otherwise the first chunk is lost when the next chunk overwrites the markdown text.
        _markdown.Text = AccumulatedText;

        _markdown.ViewportSettings &= ~ViewportSettingsFlags.HasVerticalScrollBar;
        _markdown.ContentSizeChanged += (sender, args) =>
        {
            // Height is the frame size, which includes adornments; GetContentHeight() only
            // reports the viewport content height. Without the adornment thickness the last
            // rows of the message are clipped by the border.
            Height = _markdown.GetContentHeight() + GetAdornmentsThickness().Vertical;
            _markdown.SetNeedsDraw();
            SetNeedsDraw();
            ScheduleRelayout();
        };

        // Drop a pending relayout if the message goes away before it runs.
        Disposing += (sender, args) =>
        {
            if (_relayoutToken is { } token)
            {
                _relayoutToken = null;
                _relayoutApp?.RemoveTimeout(token);
                _relayoutApp = null;
            }
        };
        
        Add(_markdown);
    }

    /// <inheritdoc/>
    /// <remarks>
    ///     Runs at most once per <see cref="BaseMessage.UpdateInterval"/> on the UI thread regardless
    ///     of how fast the agent streams chunks, so the whole-document Markdown reparse that
    ///     <see cref="Markdown.Text"/> triggers is rate-limited to the frame rate.
    /// </remarks>
    protected override void ApplyContentUpdate ()
    {
        _markdown.Text = AccumulatedText;
        _markdown.SetNeedsDraw();
        SetNeedsDraw();
    }

    private IApplication? _relayoutApp;
    private object? _relayoutToken;

    /// <summary>
    ///     Requests another layout pass on the next application iteration.
    /// </summary>
    /// <remarks>
    ///     <see cref="Markdown.ContentSizeChanged"/> fires while the Markdown child is being
    ///     laid out, which is after this message's own frame has already been resolved. The
    ///     <see cref="View.SetNeedsLayout"/> raised by setting <see cref="View.Height"/> is
    ///     therefore cleared again when the ancestor layout pass completes, leaving the message
    ///     at its old height until some unrelated layout (e.g. scrolling) happens. Deferring the
    ///     request to the next iteration ensures the new height is actually applied.
    /// </remarks>
    private void ScheduleRelayout()
    {
        if (_relayoutToken is not null)
        {
            return;
        }

        if (App is not { } app)
        {
            // No application (e.g. headless use); nothing will render, so just flag layout.
            SetNeedsLayout();

            return;
        }

        _relayoutApp = app;
        _relayoutToken = app.AddTimeout(TimeSpan.Zero, () =>
        {
            _relayoutToken = null;
            _relayoutApp = null;
            SetNeedsLayout();

            return false;
        });
    }
}
