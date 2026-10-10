using Microsoft.Extensions.AI;
using Sarsoo.Agent.Session.Stats;
using Terminal.Gui.Views;

namespace Sarsoo.Agent.CLI.Session;

public class TableSessionStats: SessionStats
{
    public event EventHandler? Updated;
    
    public TokenSessionStats Tokens { get; }

    public TableSessionStats()
    {
        Tokens = new(this);
    }

    public override void Add(UsageDetails usage)
    {
        base.Add(usage);
        
        Updated?.Invoke(this, EventArgs.Empty);
    }
}