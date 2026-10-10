using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Sarsoo.Agent.Extensions;
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

    private int _seqId = 0;

    public async Task StateChanged(AgentState state, CancellationToken token = default)
    {
        if (InHerdr)
        {
            await RunHerdrCommand(
                $"""pane report-agent "{PaneId}" --source "reeve" --agent "reeve" --state "{state.HerdrState}" --seq "{_seqId++}" """, 
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