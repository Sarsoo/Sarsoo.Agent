using Microsoft.Extensions.AI;
using Terminal.Gui.Drawing;

namespace Sarsoo.Agent.CLI.UI.Chat.Message.Agent;

public class FunctionResultMessage: AgentMessage<FunctionResultContent>
{
    public FunctionResultMessage(FunctionResultContent content) : base(content)
    {
        Title = "";
        BorderStyle = LineStyle.None;
        SchemeName = "FunctionResultMessage";
        
        Text = content.Result?.ToString() ?? "";
    }

    public override void SetContent(FunctionResultContent content)
    {
        Content = content;
        
        Text += content.Result;
    }
}