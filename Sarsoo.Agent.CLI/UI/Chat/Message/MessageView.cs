using System.Drawing;
using Microsoft.Agents.AI;
using Microsoft.Extensions.Logging;
using Sarsoo.Agent.CLI.Extensions;
using Sarsoo.Agent.CLI.UI.Chat.Message.Agent;
using Sarsoo.Agent.Extensions;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Sarsoo.Agent.CLI.UI.Chat.Message;

public class MessageView: View
{
    private readonly ILogger<MessageView> _logger;
    private List<BaseMessage> _messages = new();
    private Dictionary<string, List<AgentMessage>> _messageIndex = new();
    
    public MessageView(ILogger<MessageView> logger)
    {
        _logger = logger;
        CanFocus = true;
        
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
        AddCommand (Command.Home, () => { ScrollVertical (-Int32.MaxValue); return true; });
        AddCommand (Command.End, () => { ScrollVertical (Int32.MaxValue); return true; });
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
                if (cursor?.ContentType == content.ContentType)
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

        _messages.Add(message);
        Add(message);
    }

    /// <inheritdoc/>
    protected override void OnSubViewLayout(LayoutEventArgs args)
    {
        base.OnSubViewLayout(args);

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
}