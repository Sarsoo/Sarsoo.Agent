using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Sarsoo.Agent.CLI.Observability;

public static class CliMeter
{
    public static Meter Meter = new("Sarsoo.Agent.CLI");
}