using Microsoft.Agents.AI;
using Sarsoo.Agent.Observability;

namespace Sarsoo.Agent.Advisor;

public class AdvisorContextProvider(AdvisorSharedContext sharedContext): AIContextProvider
{
    protected override ValueTask StoreAIContextAsync(InvokedContext context, CancellationToken cancellationToken = default)
    {
        using var trace = Tracing.Source.StartActivity($"{nameof(AdvisorContextProvider)}::{nameof(StoreAIContextAsync)}");
        var window = context.RequestMessages
            .Concat(context.ResponseMessages ?? [])
            .ToList();
        
        sharedContext.SetContext(window);
        return default;
    }
}