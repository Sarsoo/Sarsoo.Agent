using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sarsoo.Agent.Gate;
using Sarsoo.Agent.Integrations;
using Sarsoo.Agent.Session;
using Sarsoo.Agent.Session.Orchestration;

namespace Sarsoo.Agent.Composition;

public interface IAgentFactory
{
    AgentSessionContext CreateSession(string profileName, string name, string? systemPrompt = null);
    AgentSessionRunner CreateRunner(AgentSessionContext session);
}

public sealed class AgentFactory(
    IServiceProvider serviceProvider,
    Func<IChatClient> clientFactory,
    IAgentProfileRegistry profiles,
    IToolGateEnforcer toolGateEnforcer,
    IEnumerable<IStateChangeReceiver> stateChangeReceivers,
    ILoggerFactory loggerFactory
) : IAgentFactory
{
    public AgentSessionContext CreateSession(string profileName, string name, string? systemPrompt = null)
    {
        var profile = profiles.Get(profileName);
        var builder = new AgentBuilder(clientFactory(), loggerFactory, serviceProvider);

        if (!string.IsNullOrWhiteSpace(systemPrompt))
        {
            builder.WithSystemPrompt(systemPrompt);
        }

        return profile.Configure(builder).Build(name);
    }

    public AgentSessionRunner CreateRunner(AgentSessionContext session)
        => new(session, toolGateEnforcer, stateChangeReceivers, loggerFactory.CreateLogger<AgentSessionRunner>());
}
