using System.Text.Json;
using k8s;
using Microsoft.Extensions.Logging.Abstractions;
using Sarsoo.Agent.K8s;
using Sarsoo.Agent.K8s.IO;

namespace Sarsoo.Agent.Tests;

public class KubernetesTest
{
    [Fact]
    public async Task Kubernetes()
    {
        var test = new ClientIndex(NullLogger<ClientIndex>.Instance);
        test.LoadFile();
        var tool = new K8s.Tool(test);
    
        var n = await tool.GetNamespaces("sarcluster");

        var s = JsonSerializer.Serialize(n, K8sJsonContext.Default.ToolResultListV1Namespace);
    }
}