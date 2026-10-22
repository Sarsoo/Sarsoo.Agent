using System.Text.Json;
using System.Text.Json.Serialization;
using k8s.Models;
using Sarsoo.Agent.Tool;

namespace Sarsoo.Agent.K8s.IO;

[JsonSourceGenerationOptions(WriteIndented = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(ToolResult<List<V1Namespace>>))]
[JsonSerializable(typeof(ToolResult<List<V1Deployment>>))]
[JsonSerializable(typeof(ToolResult<V1Pod>))]
[JsonSerializable(typeof(ToolResult<List<V1Pod>>))]
[JsonSerializable(typeof(ToolResult<List<string>>))]
[JsonSerializable(typeof(IEnumerable<string>))]
[JsonSerializable(typeof(JsonElement))]
public partial class K8sJsonContext: JsonSerializerContext
{
    
}