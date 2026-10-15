using System.Text.Json;
using System.Text.Json.Serialization;
using k8s.Models;
using Sarsoo.Agent.Tool;

namespace Sarsoo.Agent.K8s.IO;

[JsonSourceGenerationOptions()]
[JsonSerializable(typeof(ToolResult<V1NamespaceList>))]
[JsonSerializable(typeof(ToolResult<V1DeploymentList>))]
[JsonSerializable(typeof(ToolResult<V1PodList>))]
[JsonSerializable(typeof(ToolResult<V1Pod>))]
[JsonSerializable(typeof(IEnumerable<string>))]
[JsonSerializable(typeof(JsonElement))]
public partial class K8sJsonContext: JsonSerializerContext
{
    
}