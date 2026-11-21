using System.Text.Json.Serialization;

namespace Sarsoo.Agent.Tool;

[JsonSourceGenerationOptions]
[JsonSerializable(typeof(List<string>))]
public partial class ToolJsonContext: JsonSerializerContext
{

}