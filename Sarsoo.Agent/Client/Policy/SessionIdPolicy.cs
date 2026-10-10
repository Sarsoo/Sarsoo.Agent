using System.ClientModel.Primitives;

namespace Sarsoo.Agent.Client.Policy;

public class SessionIdPolicy(string sessionId): PipelinePolicy
{
    public override void Process(PipelineMessage message, IReadOnlyList<PipelinePolicy> pipeline, int currentIndex)
    {
        Stamp(message);
        ProcessNext(message, pipeline, currentIndex);
    }

    public override async ValueTask ProcessAsync(PipelineMessage message, IReadOnlyList<PipelinePolicy> pipeline, int currentIndex)
    {
        Stamp(message);
        await ProcessNextAsync(message, pipeline, currentIndex).ConfigureAwait(false);
    }

    private void Stamp(PipelineMessage message)
    {
        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            message.Request.Headers.Set("x-session-id", sessionId);
        }
    }
}
