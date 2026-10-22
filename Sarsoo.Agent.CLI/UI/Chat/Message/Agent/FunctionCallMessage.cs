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

        // The view shows the signature, not AIContent.ToString(), so re-seed the accumulator the
        // base constructor filled from ToString() and keep only the signature as the rendered text.
        SetAccumulatedText(content.Signature ?? string.Empty);
        Text = content.Signature;
    }

    public override void SetContent(FunctionCallContent content)
    {
        Content = content;

        AppendText(content.Signature);
        RequestContentUpdate();
    }
}