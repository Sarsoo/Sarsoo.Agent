using Microsoft.Agents.AI;

namespace Sarsoo.Agent.Advisor;

public class AdvisorContextProvider(AdvisorSharedContext sharedContext): AIContextProvider
{
    protected override ValueTask StoreAIContextAsync(InvokedContext context, CancellationToken cancellationToken = default)
    {
        var window = context.RequestMessages
            .Concat(context.ResponseMessages ?? [])
            .ToList();
        
        sharedContext.SetContext(window);
        return default;
    }
}