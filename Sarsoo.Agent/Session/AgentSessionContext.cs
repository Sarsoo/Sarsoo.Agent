using Microsoft.Agents.AI;
using Sarsoo.Agent.Session.Stats;

namespace Sarsoo.Agent.Session;

public class AgentSessionContext(AIAgent agent): IAsyncDisposable
{
    public string Id => Agent.Id;
    public AIAgent Agent { get; } = agent;
    public AgentSession? Session { get; private set; }
    public SessionStats Stats { get; } = new();
    
    private readonly List<IDisposable> _disposables = new();
    private readonly List<IAsyncDisposable> _asyncDisposables = new();

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
            disposable.Dispose();
        }

        foreach(var disposable in _asyncDisposables)
        {
            await disposable.DisposeAsync().ConfigureAwait(false);
        }
    }
}