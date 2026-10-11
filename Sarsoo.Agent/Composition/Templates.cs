using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Sarsoo.Agent.Session;

namespace Sarsoo.Agent.Composition;

public static class Templates
{
    extension(IChatClient client)
    {
        public AgentSessionContext AsCodingAgent(
            ILoggerFactory? loggerFactory = null,
            IServiceProvider? serviceProvider = null
        )
            => new AgentBuilder(client, loggerFactory, serviceProvider)
                .AsCodingAgent();
    }
    
    extension(AgentBuilder builder)
    {
        public AgentSessionContext AsCodingAgent()
            => builder
                .WithBashShell()
                .WithFileSystem()
                .WithAgentsMd()
                .WithSkills()
                .Build();
    }
}