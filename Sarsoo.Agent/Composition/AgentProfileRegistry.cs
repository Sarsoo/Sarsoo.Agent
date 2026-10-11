namespace Sarsoo.Agent.Composition;

public interface IAgentProfileRegistry
{
    IReadOnlyList<AgentProfile> Profiles { get; }
    AgentProfile Get(string name);
}

public sealed class AgentProfileRegistry : IAgentProfileRegistry
{
    private readonly Dictionary<string, AgentProfile> _profiles;

    public AgentProfileRegistry(IEnumerable<AgentProfile> profiles)
    {
        _profiles = profiles.ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);

        if (_profiles.Count == 0)
        {
            throw new InvalidOperationException("At least one agent profile must be registered.");
        }
    }

    public IReadOnlyList<AgentProfile> Profiles => _profiles.Values.ToList();

    public AgentProfile Get(string name)
    {
        if (_profiles.TryGetValue(name, out var profile))
        {
            return profile;
        }

        throw new KeyNotFoundException(
            $"No agent profile named '{name}' is registered. Known profiles: {string.Join(", ", _profiles.Keys)}."
        );
    }
}
