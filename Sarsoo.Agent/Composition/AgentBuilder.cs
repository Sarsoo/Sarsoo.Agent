using System.Text;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Tools.Shell;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Sarsoo.Agent.Context;
using Sarsoo.Agent.Session;

namespace Sarsoo.Agent.Composition;

public class AgentBuilder
{
    private readonly IChatClient _client;
    private readonly ILoggerFactory _loggerFactory;
    
    private readonly IServiceProvider? _serviceProvider;
    
    private List<string> _systemPrompts = new();
    private List<AIContextProvider> _contextProviders = new();
    private List<AITool> _tools = new();
    
    public AgentBuilder(
        IChatClient client,
        ILoggerFactory loggerFactory,
        IServiceProvider? serviceProvider = null)
    {
        _client = client;
        _loggerFactory = loggerFactory;
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
        _contextProviders.Add(new AgentsMdProvider(_loggerFactory.CreateLogger<AgentsMdProvider>()));
        
        return this;
    }
    
    public AgentBuilder WithBashShell(bool contextProvider = true)
    {
        var shell = new LocalShellExecutor(new()
        {
            Mode = ShellMode.Persistent,
            AcknowledgeUnsafe = true,
            CleanEnvironment = true
        });
        var shellEnvironment = contextProvider ? new ShellEnvironmentProvider(shell) : null;
        
        return WithShell(shell, shellEnvironment);
    }

    public AgentBuilder WithShell(ShellExecutor executor, ShellEnvironmentProvider? environmentProvider = null)
    {
        if (environmentProvider is not null)
        {
            _contextProviders.Add(environmentProvider);
        }
        _tools.Add(executor.AsAIFunction());
        return this;
    }

    public AgentSessionContext Build()
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
                DisableTodoProvider = true,
                DisableWebSearch = true,
                DisableFileMemory = true,
                DisableAgentModeProvider = true,
            }, 
            loggerFactory: _loggerFactory, 
            services: _serviceProvider);

        var context = new AgentSessionContext(agent);
        context.AddDisposableRange(_contextProviders);
        context.AddDisposableRange(_tools);
        
        return context;
    }
}