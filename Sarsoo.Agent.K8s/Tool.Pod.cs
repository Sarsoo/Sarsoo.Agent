using k8s;
using k8s.Models;
using Microsoft.Extensions.AI;
using Sarsoo.Agent.K8s.IO;
using Sarsoo.Agent.Observability;
using Sarsoo.Agent.Tool;

namespace Sarsoo.Agent.K8s;

public partial class Tool
{
    public AIFunction GetPodFunction()
    {
        return AIFunctionFactory.Create(GetPod, 
            name: "kubectl_get_pod", 
            description: "Get details of a kubernetes pod on a given cluster by context name",
            K8sJsonContext.Default.Options
        );
    }

    public async Task<ToolResult<V1Pod>> GetPod(string contextName, string name, string @namespace)
    {
        using var trace = Tracing.Source.StartActivity();
        if (_index.GetClient(contextName) is { } client)
        {
            return await ToolResult<V1Pod>.From(async () => await client.CoreV1.ReadNamespacedPodAsync(name: name, namespaceParameter: @namespace));
        }
        else
        {
            return ToolResult<V1Pod>.FromError("Context not found");
        }
    }
    
    public AIFunction GetPodsFunction()
    {
        return AIFunctionFactory.Create(GetPods, 
            name: "kubectl_get_pods", 
            description: "Get list of kubernetes pods on a given cluster by context name, optionally scoped by namespace",
            K8sJsonContext.Default.Options
        );
    }

    public async Task<ToolResult<V1PodList>> GetPods(string contextName, string? @namespace = null)
    {
        using var trace = Tracing.Source.StartActivity();
        if (_index.GetClient(contextName) is { } client)
        {
            if (@namespace is { })
            {
                return await ToolResult<V1PodList>.From(async () => await client.CoreV1.ListNamespacedPodAsync(namespaceParameter: @namespace));
            }
            else
            {
                return await ToolResult<V1PodList>.From(async () => await client.CoreV1.ListPodForAllNamespacesAsync());
            }
        }
        else
        {
            return ToolResult<V1PodList>.FromError("Context not found");
        }
    }
}