using Microsoft.Extensions.AI;

namespace Sarsoo.Agent.Client;

public interface IClientResolver
{
    IChatClient GetClient(Action<ChatOptions>? configureOptions = null);
}