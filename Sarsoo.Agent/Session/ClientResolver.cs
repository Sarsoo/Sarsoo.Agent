using System.ClientModel;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using OpenAI;
using Sarsoo.Agent.Options;

namespace Sarsoo.Agent.Session;

public class ClientResolver(IOptions<ProviderOptions> options)
{
    public IChatClient GetClient()
    {
        var client = new OpenAIClient(new ApiKeyCredential(options.Value.Key!), new OpenAIClientOptions()
            {
                Endpoint = new(options.Value.Url!)
            })
            .GetChatClient(options.Value.Model!)
            .AsIChatClient();

        return client;
    }
}