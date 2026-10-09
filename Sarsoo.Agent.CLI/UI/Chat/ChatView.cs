using Sarsoo.Agent.CLI.UI.Chat.Message;
using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Sarsoo.Agent.CLI.UI.Chat;

public class ChatView: View
{
    private readonly MessageView _messageView;
    private readonly ChatTextField _textField;

    public ChatView(MessageView messageView, ChatTextField textField)
    {
        CanFocus = true;
        
        _messageView = messageView;
        _textField = textField;
        
        messageView.BorderStyle = LineStyle.Dashed;
        messageView.Height = Dim.Fill(textField);
        messageView.Width = Dim.Fill();
        
        textField.Width = Dim.Fill();
        textField.Y = Pos.AnchorEnd();
        
        Add(messageView, textField);
    }

    public void FocusText()
    {
        _textField.SetFocus();
        _textField.SetNeedsDraw();
    }
}