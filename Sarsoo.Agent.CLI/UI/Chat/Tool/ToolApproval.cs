using Microsoft.Extensions.AI;
using Sarsoo.Agent.CLI.Session;
using Sarsoo.Agent.Extensions;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Sarsoo.Agent.CLI.UI.Chat.Tool;

public class ToolApproval: View
{
    private readonly View _toolSignature = new()
    {
        CanFocus = false,
        TabStop = TabBehavior.NoStop,
        TextAlignment = Alignment.Center,
        Width = Dim.Fill(),
        Height = Dim.Auto(),
        BorderStyle = LineStyle.Rounded
    };
    
    private readonly CheckBox _approval = new()
    {
        CanFocus = true,
        TabStop = TabBehavior.TabStop,
        Width = Dim.Fill(),
        Height = Dim.Auto(),
        Value = CheckState.Checked,
        Text = "Approve?"
    };
    
    private readonly TextField _additionalPrompt = new()
    {
        CanFocus = true,
        TabStop = TabBehavior.TabStop,
        Width = Dim.Fill(),
        // Height = Dim.Auto(),
        Title = "Additional Prompt",
        BorderStyle = LineStyle.Single
    };
    
    public ToolApproval()
    {
        Title = "Tool Approval";
        CanFocus = true;
        Width = Dim.Fill();
        Height = Dim.Fill();
        
        _approval.Y = Pos.Bottom(_toolSignature);
        _additionalPrompt.Y = Pos.Bottom(_approval);
        
        Add(_toolSignature, _approval, _additionalPrompt);
    }
    
    public void SetTool(ToolApprovalRequestContent content)
    {
        if (content.ToolCall is FunctionCallContent tool)
        {
            _toolSignature.Text = tool.Signature;
            _toolSignature.SetNeedsDraw();
        }
    }
    
    public (bool, string?) Value
    {
        get
        {
            var check = _approval.Value == CheckState.Checked;
            var prompt = string.IsNullOrWhiteSpace(_additionalPrompt.Value) ? null : _additionalPrompt.Value;
            
            return (check, prompt);
        }
    }
}