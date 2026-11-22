using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sarsoo.Agent.AWS.Extensions;
using Sarsoo.Agent.K8s.Extensions;

namespace Sarsoo.Agent.DevOps.Extensions;

public static class ServiceExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddDevOpsAgent(IConfiguration configuration)
        {
            services
                .AddKubernetesServices()
                .AddAwsServices()
                .AddDevOpsAnalystAgent();

            return services;
        }
    }
}
