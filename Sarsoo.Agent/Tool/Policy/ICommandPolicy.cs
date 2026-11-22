namespace Sarsoo.Agent.Tool.Policy;

public record struct CommandValidationResult(bool Permitted, string? Reason);

public interface ICommandPolicy
{
    CommandValidationResult IsPermitted(List<string> argv);
}