using System.Text;
using Microsoft.Extensions.Logging;
using Sarsoo.Agent.Extensions;
using Sarsoo.Agent.Observability;
using Sarsoo.Agent.Session;

namespace Sarsoo.Agent.Integrations;

public class Osc7501(ILogger<Herdr> logger) : IStateChangeReceiver
{
    private const string START_SEQ = "\e]7501;";
    private const string END_SEQ = "\e\\";

    private static readonly bool On = !Console.IsOutputRedirected
                              && Environment.GetEnvironmentVariable("NO_PROGRAM_STATUS") is null;

    public static void Report(string state, string? app = null, string? id = null,
        string? kind = null, int? progress = null, string? msg = null)
    {
        if (!On) return;

        var sb = new StringBuilder(384).Append(START_SEQ).Append("state=").Append(state);
        if (id is not null)       sb.Append(":id=").Append(id);
        if (app is not null)      sb.Append(":app=").Append(app);
        if (kind is not null)     sb.Append(":kind=").Append(kind);
        if (progress is not null) sb.Append(":progress=").Append(Math.Clamp(progress.Value, 0, 100));
        if (msg is not null)      sb.Append(":msg=").Append(B64(msg));
        sb.Append(END_SEQ);

        var s = sb.ToString();
        if (s.Length > 4096) return;              // discard rather than get rejected
        Console.Out.Write(s);                     // one write, no newline
        Console.Out.Flush();
    }

    private static string B64(string s) => Convert.ToBase64String(Encoding.UTF8.GetBytes(
        new string(s.Where(c => !char.IsControl(c)).ToArray()).Trim()));

    public Task StateChanged(string agentId, AgentState state, CancellationToken token = default)
    {
        using var trace = Tracing.Source.StartActivity();

        Report(state.HerdrState, app: "reeve");

        return Task.CompletedTask;
    }
}