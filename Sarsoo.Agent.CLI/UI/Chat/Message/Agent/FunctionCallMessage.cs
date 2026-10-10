using Microsoft.Extensions.AI;
using Sarsoo.Agent.Extensions;
using Terminal.Gui.Drawing;

namespace Sarsoo.Agent.CLI.UI.Chat.Message.Agent;

public class FunctionCallMessage: AgentMessage<FunctionCallContent>
{
    public FunctionCallMessage(FunctionCallContent content) : base(content)
    {
        Title = "Function";
        BorderStyle = LineStyle.Rounded;
        SchemeName = "FunctionCallMessage";
        
        Text = content.Signature;
    }

    public override void SetContent(FunctionCallContent content)
    {
        Content = content;
        
        Text += content.Signature;
    }
}