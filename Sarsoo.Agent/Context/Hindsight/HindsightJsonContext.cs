using System.Text.Json.Serialization;
using Sarsoo.Agent.Context.Hindsight.Model;

namespace Sarsoo.Agent.Context.Hindsight;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
[JsonSerializable(typeof(RetainItem))]
[JsonSerializable(typeof(RetainRequest))]
[JsonSerializable(typeof(RetainResponse))]

[JsonSerializable(typeof(RecallRequest))]
[JsonSerializable(typeof(RecallResponse))]
[JsonSerializable(typeof(RecallResult))]

[JsonSerializable(typeof(ReflectRequest))]
[JsonSerializable(typeof(ReflectResponse))]
public partial class HindsightJsonContext: JsonSerializerContext
{

}