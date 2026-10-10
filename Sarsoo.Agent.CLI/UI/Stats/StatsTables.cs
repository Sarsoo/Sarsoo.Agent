using Sarsoo.Agent.CLI.Session;
using Sarsoo.Agent.Session.Orchestration;

namespace Sarsoo.Agent.CLI.UI.Stats;

public class StatsTables
{
    private readonly AgentInteractiveLifetime _context;
    
    public TokenSessionStats Tokens { get; }

    public StatsTables(AgentInteractiveLifetime context)
    {
        _context = context;
        
        Tokens = new(_context);
    }
}