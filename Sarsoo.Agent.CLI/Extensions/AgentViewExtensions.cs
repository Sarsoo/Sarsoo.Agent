using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Sarsoo.Agent.CLI.Observability;
using Sarsoo.Agent.CLI.Session.TUI;
using Sarsoo.Agent.CLI.UI.TUI.Chat.Message;
using Sarsoo.Agent.Composition;

namespace Sarsoo.Agent.CLI.Extensions;

public static class AgentViewExtensions
{
    extension(IAgentFactory factory)
    {
        public AgentTab CreateAgentView(ILoggerFactory loggerFactory, string profile, string name,
            string? model = null, ReasoningEffort? thinkingLevel = null,
            Action<ChatOptions>? configureOptions = null,
            string? systemPrompt = null)
        {
            using var trace = CliTracing.Source.StartActivity($"{nameof(AgentWorkspace)}::{nameof(CreateAgentView)}");
            var session = factory.CreateSession(
                profile,
                name,
                model: model,
                thinkingLevel: thinkingLevel,
                configureOptions: configureOptions,
                systemPrompt: systemPrompt
            );
            var runner = factory.CreateRunner(session);
            var messages = new MessageView(loggerFactory.CreateLogger<MessageView>());
            var stats = new TokenSessionStats(session);

            return new AgentTab(runner, messages, stats, loggerFactory.CreateLogger<AgentTab>());
        }
    }
}