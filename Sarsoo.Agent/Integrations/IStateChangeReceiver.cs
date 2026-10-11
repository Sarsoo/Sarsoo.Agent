using Sarsoo.Agent.Session;

namespace Sarsoo.Agent.Integrations;

public interface IStateChangeReceiver
{
    Task StateChanged(string agentId, AgentState state, CancellationToken token = default);
}