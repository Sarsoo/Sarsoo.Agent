using System.Text.Json.Serialization;

namespace Sarsoo.Agent.Tool;

[JsonSourceGenerationOptions(DefaultIgnoreCondition = JsonIgnoreCondition.Never)]
[JsonSerializable(typeof(List<string>))]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(CommandToolResult))]
public partial class ToolJsonContext: JsonSerializerContext
{

}