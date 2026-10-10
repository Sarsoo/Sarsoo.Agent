namespace Sarsoo.Agent.Options;

public enum ToolGatePolicyRules {
    Any, All
}

public class ToolGateOptions
{
    public const string Key = "ToolGate";
    
    public bool DefaultResult { get; set; } = true;
    public ToolGatePolicyRules PolicyRule { get; set; } = ToolGatePolicyRules.Any;
}