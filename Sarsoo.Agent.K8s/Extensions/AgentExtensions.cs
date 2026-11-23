using Microsoft.Extensions.DependencyInjection;
using Sarsoo.Agent.Composition;
using Sarsoo.Agent.Context;
using Sarsoo.Agent.Extensions;
using Sarsoo.Agent.K8s.Context;
using Sarsoo.Agent.Tool.Policy;

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

        public AgentBuilder TryWithReadOnlyKubectlCommandTool()
        {
            builder.TryWithCommandTool("kubectl",
                static () => new ReadOnlyKubectlCommandPolicy(),
                (ab) =>
                {

                    ab.WithSystemPrompt("You have READ-ONLY access to the `kubectl` command. You can provide arbitrary argv to this command but policy will reject any combinations that constitute a write command.");
                    ab.WithKubernetesContextProvider();
                }
            );

            return builder;
        }
    }
}