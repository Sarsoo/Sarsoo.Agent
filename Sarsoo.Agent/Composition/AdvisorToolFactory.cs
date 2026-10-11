using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sarsoo.Agent.Client;
using Sarsoo.Agent.Options;

namespace Sarsoo.Agent.Composition;

public interface IAdvisorToolFactory
{
    AIFunction Create();
}

/// <summary>
///     Builds the advisor agent-as-tool: a tool-less <c>AIAgent</c> on its own client (model and
///     thinking level from <see cref="AdvisorOptions"/>), exposed to the calling agent as a function.
/// </summary>
public sealed class AdvisorToolFactory(
    IClientResolver clientResolver,
    IOptions<AdvisorOptions> options,
    ILoggerFactory loggerFactory,
    IServiceProvider serviceProvider
) : IAdvisorToolFactory
{
    public AIFunction Create()
    {
        var advisor = options.Value;
        var client = clientResolver.GetClient(advisor.Model, advisor.ThinkingLevel);

        var advisorAgent = new AgentBuilder(client, loggerFactory, serviceProvider)
            .WithSystemPrompt(advisor.SystemPrompt ?? AdvisorOptions.DefaultSystemPrompt)
            .BuildAgent();

        return advisorAgent.AsAIFunction(new AIFunctionFactoryOptions
        {
            Name = advisor.ToolName,
            Description = advisor.ToolDescription ?? AdvisorOptions.DefaultToolDescription
        });
    }
}
