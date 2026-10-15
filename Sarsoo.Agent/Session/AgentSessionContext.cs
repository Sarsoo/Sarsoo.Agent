using Microsoft.Agents.AI;
using Sarsoo.Agent.Observability;
using Sarsoo.Agent.Session.Stats;

namespace Sarsoo.Agent.Session;

public class AgentSessionContext(AIAgent agent, string name): IAsyncDisposable
{
    public string Id => Agent.Id;
    public string Name { get; } = name;
    public AIAgent Agent { get; } = agent;
    public AgentSession? Session { get; private set; }
    public SessionStats Stats { get; } = new();
    
    private readonly List<IDisposable> _disposables = new();
    private readonly List<IAsyncDisposable> _asyncDisposables = new();
    
    private readonly SemaphoreSlim _sessionLock = new(1);
    
    public async Task<bool> TryInitSession(CancellationToken cancellationToken = default)
    {
        using var trace = Tracing.Source.StartActivity($"{nameof(AgentSessionContext)}::{nameof(TryInitSession)}");
        try
        {
            await _sessionLock.WaitAsync(cancellationToken);
            try
            {
                if (Session is null)
                {
                    Session = await Agent.CreateSessionAsync(cancellationToken).ConfigureAwait(false);
                    return true;
                }
                else
                {
                    return false;
                }
            }
            finally
            {
                _sessionLock.Release();
            }
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    public async Task InitSession(CancellationToken cancellationToken = default)
    {
        if (!await TryInitSession(cancellationToken))
        {
            throw new InvalidOperationException("Session is already initialised");
        }
    }
    
    internal void AddDisposable(object disposable)
    {
        if (disposable is IAsyncDisposable ad)
        {
            _asyncDisposables.Add(ad);
        }
        
        if (disposable is IDisposable d)
        {
            _disposables.Add(d);
        }
    }
    
    internal void AddDisposableRange(IEnumerable<object> disposable)
    {
        foreach(var d in disposable)
        {
            AddDisposable(d);
        }
    }

    public async ValueTask DisposeAsync()
    {
        foreach(var disposable in _disposables)
        {
            try
            {
                disposable.Dispose();
            }
            catch (Exception)
            {
                
            }
        }

        foreach(var disposable in _asyncDisposables)
        {
            try
            {
                await disposable.DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception)
            {
                
            }
        }
    }
}