namespace Sarsoo.Agent.IO;

public static class Config
{
    public static string GetConfigDir()
    {
        if (Environment.GetEnvironmentVariable("REEVES_CONFIG_DIR") is { } reevesConfigDir)
        {
            return reevesConfigDir;
        }
        
        string? baseDir = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
        
        if (baseDir is null)
        {
            var home = Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile);
            
            if (home is { } h)
            {
                baseDir = Path.Join(h, ".config");
            }
        }

        baseDir ??= "/etc";
        
        baseDir = Path.Combine(baseDir, "reeve");
        
        return baseDir;
    }
    
    public static string GetConfigFile()
    {
        return Path.Combine(GetConfigDir(), "config.json");
    }
    
    public static void EnsureConfigDir()
    {
        var config = GetConfigDir();
        if (!Path.Exists(config))
        {
            Directory.CreateDirectory(config);
        }
    }
}