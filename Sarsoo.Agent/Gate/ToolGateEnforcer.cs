using System.Collections.Concurrent;
using System.Text;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sarsoo.Agent.Observability;
using Sarsoo.Agent.Options;

namespace Sarsoo.Agent.Gate;

public interface IToolGateEnforcer
{
    Task<ToolApprovalResponseContent> Request(ToolApprovalRequestContent request, string agentId, CancellationToken cancel = default);
}

public class ToolGateEnforcer(
    IOptions<ToolGateOptions> options,
    IEnumerable<IToolGate> policies,
    ILogger<ToolGateEnforcer> logger
) : IToolGateEnforcer
{
    public async Task<ToolApprovalResponseContent> Request(ToolApprovalRequestContent request, string agentId, CancellationToken cancel = default)
    {
        using var trace = Tracing.Source.StartActivity($"{nameof(ToolGateEnforcer)}::{nameof(Request)}");
        
        logger.LogInformation("Tool approval required ({call_id}): {tool_request}", request.ToolCall.CallId, request);
        
        bool result = options.Value.DefaultResult;
        
        var results = new List<ToolGateResponse>(policies.Count());
        foreach (var policy in policies)
        {
            logger.LogInformation("Requesting tool approval from policy {policy}", policy.GetType().Name);
            results.Add(await policy.Request(request, agentId, cancel).ConfigureAwait(false));
        }

        if (options.Value.PolicyRule == ToolGatePolicyRules.All)
        {
            if (results.Any(r => !r.Allowed))
            {
                result = false;
            }
        }
        else if (options.Value.PolicyRule == ToolGatePolicyRules.Any)
        {
            if (results.All(r => !r.Allowed))
            {
                result = false;
            }
        }
        
        var response = new StringBuilder();
        foreach(var r in results.Where(r => r.Allowed == result))
        {
            if (!string.IsNullOrWhiteSpace(r.Reason))
            {
                response.AppendLine(r.Reason);
            }
        }
        
        var responseString = response.ToString();
        if (string.IsNullOrWhiteSpace(responseString))
        {
            responseString = null;
        }

        logger.LogInformation("Tool gate result: {tool_gate_result}", result);
        return request.CreateResponse(result, responseString);
    }
}