using Sarsoo.Agent.CLI.Session;
using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Sarsoo.Agent.CLI.UI.Chat;

public class ChatView: View
{
    private readonly ChatTextField _textField;

    public ChatView(AgentWorkspace workspace, ChatTextField textField)
    {
        CanFocus = true;
        _textField = textField;

        var tabs = workspace.TabsHost;
        tabs.BorderStyle = LineStyle.None;
        tabs.Height = Dim.Fill(textField);
        tabs.Width = Dim.Fill();

        // Tabs is a TabGroup by default, so Tab would skip it and there would be no way
        // to move focus back from the input to the tabs. Make it a regular Tab stop so
        // Tab cycles between the tabs and the input.
        tabs.TabStop = TabBehavior.TabStop;

        // Tabs makes each tab's border title a focusable Tab stop, which traps Tab
        // inside the tab (content <-> title) and prevents reaching the input field.
        // Keep the title focusable for tab switching, but out of keyboard navigation.
        tabs.SubViewAdded += (_, e) => ExcludeTitleFromNavigation(e.SubView);

        foreach (var tab in tabs.SubViews)
        {
            ExcludeTitleFromNavigation(tab);
        }

        textField.Width = Dim.Fill();
        textField.Height = 5;
        textField.Y = Pos.AnchorEnd();

        Add(tabs, textField);
    }

    public void FocusText()
    {
        _textField.SetFocus();
        _textField.SetNeedsDraw();
    }

    private static void ExcludeTitleFromNavigation(View? tab)
    {
        if (tab?.Border.View is BorderView { TitleView: { } title })
        {
            title.TabStop = TabBehavior.NoStop;
        }
    }
}
