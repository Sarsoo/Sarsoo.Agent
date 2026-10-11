using Microsoft.Extensions.AI;

namespace Sarsoo.Agent.Gate;

public interface IToolGate
{
    Task<ToolGateResponse> Request(ToolApprovalRequestContent request, string agentId, CancellationToken cancel = default);
}