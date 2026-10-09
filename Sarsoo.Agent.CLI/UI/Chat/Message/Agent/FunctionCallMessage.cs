using Microsoft.Extensions.AI;
using Terminal.Gui.Drawing;

namespace Sarsoo.Agent.CLI.UI.Chat.Message.Agent;

public class FunctionCallMessage: AgentMessage<FunctionCallContent>
{
    public FunctionCallMessage(FunctionCallContent content) : base(content)
    {
        Title = "Function";
        BorderStyle = LineStyle.Rounded;
        
        Text = GetFunctionDisplay(content);
    }

    public override void SetContent(FunctionCallContent content)
    {
        Content = content;
        
        Text += GetFunctionDisplay(content);
    }

    private string GetFunctionDisplay(FunctionCallContent content)
    {
        var ret = content.Name;
        var args = new List<string>();
        if (content.Arguments is not null)
        {
            foreach ((var k, var v) in content.Arguments)
            {
                args.Add($"{k}: {v}");
            }
        }

        if (args.Count > 0)
        {
            ret += $"({string.Join(", ", args)})";
        }

        return ret;
    }
}