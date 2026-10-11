using System.Collections.Concurrent;
using Microsoft.Agents.AI;

namespace Sarsoo.Agent.Session;

public interface ISessionIndex
{
    void AddSession(AgentSessionContext agent);
    AgentSessionContext? GetSession(string agentId);
    AgentSessionContext? PopSession(string agentId);
}

public class SessionIndex : ISessionIndex, IAsyncDisposable
{
    private bool _isDisposed;
    private readonly ConcurrentDictionary<string, AgentSessionContext> _sessions = new();

    public void AddSession(AgentSessionContext agent)
    {
        if(_isDisposed)
        {
            throw new ObjectDisposedException(nameof(SessionIndex));
        }

        if (string.IsNullOrWhiteSpace(agent.Id))
        {
            throw new InvalidOperationException("Agent ID cannot be null");
        }
        
        if (!_sessions.TryAdd(agent.Id, agent))
        {
            throw new InvalidOperationException($"Agent with id '{agent.Id}' already exists in the session index.");
        }
    }
    
    public AgentSessionContext? GetSession(string agentId)
    {
        if(_isDisposed)
        {
            throw new ObjectDisposedException(nameof(SessionIndex));
        }
        return _sessions.GetValueOrDefault(agentId);
    }
    
    public AgentSessionContext? PopSession(string agentId)
    {
        if(_isDisposed)
        {
            throw new ObjectDisposedException(nameof(SessionIndex));
        }
        _sessions.TryRemove(agentId, out var session);
        return session;
    }

    public async ValueTask DisposeAsync()
    {
        if(!_isDisposed)
        {
            _isDisposed = true;
            foreach (var session in _sessions.Values)
            {
                try
                { 
                    await session.DisposeAsync();
                }
                catch(Exception e)
                {

                }
            }
        }
    }
}