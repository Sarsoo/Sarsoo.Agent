using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
            services.AddSingleton<OpenAIClientResolver>()
                .AddSingleton<IStateChangeReceiver, Herdr>()
                .AddSingleton<IToolGateEnforcer, ToolGateEnforcer>()

                .AddSingleton<AgentProfile>(new AgentProfile(
                    "coding",
                    "Coding agent with shell, filesystem, AGENTS.md and skills.",
                    builder => builder.AsCodingAgent()))
                .AddSingleton<IAgentProfileRegistry, AgentProfileRegistry>()
                .AddSingleton<IAgentFactory, AgentFactory>()
                .AddTransient<Func<IChatClient>>(sp => () => sp.GetRequiredService<IChatClient>())

                .Configure<ToolGateOptions>(configuration.GetSection(ToolGateOptions.Key))
                .Configure<ProviderOptions>(configuration.GetSection(ProviderOptions.OptionsKey))
                .Configure<AgentOptions>(configuration.GetSection(AgentOptions.Key));

            if (configuration.GetValue<bool>("Stubbed"))
            {
                services.AddSingleton<IChatClient, StubClient>();
            }
            else
            {
                services.AddTransient<IChatClient>(sp => sp.GetRequiredService<OpenAIClientResolver>().GetClient());
            }
            
            return services;
        }
    }
}