using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Sarsoo.Agent.CLI.UI.Chat.Tool;
using Sarsoo.Agent.Gate;
using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Sarsoo.Agent.CLI.Session;

public class ToolCallPrompter(IApplication app, ILogger<ToolCallPrompter> logger): IToolGate
{
    public async Task<ToolGateResponse> Request(ToolApprovalRequestContent request, CancellationToken cancel = default)
    {
        await Task.Yield();
        var waiter = new ManualResetEvent(false);
        logger.LogInformation("Requesting interactive tool call approval from user");
        (bool, string?)? resp = null;
        app?.Invoke((a) =>
        {
            resp = a?.TopRunnable?.Prompt<ToolApproval, (bool, string?)?>(
                resultExtractor: (o) => o.Value,
                beginInitHandler: (o) =>
                {
                    o.Text = "Tool Request";
                    o.Height = Dim.Absolute(8);
                    o.Width = Dim.Absolute(40);
                });
            waiter.Set();
        });
        
        waiter.WaitOne();
        
        logger.LogInformation("Requesting interactive tool call approval from user"); 
        return new ToolGateResponse(resp?.Item1 ?? false, resp?.Item2);
    }
}