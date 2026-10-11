using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sarsoo.Agent.Client;
using Sarsoo.Agent.Composition;
using Sarsoo.Agent.Options;

namespace Sarsoo.Agent.Advisor;

public interface IAdvisorFactory
{
    AIFunction CreateTool();
    AIContextProvider CreateContext();
}

public class AdvisorFactory(
    IClientResolver clientResolver,
    IOptions<AdvisorOptions> options,
    ILoggerFactory loggerFactory,
    IServiceProvider serviceProvider
) : IAdvisorFactory
{
    private readonly AdvisorSharedContext _sharedContext = new();

    public AIFunction CreateTool()
    {
        var advisor = options.Value;
        var client = clientResolver.GetClient(advisor.Model, advisor.ThinkingLevel);

        var agent = new AgentBuilder(client, loggerFactory, serviceProvider)
            .WithSystemPrompt(advisor.SystemPrompt ?? AdvisorOptions.DefaultSystemPrompt)
            .BuildAgent();
        
        var func = new AdvisorFunction(agent, _sharedContext);

        return AIFunctionFactory.Create(
            func.GetAdvice, 
            name: options.Value.ToolName, 
            description: advisor.ToolDescription ?? AdvisorOptions.DefaultToolDescription
        );
    }

    public AIContextProvider CreateContext() => new AdvisorContextProvider(_sharedContext);
}