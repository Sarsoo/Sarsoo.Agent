using System.Text.Json;
using CliWrap;
using CliWrap.Buffered;
using Microsoft.Extensions.AI;

namespace Sarsoo.Agent.Tool;

public class CommandTool
{
    private readonly string _command;
    private readonly ICommandPolicy? _policy;

    public CommandTool(string command, ICommandPolicy? policy = null)
    {
        _command = command;
        _policy = policy;
    }

    public async Task<string> Run(List<string> argv)
    {
        if (_policy is not null)
        {
            var policyResult = _policy.IsPermitted(argv);
            if (!policyResult.Permitted)
            {
                if (!string.IsNullOrWhiteSpace(policyResult.Reason))
                {
                    return $"{_command} cannot be run with those arguments, blocked by policy: {policyResult.Reason}";
                }

                return $"{_command} cannot be run with those arguments, blocked by policy";
            }
        }

        var wrapped = await Cli.Wrap(_command)
            .WithArguments(argv)
            .WithValidation(CommandResultValidation.None)
            .ExecuteBufferedAsync();

        var result = $@"
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

        return result;
    }

    public AIFunction AsFunction()
    {
        var options = new JsonSerializerOptions(AIJsonUtilities.DefaultOptions);
        options.TypeInfoResolverChain.Insert(0, ToolJsonContext.Default);
        return AIFunctionFactory.Create(Run, name: _command, description: $"Run a single {_command} command (no shell) with arbitrary arguments", options);
    }
}