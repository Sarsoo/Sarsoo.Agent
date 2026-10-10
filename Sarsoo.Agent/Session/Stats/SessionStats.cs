using Microsoft.Extensions.AI;

namespace Sarsoo.Agent.Session.Stats;

public class SessionStats
{
    public UsageDetails Usage { get; } = new();
    
    public event EventHandler? Updated;

    public virtual void Add(UsageDetails usage)
    {
        Usage.Add(usage);
        
        Updated?.Invoke(this, EventArgs.Empty);
    }
}