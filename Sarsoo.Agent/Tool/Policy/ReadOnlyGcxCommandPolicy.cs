namespace Sarsoo.Agent.Tool.Policy;

public class ReadOnlyGcxCommandPolicy: ICommandPolicy
{
    private static readonly List<List<string>> WhiteListedPatterns = [
        ["docs"],
        ["help"],
        ["providers", "list"],

        ["resources", "get"],
        ["resources", "list-examples"],
        ["resources", "list-types"],
        ["resources", "pull"],
        ["resources", "search"],
        ["resources", "validate"],

        ["logs", "labels"],
        ["logs", "metrics"],
        ["logs", "query"],
        ["logs", "query-patterns"],
        ["logs", "series"],

        ["metrics", "cardinality"],
        ["metrics", "labels"],
        ["metrics", "list-names"],
        ["metrics", "metadata"],
        ["metrics", "query"],
        ["metrics", "search"],
        ["metrics", "series"],

        ["traces", "get"],
        ["traces", "labels"],
        ["traces", "metrics"],
        ["traces", "query"],

        ["dashboards", "get"],
        ["dashboards", "list"],
        ["dashboards", "list-versions"],
        ["dashboards", "search"],

        ["datasources", "athena"],
        ["datasources", "azuremonitor"],
        ["datasources", "bigquery"],
        ["datasources", "cloudwatch"],
        ["datasources", "elasticsearch"],
        ["datasources", "get"],
        ["datasources", "health"],
        ["datasources", "infinity"],
        ["datasources", "list"],
        ["datasources", "loki"],
        ["datasources", "mssql"],
        ["datasources", "mysql"],
        ["datasources", "postgres"],
        ["datasources", "prometheus"],
        ["datasources", "pyroscope"],
        ["datasources", "query"],
        ["datasources", "schemas"],
        ["datasources", "tempo"],

        ["version"],
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
            Reason = "Only readonly subcommands are allowed"
        };
    }
}