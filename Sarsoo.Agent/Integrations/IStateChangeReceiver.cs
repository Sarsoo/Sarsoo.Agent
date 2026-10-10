using Sarsoo.Agent.Session;

namespace Sarsoo.Agent.Integrations;

public interface IStateChangeReceiver
{
    Task StateChanged(AgentState state, CancellationToken token = default);
}