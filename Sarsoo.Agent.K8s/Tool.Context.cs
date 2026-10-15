using Microsoft.Extensions.AI;
using Sarsoo.Agent.K8s.IO;
using Sarsoo.Agent.Observability;

namespace Sarsoo.Agent.K8s;

public partial class Tool
{
    private IEnumerable<string> GetContextNames()
    {
        using var trace = Tracing.Source.StartActivity();
        return _index.AvailableContextNames ?? Enumerable.Empty<string>();
    }

    public AIFunction GetContextFunction()
    {
        return AIFunctionFactory.Create(GetContextNames, 
            name: "kubectl_config_get_contexts", 
            description: "Get a list of available kubernetes cluster contexts, empty list if none available",
            K8sJsonContext.Default.Options
        );
    }
}