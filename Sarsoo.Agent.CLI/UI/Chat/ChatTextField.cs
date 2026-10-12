using Microsoft.Extensions.Logging;
using Sarsoo.Agent.CLI.Session;
using Terminal.Gui.Drawing;
using Terminal.Gui.Drivers;
using Terminal.Gui.Editor;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;

namespace Sarsoo.Agent.CLI.UI.Chat;

public class ChatTextField: Editor
{
    private readonly AgentWorkspace _workspace;
    private readonly ILogger<ChatTextField> _logger;

    public ChatTextField(AgentWorkspace workspace, ILogger<ChatTextField> logger)
    {
        CanFocus = true;
        TabStop = TabBehavior.TabGroup;
        
        _workspace = workspace;
        _logger = logger;
        BorderStyle = LineStyle.Single;
        
        ViewportSettings |= ViewportSettingsFlags.HasVerticalScrollBar;

        Multiline = true;
        WordWrap = true;

        // Enter submits the prompt; Shift+Enter (or Alt+Enter where Shift+Enter is not
        // distinguishable by the terminal) inserts a newline.
        KeyBindings.Remove(Key.Enter);
        KeyBindings.Remove(Key.C.WithCtrl);
        KeyBindings.Remove(Key.Tab);
        KeyBindings.Remove(Key.Tab.WithShift);
        KeyBindings.Add (Key.Tab, Command.NextTabStop);
        KeyBindings.Add (Key.Tab.WithShift, Command.PreviousTabStop);
        KeyBindings.Add(Key.Enter, Command.Accept);
        KeyBindings.Add(Key.Enter.WithShift, Command.NewLine);
        KeyBindings.Add(Key.Enter.WithAlt, Command.NewLine);

        AddCommand(Command.Accept, OnEnter);

        // DI builds this singleton on the host thread, but TuiService runs the app on its own
        // thread. TextDocument captures its owner thread at construction, so release ownership
        // here (while we still are that thread) and let the first UI-thread access claim it.
        Document?.SetOwnerThread(null);
    }

    protected override bool OnDrawingContent(DrawContext? context)
    {
        var ret = base.OnDrawingContent(context);

        Cursor = Cursor with
        {
            Style = CursorStyle.BlinkingBar
        };

        return ret;
    }

    private bool? OnEnter()
    {
        var message = Text;

        if (!string.IsNullOrWhiteSpace(message))
        {
            _logger.LogInformation("Passing message from UI to agent: {message}", message);
            Text = "";
            
            _workspace.SendToActive(message);
        }
        
        return null;
    }
}
