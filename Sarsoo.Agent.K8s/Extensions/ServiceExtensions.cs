using Microsoft.Extensions.DependencyInjection;

namespace Sarsoo.Agent.K8s.Extensions;

public static class ServiceExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddKubernetesServices()
        {
            services.AddSingleton<ClientIndex>()
                .AddTransient<Tool>();
            
            return services;
        }
    }
}