using System.Collections.Concurrent;
using k8s;
using k8s.KubeConfigModels;
using Microsoft.Extensions.Logging;
using Sarsoo.Agent.K8s.IO;

namespace Sarsoo.Agent.K8s;

public class ClientIndex
{
    private readonly ILogger<ClientIndex> _logger;
    private K8SConfiguration? _config = null;
    
    public IEnumerable<string>? AvailableContextNames => _config?.Contexts.Select(x => x.Name);
    private readonly ConcurrentDictionary<string, IKubernetes> _clients = new();

    public ClientIndex(ILogger<ClientIndex> logger)
    {
        _logger = logger;
        LoadFile();
    }

    public void LoadFile()
    {
        _config = KubernetesClientConfiguration.LoadKubeConfig(KubeConfig.GetFilePath());
    }
    
    public async Task LoadFileAsync()
    {
        _config = await KubernetesClientConfiguration.LoadKubeConfigAsync(KubeConfig.GetFilePath());
    }

    public IKubernetes? GetClient(string contextName)
    {
        try
        {
            if (_clients.TryGetValue(contextName, out var client))
            {
                return client;
            }

            if (_config is not null && (AvailableContextNames?.Contains(contextName) ?? false))
            {
                var c = _clients.GetOrAdd(contextName,
                    static (c, f) => new Kubernetes(KubernetesClientConfiguration.BuildConfigFromConfigObject(f, c)),
                    _config);

                return c;
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to load Kubernetes client {contextName}", contextName);
        }

        return null;
    }
}