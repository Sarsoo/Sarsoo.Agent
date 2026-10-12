using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sarsoo.Agent.Advisor;
using Sarsoo.Agent.Client;
using Sarsoo.Agent.Gate;
using Sarsoo.Agent.Integrations;
using Sarsoo.Agent.Options;
using Sarsoo.Agent.Session;
using Sarsoo.Agent.Session.Orchestration;

namespace Sarsoo.Agent.Composition;

public interface IAgentFactory
{
    AgentSessionContext CreateSession(
        string profileName,
        string name, 
        string? model = null, 
        ReasoningEffort? thinkingLevel = null,
        Action<ChatOptions>? configureOptions = null,
        string? systemPrompt = null);
    AgentSessionRunner CreateRunner(AgentSessionContext session);
}

public sealed class AgentFactory(
    IServiceProvider serviceProvider,
    IClientResolver clientResolver,
    IAgentProfileRegistry profiles,
    IToolGateEnforcer toolGateEnforcer,
    IEnumerable<IStateChangeReceiver> stateChangeReceivers,
    ILoggerFactory loggerFactory
) : IAgentFactory
{
    public AgentSessionContext CreateSession(
        string profileName,
        string name, 
        string? model = null, 
        ReasoningEffort? thinkingLevel = null,
        Action<ChatOptions>? configureOptions = null,
        string? systemPrompt = null
    )
    {
        var profile = profiles.Get(profileName);
        var builder = new AgentBuilder(clientResolver.GetClient(model, thinkingLevel, configureOptions), loggerFactory, serviceProvider);

        if (!string.IsNullOrWhiteSpace(systemPrompt))
        {
            builder.WithSystemPrompt(systemPrompt);
        }

        builder = profile.Configure(builder);

        return builder.Build(name);
    }

    public AgentSessionRunner CreateRunner(AgentSessionContext session)
        => new(session, toolGateEnforcer, stateChangeReceivers, loggerFactory.CreateLogger<AgentSessionRunner>());
}
