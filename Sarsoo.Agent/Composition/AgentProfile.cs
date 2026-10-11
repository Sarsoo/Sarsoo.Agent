namespace Sarsoo.Agent.Composition;

/// <summary>
///     A named recipe for building an agent. Profiles keep agent composition out of the CLI: callers
///     ask for a profile by name and receive an <c>AIAgent</c>-backed session, regardless of whether
///     the unit is a plain agent or a workflow adapted to <c>AIAgent</c>.
/// </summary>
public sealed record AgentProfile(
    string Name,
    string Description,
    Func<AgentBuilder, AgentBuilder> Configure
);
