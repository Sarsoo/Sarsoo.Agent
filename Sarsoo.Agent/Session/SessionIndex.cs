using System.Collections.Concurrent;
using Microsoft.Agents.AI;

namespace Sarsoo.Agent.Session;

public interface ISessionIndex
{
    void AddSession(AgentSessionContext agent);
    AgentSessionContext? GetSession(string agentId);
    AgentSessionContext? PopSession(string agentId);
}

public class SessionIndex : ISessionIndex
{
    private readonly ConcurrentDictionary<string, AgentSessionContext> _sessions = new();

    public void AddSession(AgentSessionContext agent)
    {
        if (!_sessions.TryAdd(agent.Id, agent))
        {
            throw new InvalidOperationException($"Agent with id '{agent.Id}' already exists in the session index.");
        }
    }
    
    public AgentSessionContext? GetSession(string agentId)
    {
        return _sessions.GetValueOrDefault(agentId);
    }
    
    public AgentSessionContext? PopSession(string agentId)
    {
        _sessions.TryRemove(agentId, out var session);
        return session;
    }
}