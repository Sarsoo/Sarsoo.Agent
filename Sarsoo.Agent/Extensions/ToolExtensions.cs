using Sarsoo.Agent.Composition;
using Sarsoo.Agent.Session;
using Sarsoo.Agent.Tool;
using Sarsoo.Agent.Tool.Policy;

namespace Sarsoo.Agent.Extensions;

public static class ToolExtensions
{
    extension(AgentBuilder builder)
    {
        public AgentBuilder TryWithCommandTool(
            string command,
            Func<ICommandPolicy?>? policy = null,
            Action<AgentBuilder>? onInclusion = null
        )
        {
            if (ExeResolution.ExeOnPath(command))
            {
                ICommandPolicy? policyInstance = policy?.Invoke();
                builder.WithFunction(new CommandTool(command, policyInstance).AsFunction());
                onInclusion?.Invoke(builder);
            }
            else
            {
                builder.WithSystemPrompt($"A tool for you to run `{command}` should be included in your harness but has been excluded because the `{command}` command could not be found on the PATH. Should your work require this tool, flag this to the user for remediation.");
            }

            return builder;
        }
    }
}