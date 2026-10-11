using System.ClientModel;
using System.ClientModel.Primitives;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using OpenAI;
using Sarsoo.Agent.Client.Policy;
using Sarsoo.Agent.Options;
using UserAgentPolicy = Sarsoo.Agent.Client.Policy.UserAgentPolicy;

namespace Sarsoo.Agent.Client;

public class OpenAIClientResolver(IOptions<ProviderOptions> options) : IClientResolver
{
    public IChatClient GetClient(Action<ChatOptions>? configureOptions = null)
    {
        var sessionId = Guid.NewGuid().ToString();
        var creds = new ApiKeyCredential(options.Value.Key!);
        var chatOptions = new OpenAIClientOptions()
        {
            Endpoint = new(options.Value.Url!)
        };
        
        chatOptions.AddPolicy(new SessionIdPolicy(sessionId), PipelinePosition.PerCall);
        chatOptions.AddPolicy(new UserAgentPolicy(), PipelinePosition.PerCall);

        var providerClient = new OpenAIClient(creds, chatOptions)
            .GetChatClient(options.Value.Model!)
            .AsIChatClient()
            .AsBuilder()
            .ConfigureOptions(o =>
            {
                o.AllowMultipleToolCalls = true;
                o.AdditionalProperties = new()
                {
                    { "prompt_cache_key", sessionId },
                    { "prompt_cache_options", new Dictionary<string, string>()
                        {
                            {"mode", "implicit"},
                            {"ttl", "30m"}
                        }   
                    }
                };
                
                configureOptions?.Invoke(o);
            });

        return providerClient.Build();
    }
}