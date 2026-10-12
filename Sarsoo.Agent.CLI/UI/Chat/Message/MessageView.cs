using System.Drawing;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Sarsoo.Agent.CLI.Extensions;
using Sarsoo.Agent.CLI.UI.Chat.Message.Agent;
using Sarsoo.Agent.Extensions;
using Terminal.Gui.App;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Sarsoo.Agent.CLI.UI.Chat.Message;

public class MessageView: View
{
    private readonly ILogger<MessageView> _logger;
    private List<BaseMessage> _messages = new();
    private Dictionary<string, List<AgentMessage>> _messageIndex = new();
    private bool _scrollToBottomPending;
    private IApplication? _mouseHookedApp;
    
    public MessageView(ILogger<MessageView> logger)
    {
        _logger = logger;
        CanFocus = true;
        TabStop = TabBehavior.TabStop;
        
        ViewportSettings |= ViewportSettingsFlags.HasVerticalScrollBar;
        VerticalScrollBar.VisibilityMode = ScrollBarVisibilityMode.Always;
        
        // Add key bindings for scrolling
        KeyBindings.Add (Key.CursorUp, Command.ScrollUp);
        KeyBindings.Add (Key.CursorDown, Command.ScrollDown);
        KeyBindings.Add (Key.PageUp, Command.PageUp);
        KeyBindings.Add (Key.PageDown, Command.PageDown);
        KeyBindings.Add (Key.Home, Command.Home);
        KeyBindings.Add (Key.End, Command.End);
        
        MouseBindings.Add (MouseFlags.WheeledUp, Command.ScrollUp);
        MouseBindings.Add (MouseFlags.WheeledDown, Command.ScrollDown);

        // Allow scroll commands from SubViews/adornment SubViews to bubble here
        CommandsToBubbleUp = [Command.ScrollUp, Command.ScrollDown];

        // Add command handlers
        AddCommand (Command.ScrollUp, () => { ScrollVertical (-1); return true; });
        AddCommand (Command.ScrollDown, () => { ScrollVertical (1); return true; });
        AddCommand (Command.PageUp, () => { ScrollVertical (-GetContainerSize().Height); return true; });
        AddCommand (Command.PageDown, () => { ScrollVertical (GetContainerSize().Height); return true; });
        AddCommand (Command.Home, () => { Viewport = Viewport with { Y = 0 }; return true; });
        AddCommand (Command.End, () => { ScrollToBottom (); return true; });

        Disposing += (_, _) =>
                     {
                         if (_mouseHookedApp is { } app)
                         {
                             app.Mouse.MouseEvent -= OnAppMouseEvent;
                             _mouseHookedApp = null;
                         }
                     };
    }

    /// <summary>
    ///     Hooks the application mouse stream once an <see cref="IApplication"/> is available.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Application mouse dispatch drops wheel events that land inside a message's frame but
    ///         outside its viewport (i.e. on a message border), so those events never reach this view
    ///         and scrolling on borders was unreliable. Intercepting the application-level stream lets
    ///         every wheel over this view be routed here instead.
    ///     </para>
    ///     <para>
    ///         Each view subscribes its own handler and no-ops for points outside its own subtree, so
    ///         multiple tabs can be hooked at once. The <c>ReferenceEquals</c> guard keeps layout from
    ///         subscribing more than once per application instance.
    ///     </para>
    /// </remarks>
    private void EnsureAppMouseHook ()
    {
        if (App is not { } app || ReferenceEquals (_mouseHookedApp, app))
        {
            return;
        }

        if (_mouseHookedApp is { } previous)
        {
            previous.Mouse.MouseEvent -= OnAppMouseEvent;
        }

        _mouseHookedApp = app;
        app.Mouse.MouseEvent += OnAppMouseEvent;
    }

    /// <summary>
    ///     Scrolls this view for wheel events that occur anywhere over its hierarchy, consuming the
    ///     event so no message scrolls internally.
    /// </summary>
    private void OnAppMouseEvent (object? sender, Mouse mouse)
    {
        if (mouse.Handled || !mouse.IsWheel)
        {
            return;
        }

        // Leave wheels over popovers/menus (which sit above the messages) to those views.
        // ApplicationMouse populates Mouse.View with the deepest view before raising this event, so
        // the hierarchy check is reliable even for border points that dispatch would otherwise drop.
        View? target = mouse.View;

        if (target is not null)
        {
            if (target != this && !View.IsInHierarchy (this, target, true))
            {
                return;
            }
        }
        else if (!ViewportToScreen ().Contains (mouse.ScreenPosition))
        {
            // No view was hit (e.g. the point is outside the runnable); only claim wheels that are
            // still geometrically over this view so the fix does not silently drop them.
            return;
        }

        int delta;

        if (mouse.Flags.HasFlag (MouseFlags.WheeledDown))
        {
            delta = 1;
        }
        else if (mouse.Flags.HasFlag (MouseFlags.WheeledUp))
        {
            delta = -1;
        }
        else
        {
            return;
        }

        ScrollVertical (delta);
        mouse.Handled = true;
    }

    /// <summary>
    ///     Requests that the viewport scroll to the bottom of the message view once the next layout
    ///     pass has applied the new content size. Safe to call from any thread.
    /// </summary>
    public void RequestScrollToBottom ()
    {
        App?.Invoke (() =>
        {
            _scrollToBottomPending = true;
            SetNeedsLayout ();
        });
    }

    private void ScrollToBottom () => Viewport = Viewport with { Y = Math.Max (0, GetContentHeight () - Viewport.Height) };

    private void UpdateContentSize ()
    {
        if (SubViews.Count == 0)
        {
            return;
        }

        // A base View tracks the Viewport size, so the content area never exceeds
        // what is visible and scrolling is disabled. Size the content to the
        // SubViews (floored at the Viewport) so the view can actually scroll.
        SetContentSize(
            new Size(
                Math.Max(Viewport.Width, GetWidthRequiredForSubViews()),
                Math.Max(Viewport.Height, GetHeightRequiredForSubViews())
            )
        );
    }
    
    public void AddAgentMessage(AgentResponseUpdate message)
    {
        if (message is { MessageId: { } })
        {
            if (!_messageIndex.TryGetValue(message.MessageId, out var m))
            {
                m = new();
                _messageIndex.Add(message.MessageId, m);
            }

            AgentMessage? cursor = m.LastOrDefault();
            foreach (var content in message.Contents)
            {
                if (cursor?.ContentType == content.ContentType
                    && content is not (ToolCallContent or ToolResultContent))
                {
                    cursor.SetContent(content);
                }
                else
                {
                    cursor = content.GetNewMessage();
                    if (cursor is not null)
                    {
                        m.Add(cursor);
                        AddMessage(cursor);
                    }
                }
            }
        }
    }

    public void AddMessage(BaseMessage message)
    {
        _logger.LogInformation("Adding {message_type} to message view", message.GetType());
        
        if (_messages.LastOrDefault() is { } last)
        {
            message.Y = Pos.Bottom(last);
        }
        else
        {
            message.Y = Pos.Absolute(0);
        }

        message.ContentChanged += OnMessageContentChanged;

        _messages.Add(message);
        Add(message);

        RequestScrollToBottom ();
    }

    private void OnMessageContentChanged (object? sender, EventArgs e) => RequestScrollToBottom ();

    /// <inheritdoc/>
    protected override void OnSubViewLayout(LayoutEventArgs args)
    {
        base.OnSubViewLayout(args);
        EnsureAppMouseHook();
        UpdateContentSize();
    }

    /// <inheritdoc/>
    /// <remarks>
    ///     Runs after SubViews have been laid out, so message heights reflect any content that was
    ///     just rendered (Markdown rebuilds lazily during layout). This is where a pending scroll
    ///     request can safely be honoured.
    /// </remarks>
    protected override void OnSubViewsLaidOut(LayoutEventArgs args)
    {
        base.OnSubViewsLaidOut(args);

        if (!_scrollToBottomPending)
        {
            return;
        }

        _scrollToBottomPending = false;

        // Re-read the content size now that SubViews have their final heights, then scroll.
        UpdateContentSize();
        ScrollToBottom ();
    }
}