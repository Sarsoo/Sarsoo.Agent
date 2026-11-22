using System.Text;
using k8s;
using k8s.KubeConfigModels;
using Microsoft.Agents.AI;
using Microsoft.Extensions.Logging;
using Sarsoo.Agent.K8s.IO;
using Sarsoo.Agent.Observability;

namespace Sarsoo.Agent.K8s.Context;

public class K8sClusterContextProvider : AIContextProvider
{
    private class K8sClusterState
    {
        public string? ClusterContext { get; set; }
        private bool _initialized;
        private K8SConfiguration? _config;

        public async Task<string?> GetClusterContext(ILogger<K8sClusterContextProvider>? logger,
            CancellationToken cancellationToken = default)
        {
            if (!_initialized)
            {
                await Initialize(logger, cancellationToken);
            }

            return ClusterContext;
        }
        
        private async Task Initialize(ILogger<K8sClusterContextProvider>? logger, CancellationToken cancellationToken = default)
        {
            using var trace = Tracing.Source.StartActivity($"{nameof(K8sClusterContextProvider)}::{nameof(Initialize)}");
            var instructionBuilder = new StringBuilder();

            if (KubeConfig.GetFilePath() is not null)
            {
                try
                {
                    _config = await KubernetesClientConfiguration.LoadKubeConfigAsync();

                    instructionBuilder.AppendLine("This machine has the following Kubernetes contexts configured:");

                    instructionBuilder.AppendLine("<available_kubernetes_clusters>");
                    foreach (var context in _config.Contexts)
                    {
                        if (_config.CurrentContext.Equals(context.Name))
                        {
                            instructionBuilder.AppendLine("  <context current=\"true\">");
                        }
                        else
                        {
                            instructionBuilder.AppendLine("  <context>");
                        }
                        instructionBuilder.Append    ("    <name>").Append(context.Name).AppendLine("</name>");
                        instructionBuilder.AppendLine("    <cluster>");
                        instructionBuilder.Append    ("      <name>").Append(context.ContextDetails.Cluster).AppendLine("</name>");
                        instructionBuilder.AppendLine("    </cluster>");
                        instructionBuilder.AppendLine("  </context>");
                    }
                    instructionBuilder.AppendLine("</available_kubernetes_clusters>");
                }
                catch (Exception e)
                {
                    logger?.LogError(e, "Error loading Kubernetes config");
                    instructionBuilder.AppendLine("There was an error loading the Kubernetes config file on this machine. `kubectl` commands may be non-functional");
                }
            }
            else
            {
                instructionBuilder.AppendLine("A Kubernetes config file could not be found on this machine, do not assume that `kubectl` commands will work.");
            }

            if (instructionBuilder.Length > 0)
            {
                ClusterContext = instructionBuilder.ToString();
            }
            
            _initialized = true;
        }
    }
    
    private readonly ProviderSessionState<K8sClusterState> _sessionState;
    private readonly ILogger<K8sClusterContextProvider>? _logger;

    public K8sClusterContextProvider(ILogger<K8sClusterContextProvider>? logger = null)
    {
        _logger = logger;
        _sessionState = new ProviderSessionState<K8sClusterState>(
            _ => new K8sClusterState(),
            GetType().Name
        );
    }

    protected override async ValueTask<AIContext> ProvideAIContextAsync(InvokingContext context, CancellationToken cancellationToken = default)
    {
        using var trace = Tracing.Source.StartActivity($"{nameof(K8sClusterContextProvider)}::{nameof(ProvideAIContextAsync)}");
        var state = _sessionState.GetOrInitializeState(context.Session);
        
        var retContext = new AIContext
        {
            Instructions = await state.GetClusterContext(_logger, cancellationToken)
        };

        return retContext;
    }
}