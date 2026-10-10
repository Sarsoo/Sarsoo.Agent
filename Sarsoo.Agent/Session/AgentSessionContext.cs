using Microsoft.Agents.AI;
using Sarsoo.Agent.Session.Stats;

namespace Sarsoo.Agent.Session;

public class AgentSessionContext
{
    public string Id => Agent.Id;
    public AIAgent Agent { get; }
    public AgentSession? Session { get; private set; }
    public SessionStats Stats { get; init; } = new();
    
    public AgentSessionContext(AIAgent agent)
    {
        Agent = agent;
    }

    public async Task InitSession(CancellationToken cancellationToken = default)
    {
        if (Session is null)
        {
            Session = await Agent.CreateSessionAsync(cancellationToken).ConfigureAwait(false);
        }
        else
        {
            throw new InvalidOperationException("Session is already initialised");
        }
    }
}