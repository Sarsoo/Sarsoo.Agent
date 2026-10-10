namespace Sarsoo.Agent.Gate;

public record struct ToolGateResponse(bool Allowed, string? Reason = null);