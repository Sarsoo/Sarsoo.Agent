using System.Text;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Tools.Shell;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sarsoo.Agent.Context;
using Sarsoo.Agent.Session;
#pragma warning disable MAAI001

namespace Sarsoo.Agent.Composition;

public class AgentBuilder
{
    private readonly IChatClient _client;
    private readonly ILoggerFactory? _loggerFactory;
    
    private readonly IServiceProvider? _serviceProvider;
    
    private readonly List<string> _systemPrompts = new();
    private readonly List<AIContextProvider> _contextProviders = new();
    private readonly List<AITool> _tools = new();
    private readonly List<Func<ToolAutoApprovalRuleContext, ValueTask<bool>>> _autoApprovalRules = new();

    private AgentFileStore? _fileStore = null;
    private FileAccessProviderOptions? _fileAccessOptions = null;
    
    public AgentBuilder(
        IChatClient client,
        ILoggerFactory? loggerFactory = null,
        IServiceProvider? serviceProvider = null)
    {
        _client = client;
        _loggerFactory = loggerFactory ?? serviceProvider?.GetRequiredService<ILoggerFactory>();
        _serviceProvider = serviceProvider;
    }

    public AgentBuilder WithSystemPrompt(string prompt)
    {
        _systemPrompts.Add(prompt);
        return this;
    }

    public AgentBuilder WithContextProvider(AIContextProvider contextProvider)
    {
        _contextProviders.Add(contextProvider);
        return this;
    }

    public AgentBuilder WithAgentsMd()
    {
        _contextProviders.Add(new AgentsMdProvider(_loggerFactory?.CreateLogger<AgentsMdProvider>()));
        
        return this;
    }

    private string AddSkillsSubDirectory(string path) => Path.Combine(path, ".agents", "skills");

    public AgentBuilder WithSkills(bool globalSkills = true, bool projectSkills = true, Func<ToolAutoApprovalRuleContext, ValueTask<bool>>? autoApproval = null)
    {
        var skillsPaths = new List<string>();

        if (globalSkills)
        {
            skillsPaths.Add(AddSkillsSubDirectory(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)));
        }
        
        if (projectSkills)
        {
            skillsPaths.Add(AddSkillsSubDirectory(Directory.GetCurrentDirectory()));
        }
        
        var skillsProvider = new AgentSkillsProvider(
            skillPaths: skillsPaths,
            loggerFactory: _loggerFactory
        );
        
        _autoApprovalRules.Add(autoApproval ?? AgentSkillsProvider.ReadOnlyToolsAutoApprovalRule);
        
        _contextProviders.Add(skillsProvider);
        
        return this;
    }

    public AgentBuilder WithFileSystem()
    {
        _fileStore = new FileSystemAgentFileStore(Directory.GetCurrentDirectory());
        _fileAccessOptions = new()
        {
            DisableReadOnlyToolApproval = true
        };
        return this;
    }
    
    public AgentBuilder WithBashShell(bool contextProvider = true, bool requireApproval = true)
    {
        var shell = new LocalShellExecutor(new()
        {
            Mode = ShellMode.Persistent,
            AcknowledgeUnsafe = true,
            CleanEnvironment = true
        });
        
        if(contextProvider)
        {
            _contextProviders.Add(new ShellEnvironmentProvider(shell, new()
            {
                ProbeTools = ["git", "dotnet", "node", "python", "python3", "docker", "podman"],
                OverrideFamily = ShellFamily.Posix
            }));
        }
        
        return WithFunction(shell.AsAIFunction(
                name: "bash", 
                description: "Run a bash command", 
                requireApproval: requireApproval
            )
        );
    }

    public AgentBuilder WithFunction(AIFunction func)
    {
        _tools.Add(func);
        return this;
    }
    
    public AgentSessionContext Build(string name)
    {
        string? systemPrompt = null;

        if (_systemPrompts.Any())
        {
            var builder = new StringBuilder();

            foreach (var prompt in _systemPrompts)
            {
                builder.AppendLine(prompt);
            }
            
            systemPrompt = builder.ToString();
        }
        
        var agent = _client.AsHarnessAgent(options: new()
            {
                HarnessInstructions = systemPrompt,
                AIContextProviders = _contextProviders.Any() ? _contextProviders : null,
                ChatOptions = new()
                {
                    Tools = _tools.Any() ? _tools : null
                },
                ToolApprovalAgentOptions = new()
                {
                    AutoApprovalRules = _autoApprovalRules.Any() ? _autoApprovalRules : null
                },
                
                DisableAgentSkillsProvider = true,
                DisableTodoProvider = true,
                DisableWebSearch = true,
                DisableFileMemory = true,
                DisableAgentModeProvider = true,
                
                FileAccessStore = _fileStore,
                FileAccessProviderOptions = _fileAccessOptions
            }, 
            loggerFactory: _loggerFactory, 
            services: _serviceProvider
        );

        var context = new AgentSessionContext(agent, name);
        context.AddDisposableRange(_contextProviders);
        context.AddDisposableRange(_tools);
        
        return context;
    }
}