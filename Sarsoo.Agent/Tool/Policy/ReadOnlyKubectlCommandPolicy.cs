namespace Sarsoo.Agent.Tool.Policy;

public class ReadOnlyKubectlCommandPolicy: ICommandPolicy
{
    private static readonly List<List<string>> WhiteListedPatterns = [
        ["version"],

        ["get"],
        ["events"],
        ["logs"],
        ["describe"],
        ["top"],
        ["cluster-info"],

        ["wait"],

        ["api-resources"],
        ["api-versions"],

        ["config", "current-context"],
        ["config", "get-clusters"],
        ["config", "get-contexts"],
        ["config", "get-users"],
        ["config", "view"],
    ];

    public CommandValidationResult IsPermitted(List<string> argv)
    {
        if (VerbPolicy.VerbSetIsAnyOf(argv, WhiteListedPatterns))
        {
            return new()
            {
                Permitted = true
            };
        }

        return new()
        {
            Permitted = false,
            Reason = "Only readonly subcommands are allowed [get]"
        };
    }
}