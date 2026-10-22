using k8s;
using k8s.Models;
using Microsoft.Extensions.AI;
using Sarsoo.Agent.K8s.IO;
using Sarsoo.Agent.Observability;
using Sarsoo.Agent.Tool;

namespace Sarsoo.Agent.K8s;

public partial class Tool
{
    public AIFunction GetDeploymentsFunction()
    {
        return AIFunctionFactory.Create(GetDeployments, 
            name: "kubectl_get_deployments", 
            description: "Get a list of kubernetes deployments on a given cluster by context name, optionally scoped to a specific namespace",
            K8sJsonContext.Default.Options
        );
    }

    private static IEnumerable<V1Deployment> MapDeployments(V1DeploymentList deployments)
    {
        foreach (var deployment in deployments)
        {
            deployment.Metadata.ManagedFields = null;
            yield return deployment;
        }
    }

    public async Task<ToolResult<List<V1Deployment>>> GetDeployments(string contextName, string? @namespace = null)
    {
        using var trace = Tracing.Source.StartActivity();
        if (_index.GetClient(contextName) is { } client)
        {
            if (@namespace is { })
            {
                return await ToolResult<List<V1Deployment>>.From(async () =>
                    MapDeployments(await client.AppsV1.ListNamespacedDeploymentAsync(namespaceParameter: @namespace)).ToList());
            }
            else
            {
                return await ToolResult<List<V1Deployment>>.From(async () => MapDeployments(await client.AppsV1.ListDeploymentForAllNamespacesAsync()).ToList());
            }
        }
        else
        {
            return ToolResult<List<V1Deployment>>.FromError("Context not found");
        }
    }
}