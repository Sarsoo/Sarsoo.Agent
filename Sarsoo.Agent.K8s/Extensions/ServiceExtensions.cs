using Microsoft.Extensions.DependencyInjection;
using Sarsoo.Agent.K8s.Context;

namespace Sarsoo.Agent.K8s.Extensions;

public static class ServiceExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddKubernetesServices()
        {
            services.AddSingleton<ClientIndex>()
                .AddTransient<K8sClusterContextProvider>()
                .AddTransient<Tool>();
            
            return services;
        }
    }
}