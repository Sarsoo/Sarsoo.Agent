using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sarsoo.Agent.Client;
using Sarsoo.Agent.Gate;
using Sarsoo.Agent.Integrations;
using Sarsoo.Agent.Options;
using Sarsoo.Agent.Session;
using Sarsoo.Agent.Session.Orchestration;

namespace Sarsoo.Agent.Extensions;

public static class ServiceExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddAgent(IConfiguration configuration)
        {
            services.AddSingleton<ClientResolver>()
                .AddSingleton<IChatClient>(sp => sp.GetRequiredService<ClientResolver>().GetClient())
                .AddSingleton<IStateChangeReceiver, Herdr>()
                .AddSingleton<IToolGateEnforcer, ToolGateEnforcer>()
                .AddSingleton<ISessionIndex, SessionIndex>()
                
                .AddSingleton<AgentInteractiveLifetime>()
                .AddHostedService<AgentInteractiveLifetime>(sp => sp.GetRequiredService<AgentInteractiveLifetime>())
                
                .Configure<ToolGateOptions>(configuration.GetSection(ToolGateOptions.Key))
                .Configure<ProviderOptions>(configuration.GetSection(ProviderOptions.OptionsKey));
            
            if (configuration.GetValue<bool>("Stubbed"))
            {
                services.AddSingleton<IChatClient, StubClient>();
            }
            else
            {
                services.AddSingleton<IChatClient>(sp => sp.GetRequiredService<ClientResolver>().GetClient());
            }
            
            return services;
        }
    }
}