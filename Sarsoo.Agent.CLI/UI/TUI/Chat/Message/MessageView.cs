using System.Diagnostics.Metrics;
using System.Drawing;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Sarsoo.Agent.CLI.Extensions;
using Sarsoo.Agent.CLI.Observability;
using Sarsoo.Agent.CLI.UI.TUI.Chat.Message.Agent;
using Sarsoo.Agent.Extensions;
using Terminal.Gui.App;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Sarsoo.Agent.CLI.UI.TUI.Chat.Message;

public class MessageView: View
{
    private static Counter<int> _updateContentSizes =
        CliMeter.Meter.CreateCounter<int>("sarsoo.agent.cli.messageview.updatecontentsize.call");
    private static Counter<int> _subViewLayout =
        CliMeter.Meter.CreateCounter<int>("sarsoo.agent.cli.messageview.subviewlayout.call");
    private static Counter<int> _subViewLaidOut =
        CliMeter.Meter.CreateCounter<int>("sarsoo.agent.cli.messageview.subviewslaidout.call");
    private static Counter<int> _agentMessageReceived =
        CliMeter.Meter.CreateCounter<int>("sarsoo.agent.cli.messageview.agentmessage.received");
    private static Counter<int> _messageReceived =
        CliMeter.Meter.CreateCounter<int>("sarsoo.agent.cli.messageview.message.added");
    
    private readonly ILogger<MessageView> _logger;
    private List<BaseMessage> _messages = new();
    private Dictionary<string, List<AgentMessage>> _messageIndex = new();

    /// <summary>
    ///     Whether new content should keep the viewport pinned to the bottom. Cleared when the user
    ///     scrolls away and restored when they return to the bottom (or a message is added).
    /// </summary>
    private bool _stickToBottom = true;

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

        // Add command handlers. Any manual scroll records whether the user left the bottom, so
        // autoscroll resumes only when they come back to it.
        AddCommand (Command.ScrollUp, () => ScrollBy (-1));
        AddCommand (Command.ScrollDown, () => ScrollBy (1));
        AddCommand (Command.PageUp, () => ScrollBy (-GetContainerSize().Height));
        AddCommand (Command.PageDown, () => ScrollBy (GetContainerSize().Height));
        AddCommand (Command.Home, () => ScrollTo (0));
        AddCommand (Command.End, () =>
                                      {
                                          ScrollToBottom ();

                                          return true;
                                      });

        Disposing += (_, _) =>
                     {
                         if (_mouseHookedApp is { } app)
                         {
                             app.Mouse.MouseEvent -= OnAppMouseEvent;
                             _mouseHookedApp = null;
                         }
                     };
    }

    private bool ScrollBy (int rows)
    {
        ScrollVertical (rows);
        UpdateStickiness ();

        return true;
    }

    private bool ScrollTo (int y)
    {
        Viewport = Viewport with { Y = y };
        UpdateStickiness ();

        return true;
    }

    /// <summary>True when the last row of content is visible.</summary>
    private bool IsAtBottom => Viewport.Y >= Math.Max (0, GetContentHeight () - Viewport.Height);

    private void UpdateStickiness () => _stickToBottom = IsAtBottom;

    private void ScrollToBottom ()
    {
        _stickToBottom = true;
        int target = Math.Max (0, GetContentHeight () - Viewport.Height);

        // Assigning the same position is not guaranteed to be a no-op in the framework; comparing
        // first keeps a post-layout scroll from re-arming layout every pass while content streams.
        if (Viewport.Y != target)
        {
            Viewport = Viewport with { Y = target };
        }
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
        using var trace = CliTracing.Source.StartActivity($"{nameof(MessageView)}::{nameof(EnsureAppMouseHook)}");
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

        ScrollBy (delta);
        mouse.Handled = true;
    }

    private void UpdateContentSize ()
    {
        using var trace = CliTracing.Source.StartActivity($"{nameof(MessageView)}::{nameof(UpdateContentSize)}");

        if (SubViews.Count == 0)
        {
            return;
        }

        _updateContentSizes.Add(1);

        // Messages form a single stacked column: each message's Y is Pos.Bottom(previous), so the
        // last message's bottom edge is the content height. Reading the laid-out frame avoids an
        // O(messages) Dim.Auto walk over every subview on every layout pass. Only valid once the
        // subviews have been laid out, which is why this is called from OnSubViewsLaidOut and not
        // from OnSubViewLayout.
        int contentHeight = _messages.Count == 0 ? 0 : _messages[^1].Frame.Bottom;

        // A base View tracks the Viewport size, so the content area never exceeds what is visible
        // and scrolling is disabled. Size the content to the messages (floored at the Viewport) so
        // the view can actually scroll. The content width tracks the viewport because messages are
        // Dim.Fill.
        SetContentSize(new Size(Viewport.Width, Math.Max(Viewport.Height, contentHeight)));

        // A content size change does not re-clamp the viewport, so a viewport parked past the end
        // (e.g. the terminal grew while the user was scrolled away) would show blank rows. Clamp it
        // here rather than relying on a later Viewport assignment.
        if (!_stickToBottom)
        {
            int maxY = Math.Max (0, GetContentHeight () - Viewport.Height);

            if (Viewport.Y > maxY)
            {
                Viewport = Viewport with { Y = maxY };
            }
        }
    }
    
    public void AddAgentMessage(AgentResponseUpdate message)
    {
        _logger.LogThreadWarning(App);
        if (message is { MessageId: { } })
        {
            _agentMessageReceived.Add(1);
            
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
        _logger.LogThreadWarning(App);
        _messageReceived.Add(1);
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

        // Adding a SubView must happen on the UI thread. When the caller is already on the
        // application's main thread (or there is no live main thread, e.g. headless setups that
        // never start the main loop) add it now; otherwise marshal the add through the
        // application's timed-events queue.
        if (App is { MainThreadId: int mainThreadId } app && mainThreadId != Environment.CurrentManagedThreadId)
        {
            app.Invoke(AddToView);
        }
        else
        {
            AddToView();
        }

        void AddToView ()
        {
            // A new message returns the view to the bottom even if the user had scrolled away.
            _stickToBottom = true;
            Add(message);
        }
    }

    /// <summary>
    ///     Handling for <see cref="BaseMessage.ContentChanged"/>, which is raised on the UI thread
    ///     after a coalesced content update has been applied to the message.
    /// </summary>
    private void OnMessageContentChanged (object? sender, EventArgs e) => SetNeedsLayout ();

    /// <inheritdoc/>
    protected override void OnSubViewLayout(LayoutEventArgs args)
    {
        _subViewLayout.Add(1);
        using var trace = CliTracing.Source.StartActivity($"{nameof(MessageView)}::{nameof(OnSubViewLayout)}");
        base.OnSubViewLayout(args);
        
        EnsureAppMouseHook();
    }

    /// <inheritdoc/>
    /// <remarks>
    ///     Runs after SubViews have been laid out, so message heights and positions are final and
    ///     the content size — and therefore where the bottom is — is accurate in this pass. This is
    ///     the only place the content size is measured; measuring it earlier (in
    ///     <see cref="OnSubViewLayout"/>) read stale heights and doubled the work per layout.
    /// </remarks>
    protected override void OnSubViewsLaidOut(LayoutEventArgs args)
    {
        _subViewLaidOut.Add(1);
        using var trace = CliTracing.Source.StartActivity($"{nameof(MessageView)}::{nameof(OnSubViewsLaidOut)}");
        base.OnSubViewsLaidOut(args);

        UpdateContentSize();

        // Safe to set the viewport here: the view clamps it to the content size, and a repeat of
        // the same position is a no-op, so this converges after one pass.
        if (_stickToBottom)
        {
            ScrollToBottom ();
        }
    }
}
