using Terminal.Gui.Tracing;

namespace Sarsoo.Agent.CLI.Observability;

public class TraceBackend: ITraceBackend
{
    public void Log(TraceEntry entry)
    {
        using var trace = CliTracing.Source.StartActivity(entry.Method);
    }

    public void Clear()
    {
    }
}