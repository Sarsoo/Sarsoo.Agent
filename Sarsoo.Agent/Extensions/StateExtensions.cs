using Sarsoo.Agent.Session;

namespace Sarsoo.Agent.Extensions;

public static class StateExtensions
{
    extension(AgentState state)
    {
        public string HerdrState => state switch
        {
            AgentState.Idle => "idle",
            AgentState.Busy => "working",
            _ => throw new ArgumentOutOfRangeException(nameof(state), state, null)
        };
    }
}