namespace Sarsoo.Agent.Options;

public class AgentOptions
{
    public const string Key = "Agents";

    public string DefaultProfile { get; set; } = "coding";
    public List<AgentDefinition> Startup { get; set; } = new();
}

public class AgentDefinition
{
    public string Name { get; set; } = "";
    public string Profile { get; set; } = "coding";
    public string? Prompt { get; set; }
}
