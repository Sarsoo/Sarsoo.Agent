using Microsoft.Extensions.AI;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Sarsoo.Agent.CLI.UI.Chat.Message.Agent;

public class TextMessage: AgentMessage<TextContent>
{
    private Markdown Markdown = new()
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

        // The constructor receives the first streamed chunk (MessageView creates the
        // message from it rather than calling SetContent), so seed the accumulator here.
        // Otherwise the first chunk is lost when the next chunk overwrites Markdown.Text.
        _totalMessage = content.ToString() ?? string.Empty;
        Markdown.Text = _totalMessage;

        Markdown.ViewportSettings &= ~ViewportSettingsFlags.HasVerticalScrollBar;
        Markdown.ContentSizeChanged += (sender, args) =>
        {
            // Height is the frame size, which includes adornments; GetContentHeight() only
            // reports the viewport content height. Without the adornment thickness the last
            // rows of the message are clipped by the border.
            Height = Markdown.GetContentHeight() + GetAdornmentsThickness().Vertical;
            Markdown.SetNeedsDraw();
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
        
        Add(Markdown);
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
    
    private string _totalMessage = string.Empty;
    private readonly Lock _messageLock = new();
    public override void SetContent(TextContent content)
    {
        Content = content;

        var c = content.ToString();
        if (!string.IsNullOrEmpty(c))
        {
            string toSet;
            lock (_messageLock)
            {
                _totalMessage += c;
                toSet = _totalMessage;
            }
            App?.Invoke(() =>
                {
                    Markdown.Text = toSet;
                    Markdown.SetNeedsDraw();
                    SetNeedsDraw();
                }
            );
        }
    }
}