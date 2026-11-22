using Microsoft.Extensions.DependencyInjection;
using Sarsoo.Agent.Composition;
using Sarsoo.Agent.K8s.Context;

namespace Sarsoo.Agent.K8s.Extensions;

public static class AgentExtensions
{
    extension(AgentBuilder builder)
    {
        public AgentBuilder WithKubernetesToolsReadOnly()
        {
            builder.WithFunction(sp => sp.GetRequiredService<Tool>().GetContextFunction())
                .WithFunction(sp => sp.GetRequiredService<Tool>().GetNamespacesFunction())
                .WithFunction(sp => sp.GetRequiredService<Tool>().GetNamespaceNamesFunction())
                .WithFunction(sp => sp.GetRequiredService<Tool>().GetDeploymentsFunction())
                .WithFunction(sp => sp.GetRequiredService<Tool>().GetPodsFunction())
                .WithFunction(sp => sp.GetRequiredService<Tool>().GetPodFunction());
            
            return builder;
        }

        public AgentBuilder WithKubernetesContextProvider()
        {
            builder.WithContextProvider(sp => sp.GetRequiredService<K8sClusterContextProvider>());

            return builder;
        }
    }
}