using System.Text.Json.Serialization;
using Microsoft.Agents.AI;
using Sarsoo.Agent.Tool;

namespace Sarsoo.Agent.IO;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(AgentSession))]
public partial class JsonContext: JsonSerializerContext { }