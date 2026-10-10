using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sarsoo.Agent.Options;
using Sarsoo.Agent.Session;

namespace Sarsoo.Agent.Extensions;

public static class ServiceExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddAgent(IConfiguration configuration)
        {
            services.AddTransient<SAgent>()
                .AddSingleton<ClientResolver>()
                .AddSingleton<IChatClient>(sp => sp.GetRequiredService<ClientResolver>().GetClient())
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