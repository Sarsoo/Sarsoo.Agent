using Microsoft.Extensions.DependencyInjection;
using Sarsoo.Agent.AWS.Context;
using Sarsoo.Agent.Composition;

namespace Sarsoo.Agent.AWS.Extensions;

public static class AgentExtensions
{
    extension(AgentBuilder builder)
    {
        public AgentBuilder WithAwsContextProvider()
        {
            builder.WithContextProvider(sp => sp.GetRequiredService<AwsProfileContextProvider>());

            return builder;
        }
    }
}