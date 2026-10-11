using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Sarsoo.Agent.CLI.UI.Chat;

public class ChatView: View
{
    private readonly ChatTextField _textField;
    private readonly Tabs _tabs = new();
    
    public Tabs AgentTabs => _tabs;

    public ChatView(ChatTextField textField)
    {
        CanFocus = true;
        _textField = textField;
        
        // messageView.BorderStyle = LineStyle.None;
        // messageView.Height = Dim.Fill(textField);
        // messageView.Width = Dim.Fill();
        
        _tabs.BorderStyle = LineStyle.None;
        _tabs.Height = Dim.Fill(textField);
        _tabs.Width = Dim.Fill();
        
        textField.Width = Dim.Fill();
        textField.Height = 5;
        textField.Y = Pos.AnchorEnd();
        
        // Add(messageView, textField);
        Add(_tabs, textField);
    }

    public void FocusText()
    {
        _textField.SetFocus();
        _textField.SetNeedsDraw();
    }
}