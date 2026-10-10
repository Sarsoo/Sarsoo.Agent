using System.Text;
using Microsoft.Agents.AI;
using Microsoft.Extensions.Logging;

namespace Sarsoo.Agent.Context;

public class AgentsMdProvider : AIContextProvider
{
    private class AgentsMdState
    {
        public string? AgentsMd { get; set; }
        private bool _initialized;

        public async Task<string?> GetAgentsMd(ILogger<AgentsMdProvider> logger,
            CancellationToken cancellationToken = default)
        {
            if (!_initialized)
            {
                await Initialize(logger, cancellationToken);
            }

            return AgentsMd;
        }
        
        private async Task Initialize(ILogger<AgentsMdProvider> logger, CancellationToken cancellationToken = default)
        {
            var cwd = Directory.GetCurrentDirectory();
            var root = Directory.GetDirectoryRoot(cwd);
        
            var instructionBuilder = new StringBuilder();

            var agents = new List<string>(6);
            try
            {
                var directoryCursor = cwd;
            
                while(directoryCursor != root && Directory.Exists(directoryCursor))
                {
                    var agentsMdPath = Path.Combine(directoryCursor, "AGENTS.md");
                    if (Path.Exists(agentsMdPath))
                    {
                        var agentsMd = await File.ReadAllTextAsync(agentsMdPath, cancellationToken);
                        agents.Add(agentsMd);
                    }
                    directoryCursor = Directory.GetParent(directoryCursor)?.FullName;
                }
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error loading AGENTS.md");
            }

            for (var i = agents.Count; i != 0; i--)
            {
                instructionBuilder.AppendLine(agents[i - 1]);
            }

            if (instructionBuilder.Length > 0)
            {
                AgentsMd = instructionBuilder.ToString();
            }
            
            _initialized = true;
        }
    }
    
    private readonly ProviderSessionState<AgentsMdState> _sessionState;
    private readonly ILogger<AgentsMdProvider> _logger;

    public AgentsMdProvider(ILogger<AgentsMdProvider> logger)
    {
        _logger = logger;
        _sessionState = new ProviderSessionState<AgentsMdState>(
            _ => new AgentsMdState(),
            GetType().Name
        );
    }

    protected override async ValueTask<AIContext> ProvideAIContextAsync(InvokingContext context, CancellationToken cancellationToken = default)
    {
        var state = _sessionState.GetOrInitializeState(context.Session);
        
        var retContext = new AIContext
        {
            Instructions = await state.GetAgentsMd(_logger, cancellationToken)
        };

        return retContext;
    }
}