using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sarsoo.Agent.Composition;
using Sarsoo.Agent.K8s.Extensions;
using Sarsoo.Agent.Session;

namespace Sarsoo.Agent.DevOps;

public static partial class Templates
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddDevOpsAnalystAgent() =>
            services.AddSingleton(new AgentProfile(
                "devops-analyst",
                "DevOps agent with kubernetes tools, AGENTS.md and skills.",
                builder => builder.AsDevOpsAnalystAgent()
                )
        );
    }
    
    extension(IChatClient client)
    {
        public AgentSessionContext AsDevOpsAnalystAgent(
            string name,
            ILoggerFactory? loggerFactory = null,
            IServiceProvider? serviceProvider = null
        )
            => new AgentBuilder(client, loggerFactory, serviceProvider)
                .AsDevOpsAnalystAgent()
                .Build(name);
    }
    
    extension(AgentBuilder builder)
    {
        public AgentBuilder AsDevOpsAnalystAgent()
            => builder
                .WithSystemPrompt("""
                                  You are an expert cloud operations analyst operating inside reeve, an agent harness. 
                                  You help users by reading the state of infrastructure resources and interpreting it.
                                  """)
                .WithAdvisor()
                .WithAgentsMd()
                .WithKubernetesToolsReadOnly()
                .WithSkills();
    }
}