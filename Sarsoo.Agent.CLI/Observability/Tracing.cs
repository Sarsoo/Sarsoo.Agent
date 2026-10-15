using System.Diagnostics;

namespace Sarsoo.Agent.CLI.Observability;

public static class CliTracing
{
    public static ActivitySource Source = new("Sarsoo.Agent.CLI");
}