using Microsoft.Extensions.AI;

namespace Sarsoo.Agent.Options;

public class HindsightOptions
{
    public const string Key = "Hindsight";

    public bool Enabled { get; set; } = true;
}
