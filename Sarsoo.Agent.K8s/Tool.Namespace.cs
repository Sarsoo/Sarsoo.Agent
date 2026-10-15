using k8s;
using k8s.Models;
using Microsoft.Extensions.AI;
using Sarsoo.Agent.K8s.IO;
using Sarsoo.Agent.Observability;
using Sarsoo.Agent.Tool;

namespace Sarsoo.Agent.K8s;

public partial class Tool
{
    public AIFunction GetNamespacesFunction()
    {
        return AIFunctionFactory.Create(GetNamespaces, 
            name: "kubectl_get_namespaces", 
            description: "Get a list of kubernetes namespaces on a given cluster by context name",
            K8sJsonContext.Default.Options
        );
    }

    public async Task<ToolResult<V1NamespaceList>> GetNamespaces(string contextName)
    {
        using var trace = Tracing.Source.StartActivity();
        if (_index.GetClient(contextName) is { } client)
        {
            return await ToolResult<V1NamespaceList>.From(async () => await client.CoreV1.ListNamespaceAsync());
        }
        else
        {
            return ToolResult<V1NamespaceList>.FromError("Context not found");
        }
    }
}