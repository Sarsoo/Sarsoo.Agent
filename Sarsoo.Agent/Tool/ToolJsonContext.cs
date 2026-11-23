using System.Text.Json.Serialization;

namespace Sarsoo.Agent.Tool;

[JsonSourceGenerationOptions]
[JsonSerializable(typeof(List<string>))]
[JsonSerializable(typeof(int))]
public partial class ToolJsonContext: JsonSerializerContext
{

}