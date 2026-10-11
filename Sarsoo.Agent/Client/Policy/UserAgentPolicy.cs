using System.ClientModel.Primitives;
using System.Reflection;

namespace Sarsoo.Agent.Client.Policy;

public class UserAgentPolicy(): PipelinePolicy
{
    private static readonly string AssemblyName = Assembly.GetExecutingAssembly().GetName().Name ?? "";
    private static readonly string AssemblyVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "";
    private static readonly string UserAgent = $"{AssemblyName}/{AssemblyVersion}";
    
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
        message.Request.Headers.Set("User-Agent", UserAgent);
    }
}
