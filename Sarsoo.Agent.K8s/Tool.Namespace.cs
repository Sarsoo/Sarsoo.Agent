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
            name: "kubectl_get_namespaces_verbose", 
            description: "Get a list of kubernetes namespace json objects on a given cluster by context name",
            K8sJsonContext.Default.Options
        );
    }
    public AIFunction GetNamespaceNamesFunction()
    {
        return AIFunctionFactory.Create(GetNamespaceNames, 
            name: "kubectl_get_namespaces_names", 
            description: "Get a list of kubernetes namespace names on a given cluster by context name",
            K8sJsonContext.Default.Options
        );
    }

    public async Task<ToolResult<List<V1Namespace>>> GetNamespaces(string contextName)
    {
        using var trace = Tracing.Source.StartActivity();
        if (_index.GetClient(contextName) is { } client)
        {
            return await ToolResult<List<V1Namespace>>.From(async () =>
            {
                var namespaceList = await client.CoreV1.ListNamespaceAsync();
                var returned = new List<V1Namespace>(namespaceList.Items.Count);
                foreach (var n in namespaceList.Items)
                {
                    n.Metadata.ManagedFields = null;
                    returned.Add(n);
                }
                
                return returned;
            });
        }
        else
        {
            return ToolResult<List<V1Namespace>>.FromError("Context not found");
        }
    }
    
    public async Task<ToolResult<List<string>>> GetNamespaceNames(string contextName)
    {
        using var trace = Tracing.Source.StartActivity();
        if (_index.GetClient(contextName) is { } client)
        {
            return await ToolResult<List<string>>.From(async () =>
            {
                var namespaceList = await client.CoreV1.ListNamespaceAsync();
                return [.. namespaceList.Items.Select(x => x.Name())];
            });
        }
        else
        {
            return ToolResult<List<string>>.FromError("Context not found");
        }
    }
}