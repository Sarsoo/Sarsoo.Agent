using Microsoft.Extensions.AI;

namespace Sarsoo.Agent.Client;

public interface IClientResolver
{
    IChatClient GetClient(
        string? model = null,
        ReasoningEffort? thinkingLevel = null,
        Action<ChatOptions>? configureOptions = null
    );
}
