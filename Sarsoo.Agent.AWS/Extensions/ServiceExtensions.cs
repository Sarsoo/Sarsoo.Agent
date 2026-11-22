using Microsoft.Extensions.DependencyInjection;
using Sarsoo.Agent.AWS.Context;

namespace Sarsoo.Agent.AWS.Extensions;

public static class ServiceExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddAwsServices()
        {
            services.AddTransient<AwsProfileContextProvider>();
            
            return services;
        }
    }
}