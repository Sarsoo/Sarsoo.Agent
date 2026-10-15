namespace Sarsoo.Agent.K8s.IO;

public static class KubeConfig
{
    public static string? GetFilePath()
    {
        if (Environment.GetEnvironmentVariable("KUBECONFIG") is { } fileOverride)
        {
            return File.Exists(fileOverride) ? fileOverride : null;
        }
        
        var defaultConfig = Path.Combine(Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile), ".kube", "config");

        return File.Exists(defaultConfig) ? defaultConfig : null;
    }
}