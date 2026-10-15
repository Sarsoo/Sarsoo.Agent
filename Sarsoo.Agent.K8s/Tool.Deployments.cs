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

    public async Task<ToolResult<V1DeploymentList>> GetDeployments(string contextName, string? @namespace = null)
    {
        using var trace = Tracing.Source.StartActivity();
        if (_index.GetClient(contextName) is { } client)
        {
            if (@namespace is { })
            {
                return await ToolResult<V1DeploymentList>.From(async () => await client.AppsV1.ListNamespacedDeploymentAsync(namespaceParameter: @namespace));
            }
            else
            {
                return await ToolResult<V1DeploymentList>.From(async () => await client.AppsV1.ListDeploymentForAllNamespacesAsync());
            }
        }
        else
        {
            return ToolResult<V1DeploymentList>.FromError("Context not found");
        }
    }
}