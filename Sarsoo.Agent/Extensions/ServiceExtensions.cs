using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sarsoo.Agent.Advisor;
using Sarsoo.Agent.Client;
using Sarsoo.Agent.Composition;
using Sarsoo.Agent.Gate;
using Sarsoo.Agent.Integrations;
using Sarsoo.Agent.Options;

namespace Sarsoo.Agent.Extensions;

public static class ServiceExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddAgent(IConfiguration configuration)
        {
            services
                .AddSingleton<IStateChangeReceiver, Herdr>()
                .AddSingleton<IToolGateEnforcer, ToolGateEnforcer>()

                .AddCodingAgent()
                .AddSingleton<IAgentProfileRegistry, AgentProfileRegistry>()
                .AddTransient<IAdvisorFactory, AdvisorFactory>()
                .AddSingleton<IAgentFactory, AgentFactory>()

                .Configure<ToolGateOptions>(configuration.GetSection(ToolGateOptions.Key))
                .Configure<ProviderOptions>(configuration.GetSection(ProviderOptions.OptionsKey))
                .Configure<AgentOptions>(configuration.GetSection(AgentOptions.Key))
                .Configure<AdvisorOptions>(configuration.GetSection(AdvisorOptions.Key));

            if (configuration.GetValue<bool>("Stubbed"))
            {
                services.AddSingleton<IClientResolver, StubClientResolver>();
            }
            else
            {
                services.AddSingleton<IClientResolver, OpenAIClientResolver>();
            }

            return services;
        }
    }
}
