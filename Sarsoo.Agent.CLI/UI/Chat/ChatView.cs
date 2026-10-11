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
}
