using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Sarsoo.Agent.Session;

namespace Sarsoo.Agent.Composition;

public static class Templates
{
    extension(IChatClient client)
    {
        public AgentSessionContext AsCodingAgent(
            string name,
            ILoggerFactory? loggerFactory = null,
            IServiceProvider? serviceProvider = null
        )
            => new AgentBuilder(client, loggerFactory, serviceProvider)
                .AsCodingAgent()
                .Build(name);
    }
    
    extension(AgentBuilder builder)
    {
        public AgentBuilder AsCodingAgent()
            => builder
                .WithBashShell()
                .WithFileSystem()
                .WithAgentsMd()
                .WithSkills();
    }
}