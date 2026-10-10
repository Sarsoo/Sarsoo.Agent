using Microsoft.Extensions.AI;

namespace Sarsoo.Agent.Session.Init;

public interface IClientResolver
{
    IChatClient GetClient();
}