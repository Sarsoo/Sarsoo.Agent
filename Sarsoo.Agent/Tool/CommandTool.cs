using System.Text.Json;
using CliWrap;
using CliWrap.Buffered;
using Microsoft.Extensions.AI;
using Sarsoo.Agent.Tool.Policy;

namespace Sarsoo.Agent.Tool;

public partial class CommandTool(string command, ICommandPolicy? policy = null, string? descriptionAddition = null)
{
    public async Task<string> Run(List<string> argv, int timeoutSeconds = 10)
    {
        try
        {
            if (policy is not null)
            {
                var policyResult = policy.IsPermitted(argv);
                if (!policyResult.Permitted)
                {
                    if (!string.IsNullOrWhiteSpace(policyResult.Reason))
                    {
                        return
                            $"{command} cannot be run with those arguments, blocked by policy: {policyResult.Reason}";
                    }

                    return $"{command} cannot be run with those arguments, blocked by policy";
                }
            }

            using var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

            var wrapped = await Cli.Wrap(command)
                .WithArguments(argv)
                .WithValidation(CommandResultValidation.None)
                .ExecuteBufferedAsync(cts.Token);

            return $@"
<command>
  <output>
    <standard-output>
{wrapped.StandardOutput}
    </standard-output>
    <standard-error>
{wrapped.StandardError}
    </standard-error>
  </output>

  <exit-code>{wrapped.ExitCode}</exit-code>
  <execution-time>{wrapped.RunTime}</execution-time>
</command>
";
        }
        catch (OperationCanceledException e)
        {
            return $@"
<command>
  <error>Tool timed-out during execution (timeout set to {timeoutSeconds}s)</error>
</command>
";
        }
        catch (Exception e)
        {
            return $@"
<command>
  <error>Tool failed to execute</error>
  <exception>
{e.Message}
  </exception>
</command>
";
        }
    }

    public AIFunction AsFunction()
    {
        var options = new JsonSerializerOptions(AIJsonUtilities.DefaultOptions);
        options.TypeInfoResolverChain.Insert(0, ToolJsonContext.Default);

        var description = $"Run a single {command} command (no shell) with arbitrary arguments";
        if (!string.IsNullOrEmpty(descriptionAddition))
        {
            description += $", {descriptionAddition}";
        }

        return AIFunctionFactory.Create(Run, name: command, description: description, options);
    }
}