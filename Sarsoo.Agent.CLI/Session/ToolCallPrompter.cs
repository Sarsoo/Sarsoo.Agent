using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Sarsoo.Agent.CLI.UI.Chat.Tool;
using Sarsoo.Agent.Gate;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Sarsoo.Agent.CLI.Session;

public class ToolCallPrompter(IApplication app, ILogger<ToolCallPrompter> logger): IToolGate
{
    public async Task<ToolGateResponse> Request(ToolApprovalRequestContent request, string agentId, CancellationToken cancel = default)
    {
        var completion = new TaskCompletionSource<(bool, string?)?>(TaskCreationOptions.RunContinuationsAsynchronously);
        Prompt<ToolApproval, (bool, string?)?>? prompt = null;

        logger.LogInformation("Requesting interactive tool call approval from user for agent {agent}", agentId);

        using var registration = cancel.Register(() =>
        {
            app.Invoke(_ => prompt?.RequestStop());
            completion.TrySetResult(null);
        });

        app.Invoke(a =>
        {
            if (cancel.IsCancellationRequested)
            {
                completion.TrySetResult(null);
                return;
            }

            var result = a.TopRunnable?.Prompt<ToolApproval, (bool, string?)?>(
                resultExtractor: o => o.Value,
                beginInitHandler: o =>
                {
                    prompt = o;
                    o.Title = $"Tool Request - {agentId}";
                    o.Height = Dim.Percent(80);
                    o.Width = Dim.Percent(80);
                    o.BorderStyle = LineStyle.Rounded;

                    o.GetWrappedView().SetTool(request);
                });

            completion.TrySetResult(result);
        });

        var response = await completion.Task.ConfigureAwait(false);

        if (response is null)
        {
            logger.LogInformation("Tool call approval for agent {agent} was cancelled", agentId);
            return new ToolGateResponse(false, "Cancelled");
        }

        logger.LogInformation("Tool call approval for agent {agent} resolved: {allowed}", agentId, response.Value.Item1);
        return new ToolGateResponse(response.Value.Item1, response.Value.Item2);
    }
}
