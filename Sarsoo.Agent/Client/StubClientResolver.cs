using Microsoft.Extensions.AI;

namespace Sarsoo.Agent.Client;

public sealed class StubClientResolver : IClientResolver
{
    public IChatClient GetClient(
        string? model = null,
        ReasoningEffort? thinkingLevel = null,
        Action<ChatOptions>? configureOptions = null
    )
        => new StubClient();
}
