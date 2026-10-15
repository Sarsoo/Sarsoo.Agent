using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sarsoo.Agent.CLI.Extensions;
using Sarsoo.Agent.CLI.Observability;
using Sarsoo.Agent.CLI.UI.Chat.Message;
using Sarsoo.Agent.Composition;
using Sarsoo.Agent.Options;
using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Sarsoo.Agent.CLI.Session;

/// <summary>
///     Owns the set of running agents and the tabs that display them. This is the single registry
///     for agent sessions; it is not a background service and does not resolve per-agent objects
///     from the container.
/// </summary>
public sealed class AgentWorkspace : IAsyncDisposable
{
    private readonly IAgentFactory _factory;
    private readonly IOptions<AgentOptions> _options;
    private readonly IApplication? _app;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<AgentWorkspace> _logger;
    private readonly ILogger<AgentTab> _tabLogger;
    private readonly Tabs _tabs = new();
    private readonly Dictionary<string, AgentTab> _tabsByName = new(StringComparer.OrdinalIgnoreCase);
    private int _autoNameCounter;
    private bool _disposed;

    public AgentWorkspace(
        IAgentFactory factory,
        IOptions<AgentOptions> options,
        ILoggerFactory loggerFactory,
        ILogger<AgentWorkspace> logger,
        ILogger<AgentTab> tabLogger,
        IApplication? app = null
    )
    {
        _factory = factory;
        _options = options;
        _loggerFactory = loggerFactory;
        _logger = logger;
        _tabLogger = tabLogger;
        _app = app;

        _tabs.ValueChanged += OnTabValueChanged;
    }

    public View TabsHost => _tabs;

    public AgentTab? Active { get; private set; }

    public event EventHandler<AgentTab?>? ActiveChanged;

    public AgentTab? Create(string? profile = null, string? name = null,  
        string? model = null, 
        ReasoningEffort? thinkingLevel = null,
        Action<ChatOptions>? configureOptions = null,
        string? systemPrompt = null)
    {
        _logger.LogThreadWarning(_app);
        using var trace = CliTracing.Source.StartActivity($"{nameof(AgentWorkspace)}::{nameof(Create)}");
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(AgentWorkspace));
        }

        profile ??= _options.Value.DefaultProfile;
        name ??= NextAutoName(profile, name);

        if (_tabsByName.ContainsKey(name))
        {
            throw new InvalidOperationException($"An agent named '{name}' already exists.");
        }
        
        return BuildTab(
            profile, 
            name, 
            model: model,
            thinkingLevel: thinkingLevel,
            configureOptions: configureOptions,
            systemPrompt: systemPrompt
        );
    }

    public void LaunchConfigured()
    {
        try
        {
            _logger.LogThreadWarning(_app);
            using var trace = CliTracing.Source.StartActivity($"{nameof(AgentWorkspace)}::{nameof(LaunchConfigured)}");
            foreach (var definition in _options.Value.Startup)
            {
                if (string.IsNullOrWhiteSpace(definition.Name))
                {
                    _logger.LogWarning("Skipping configured agent with no name");
                    continue;
                }

                try
                {
                    Create(
                        profile: definition.Profile,
                        name: definition.Name,
                        model: definition.Model,
                        thinkingLevel: definition.ThinkingLevel,
                        systemPrompt: definition.Prompt
                    );
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Failed to launch configured agent {agent}", definition.Name);
                }
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to launch configured agents");
        }
    }

    public async Task<bool> Remove(string name)
    {
        _logger.LogThreadWarning(_app);
        using var trace = CliTracing.Source.StartActivity($"{nameof(AgentWorkspace)}::{nameof(Remove)}");
        if (!_tabsByName.Remove(name, out var tab))
        {
            return false;
        }

        _tabs.Remove(tab);
        await tab.DisposeAsync().ConfigureAwait(false);

        return true;
    }

    public void SendToActive(string message) => Active?.Runner.Post(message);

    private AgentTab BuildTab(string profile, string name, 
        string? model = null, 
        ReasoningEffort? thinkingLevel = null,
        Action<ChatOptions>? configureOptions = null,
        string? systemPrompt = null
    )
    {
        _logger.LogThreadWarning(_app);
        using var trace = CliTracing.Source.StartActivity($"{nameof(AgentWorkspace)}::{nameof(BuildTab)}");
        var session = _factory.CreateSession(
            profile, 
            name, 
            model: model,
            thinkingLevel: thinkingLevel,
            configureOptions: configureOptions,
            systemPrompt: systemPrompt
        );
        var runner = _factory.CreateRunner(session);
        var messages = new MessageView(_loggerFactory.CreateLogger<MessageView>());
        var stats = new TokenSessionStats(session);

        var tab = new AgentTab(runner, messages, stats, _tabLogger);
        _tabsByName.Add(name, tab);

        void AddTab()
        {
            _tabs.Add(tab);
            _tabs.Value = tab;
        }

        // Register the tab on the UI thread when one is live; otherwise (headless or not-yet-run
        // application) mutate the tab set inline so the workspace still tracks its active agent.
        if (_app is { MainThreadId: int mainThreadId } app && mainThreadId != Environment.CurrentManagedThreadId)
        {
            app.Invoke(AddTab);
        }
        else
        {
            AddTab();
        }

        _logger.LogThreadWarning(_app);
        _ = runner.RunAsync();

        _logger.LogInformation("Created agent {agent} from profile {profile}", name, profile);

        return tab;
    }

    private string NextAutoName(string? profile = null, string? name = null)
    {
        string n;
        do
        {
            n = $"{name ?? profile ?? "agent"}-{++_autoNameCounter}";
        } while (_tabsByName.ContainsKey(n));

        return n;
    }

    private void OnTabValueChanged(object? sender, ValueChangedEventArgs<View?> e)
    {
        _logger.LogThreadWarning(_app);
        using var trace = CliTracing.Source.StartActivity($"{nameof(AgentWorkspace)}::{nameof(OnTabValueChanged)}");
        var active = e.NewValue as AgentTab;

        if (ReferenceEquals(active, Active))
        {
            return;
        }

        Active = active;
        ActiveChanged?.Invoke(this, active);
    }

    public async ValueTask DisposeAsync()
    {
        using var trace = CliTracing.Source.StartActivity($"{nameof(AgentWorkspace)}::{nameof(DisposeAsync)}");
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        _tabs.ValueChanged -= OnTabValueChanged;

        // The view tree (MainWindow) owns the tabs' disposal; the workspace owns the runners.
        foreach (var tab in _tabsByName.Values.ToList())
        {
            await tab.DisposeRunnerAsync().ConfigureAwait(false);
        }

        _tabsByName.Clear();
    }
}
