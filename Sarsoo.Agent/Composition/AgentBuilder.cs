using System.Text;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Tools.Shell;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sarsoo.Agent.Advisor;
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

    private string? _name = null;
    private string? _id = null;
    private string? _description = null;
    
    private string? _modelId = null;
    private string? _conversationId = null;
    private bool? _multipleToolCalls = null;
    
    private ReasoningEffort? _reasoningEffort = null;
    private ReasoningOutput? _reasoningFormat = null;
    
    private ChatToolMode? _toolMode = null;
    
    public AgentBuilder(
        IChatClient client,
        ILoggerFactory? loggerFactory = null,
        IServiceProvider? serviceProvider = null)
    {
        _client = client;
        _loggerFactory = loggerFactory ?? serviceProvider?.GetRequiredService<ILoggerFactory>();
        _serviceProvider = serviceProvider;
    }

    public AgentBuilder WithId(string id)
    {
        _id = id;
        return this;
    }

    public AgentBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public AgentBuilder WithDescription(string description)
    {
        _description = description;
        return this;
    }

    public AgentBuilder WithModel(string modelId)
    {
        _modelId = modelId;
        return this;
    }

    public AgentBuilder WithConversationId(string id)
    {
        _conversationId = id;
        return this;
    }

    public AgentBuilder WithMultipleToolCalls(bool? allowed = true)
    {
        _multipleToolCalls = allowed;
        return this;
    }

    public AgentBuilder WithReasoningEffort(ReasoningEffort effort)
    {
        _reasoningEffort = effort;
        return this;
    }

    public AgentBuilder WithReasoningFormat(ReasoningOutput format)
    {
        _reasoningFormat = format;
        return this;
    }

    public AgentBuilder WithToolMode(ChatToolMode mode)
    {
        _toolMode = mode;
        return this;
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
                ProbeTools = ["git", "dotnet", "node", "python3", "docker", "podman"],
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

    public AgentBuilder WithAdvisor()
    {
        if (_serviceProvider is not null)
        {
            var advisorFactory = _serviceProvider.GetRequiredService<IAdvisorFactory>();
            _tools.Add(advisorFactory.CreateTool());
            _contextProviders.Add(advisorFactory.CreateContext());
        }
        else
        {
            throw new InvalidOperationException("Service provider not provided to resolve Advisor resources");
        }
        
        return this;
    }
    
    /// <summary>
    ///     Builds the underlying <see cref="AIAgent"/> without wrapping it in a session context. Used
    ///     for sub-agents (such as the advisor) that are embedded rather than run as a tab.
    /// </summary>
    public AIAgent BuildAgent()
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

        ReasoningOptions? reasoning = null;
        if ((_reasoningEffort, _reasoningFormat) is not (null, null))
        {
            reasoning = new()
            {
                Effort = _reasoningEffort,
                Output = _reasoningFormat
            };
        }
        
        return _client.AsHarnessAgent(options: new()
            {
                Id = _id,
                Name = _name,
                Description = _description,
                HarnessInstructions = systemPrompt,
                AIContextProviders = _contextProviders.Any() ? _contextProviders : null,
                ChatOptions = new()
                {
                    Tools = _tools.Any() ? _tools : null,
                    ModelId = _modelId,
                    ConversationId = _conversationId,
                    AllowMultipleToolCalls = _multipleToolCalls,
                    Reasoning = reasoning,
                    ToolMode = _toolMode
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
    }

    public AgentSessionContext Build(string name)
    {
        var context = new AgentSessionContext(BuildAgent(), name);
        context.AddDisposableRange(_contextProviders);
        context.AddDisposableRange(_tools);
        
        return context;
    }
}