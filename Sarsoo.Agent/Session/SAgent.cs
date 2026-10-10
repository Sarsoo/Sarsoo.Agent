using System.Text.Json;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Tools.Shell;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace Sarsoo.Agent.Session;

public class SAgent(
    ILogger<SAgent> logger,
    IChatClient client, 
    ILoggerFactory loggerFactory, 
    IServiceProvider serviceProvider
): AIAgent, IAsyncDisposable
{
    private ShellExecutor? _shell;
    private ShellEnvironmentProvider? _shellEnvironment;
    
    private HarnessAgent? _harness;
    
    private string? _harnessPrompt = null;
    private string? _chatPrompt = null;
    
    public HarnessAgent Harness => _harness ?? throw new InvalidOperationException("Agent not initialized");
    
    public void Init()
    {
        logger.LogInformation("Initializing agent");
        
        _shell = new LocalShellExecutor(new() { Mode = ShellMode.Persistent, AcknowledgeUnsafe = true });
        _shellEnvironment = new ShellEnvironmentProvider(_shell);
        
        _harness = client.AsHarnessAgent(options: new()
        {
            HarnessInstructions = _harnessPrompt,
            AIContextProviders = [_shellEnvironment],
            ChatOptions = new()
            {
                Instructions = _chatPrompt,
                Tools = [_shell.AsAIFunction(name: "bash", description: "Execute a bash command", requireApproval: false)]
            },
            DisableTodoProvider = true,
            DisableWebSearch = true,
            DisableFileMemory = true,
            DisableAgentModeProvider = true,
        }, 
            loggerFactory: loggerFactory, 
            services: serviceProvider);
    }

    public SAgent WithHarnessPrompt(string prompt)
    {
        _harnessPrompt = prompt;
        return this;
    }

    public SAgent WithChatPrompt(string prompt)
    {
        _chatPrompt = prompt;
        return this;
    }

    protected override Task<AgentResponse> RunCoreAsync(IEnumerable<ChatMessage> messages, AgentSession? session = null, AgentRunOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Running agent");
        if (_harness is null)
        {
            Init();
        }
        
        return Harness.RunAsync(messages: messages, session: session, options: options, cancellationToken: cancellationToken);
    }

    protected override IAsyncEnumerable<AgentResponseUpdate> RunCoreStreamingAsync(IEnumerable<ChatMessage> messages, AgentSession? session = null,
        AgentRunOptions? options = null, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Running agent");
        if (_harness is null)
        {
            Init();
        }
        
        return Harness.RunStreamingAsync(messages, session, options, cancellationToken);
    }

    protected override ValueTask<AgentSession> CreateSessionCoreAsync(CancellationToken cancellationToken = default) => Harness.CreateSessionAsync(cancellationToken);

    protected override ValueTask<JsonElement> SerializeSessionCoreAsync(AgentSession session, JsonSerializerOptions? jsonSerializerOptions = null,
        CancellationToken cancellationToken = default) =>
        Harness.SerializeSessionAsync(session, jsonSerializerOptions, cancellationToken);

    protected override ValueTask<AgentSession> DeserializeSessionCoreAsync(JsonElement serializedState, JsonSerializerOptions? jsonSerializerOptions = null,
        CancellationToken cancellationToken = default) =>
        Harness.DeserializeSessionAsync(serializedState, jsonSerializerOptions, cancellationToken);

    public async ValueTask DisposeAsync()
    {
        if (_shell != null)
        {
            await _shell.DisposeAsync();
            _shell = null;
        }
    }
}