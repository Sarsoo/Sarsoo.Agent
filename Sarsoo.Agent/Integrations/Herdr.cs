using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Sarsoo.Agent.Extensions;
using Sarsoo.Agent.Observability;
using Sarsoo.Agent.Session;

namespace Sarsoo.Agent.Integrations;

public class Herdr(ILogger<Herdr> logger) : IStateChangeReceiver
{
    public const string HERDR_ENV = "HERDR_ENV";
    public const string HERDR_PANE_ID = "HERDR_PANE_ID";
    public const string HERDR_BIN_PATH = "HERDR_BIN_PATH";

    public bool InHerdr => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(HERDR_ENV));
    public string? PaneId => Environment.GetEnvironmentVariable(HERDR_PANE_ID);
    public string? HerdrExe => Environment.GetEnvironmentVariable(HERDR_BIN_PATH);

    private readonly ConcurrentDictionary<string, int> _seqIds = new();

    public async Task StateChanged(string agentId, AgentState state, CancellationToken token = default)
    {
        using var trace = Tracing.Source.StartActivity();
        if (InHerdr)
        {
            var seqId = _seqIds.AddOrUpdate(agentId, 0, (_, current) => current + 1);

            await RunHerdrCommand(
                $"""pane report-agent "{PaneId}" --source "reeve" --agent "{agentId}" --state "{state.HerdrState}" --seq "{seqId}" """, 
                token
                )
                .ConfigureAwait(false);
        }
    }

    private async Task RunHerdrCommand(string command, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Running Herdr command: {command}", command);
        if (HerdrExe is { } herdrExe)
        {
            if (Environment.ExpandEnvironmentVariables(herdrExe) is { } expandedHerdrExe)
            {
                using var process = new Process();
                process.StartInfo.FileName = expandedHerdrExe;
                process.StartInfo.Arguments = command;

                process.Start();
                await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
                logger.LogInformation("Herdr command exited with code {exitCode}", process.ExitCode);
            }
        }
    }
}