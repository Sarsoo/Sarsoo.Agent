namespace Sarsoo.Agent.Tool.Policy;

public class ReadOnlyAwsCommandPolicy: ICommandPolicy
{
    private static readonly List<string> ReadOnlyVerbs =
    [
        "get-",
        "describe-",
        "list-"
    ];

    private static readonly List<string> BlacklistedSubcommands =
    [
        "login",
        "logout",
        "secretsmanager"
    ];

    public CommandValidationResult IsPermitted(List<string> argv)
    {
        if (argv.Count == 0)
        {
            return new()
            {
                Permitted = true
            };
        }
        else if (argv.Count == 1)
        {
            if (BlacklistedSubcommands.Contains(argv[0], StringComparer.OrdinalIgnoreCase))
            {
                return new()
                {
                    Permitted = false,
                    Reason = "Only readonly subcommands are allowed"
                };
            }

            return new()
            {
                Permitted = true
            };
        }
        else if (argv.Count == 2)
        {
            if (argv.Where(x => !string.IsNullOrWhiteSpace(x)).Skip(1).FirstOrDefault() is { } subcommand)
            {
                if (ReadOnlyVerbs.Any(x => subcommand.StartsWith(x, StringComparison.OrdinalIgnoreCase)))
                {
                    return new()
                    {
                        Permitted = true
                    };
                }
            }
        }

        return new()
        {
            Permitted = false,
            Reason = "Only readonly subcommands are allowed"
        };
    }
}