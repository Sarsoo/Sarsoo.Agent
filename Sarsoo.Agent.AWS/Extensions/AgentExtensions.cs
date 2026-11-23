using Microsoft.Extensions.DependencyInjection;
using Sarsoo.Agent.AWS.Context;
using Sarsoo.Agent.Composition;
using Sarsoo.Agent.Context;
using Sarsoo.Agent.Extensions;
using Sarsoo.Agent.Tool.Policy;

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

        public AgentBuilder TryWithReadOnlyAwsCommandTool()
        {
            builder.TryWithCommandTool("aws",
                static () => new ReadOnlyAwsCommandPolicy(),
                (ab) =>
                {
                    ab.WithSystemPrompt("You have READ-ONLY access to the `aws` command. You can provide arbitrary argv to this command but policy will reject any combinations that constitute a write command.");
                    ab.WithAwsContextProvider();
                }
            );

            return builder;
        }
    }
}