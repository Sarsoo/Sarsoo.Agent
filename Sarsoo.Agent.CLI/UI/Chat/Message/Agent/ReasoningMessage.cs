using Microsoft.Extensions.AI;
using Terminal.Gui.Drawing;

namespace Sarsoo.Agent.CLI.UI.Chat.Message.Agent;

public class ReasoningMessage: AgentMessage<TextReasoningContent>
{
    public ReasoningMessage(TextReasoningContent content) : base(content)
    {
        Title = "Thinking....";
        BorderStyle = LineStyle.RoundedDashed;
        SchemeName = "ReasoningMessage";
    }
}