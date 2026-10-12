using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Sarsoo.Agent.Advisor;
using Sarsoo.Agent.CLI.Session;
using Sarsoo.Agent.Client;
using Sarsoo.Agent.Composition;
using Sarsoo.Agent.Gate;
using Sarsoo.Agent.Integrations;
using Sarsoo.Agent.Options;
using Sarsoo.Agent.Session;
using Terminal.Gui.Configuration;

namespace Sarsoo.Agent.Tests;

internal static class TestAgents
{
    public static IAgentFactory CreateFactory(IEnumerable<IStateChangeReceiver>? receivers = null)
    {
        var services = new ServiceCollection().AddTransient<IAdvisorFactory, AdvisorFactory>().BuildServiceProvider();

        var profiles = new AgentProfileRegistry(
        [
            new AgentProfile("test", "test profile", builder => builder)
        ]);

        var gate = new ToolGateEnforcer(
            Microsoft.Extensions.Options.Options.Create(new ToolGateOptions()),
            Array.Empty<IToolGate>(),
            NullLogger<ToolGateEnforcer>.Instance);

        var clientResolver = new FakeClientResolver();

        return new AgentFactory(
            services,
            clientResolver,
            profiles,
            gate,
            receivers ?? Array.Empty<IStateChangeReceiver>(),
            NullLoggerFactory.Instance);
    }

    public static AgentWorkspace CreateWorkspace(IEnumerable<IStateChangeReceiver>? receivers = null)
        => new(
            CreateFactory(receivers),
            Microsoft.Extensions.Options.Options.Create(new AgentOptions { DefaultProfile = "test" }),
            NullLoggerFactory.Instance,
            NullLogger<AgentWorkspace>.Instance);
}

internal sealed class FakeClientResolver(Func<IChatClient>? factory = null) : IClientResolver
{
    public IChatClient GetClient(
        string? model = null,
        ReasoningEffort? thinkingLevel = null,
        Action<ChatOptions>? configureOptions = null
    )
        => factory?.Invoke() ?? new FakeChatClient();
}

internal sealed class RecordingReceiver : IStateChangeReceiver
{
    public ConcurrentQueue<(string AgentId, AgentState State)> Events { get; } = new();

    public Task StateChanged(string agentId, AgentState state, CancellationToken token = default)
    {
        Events.Enqueue((agentId, state));
        return Task.CompletedTask;
    }
}

public class AgentProfileRegistryTests
{
    [Fact]
    public void Get_UnknownProfile_Throws()
    {
        var registry = new AgentProfileRegistry([new AgentProfile("coding", "desc", builder => builder)]);

        Assert.Throws<KeyNotFoundException>(() => registry.Get("nope"));
    }

    [Fact]
    public void DuplicateProfileNames_Throw()
    {
        Assert.Throws<ArgumentException>(() => new AgentProfileRegistry(
        [
            new AgentProfile("coding", "a", builder => builder),
            new AgentProfile("coding", "b", builder => builder)
        ]));
    }

    [Fact]
    public void Profiles_AreLookedUpCaseInsensitively()
    {
        var registry = new AgentProfileRegistry([new AgentProfile("coding", "desc", builder => builder)]);

        Assert.Single(registry.Profiles);
        Assert.Equal("coding", registry.Get("CODING").Name);
    }
}

public class AgentFactoryTests
{
    [Fact]
    public void CreateSession_NamesTheSession()
    {
        var factory = TestAgents.CreateFactory();

        var session = factory.CreateSession("test", "alpha");

        Assert.Equal("alpha", session.Name);
    }

    [Fact]
    public void CreateSession_UnknownProfile_Throws()
    {
        var factory = TestAgents.CreateFactory();

        Assert.Throws<KeyNotFoundException>(() => factory.CreateSession("nope", "alpha"));
    }
}

public class AgentSessionRunnerTests
{
    [Fact]
    public async Task Post_RaisesUserAndAgentMessages_AndCyclesState()
    {
        var factory = TestAgents.CreateFactory();
        var runner = factory.CreateRunner(factory.CreateSession("test", "alpha"));

        var users = new ConcurrentQueue<ChatMessage>();
        var updates = new ConcurrentQueue<Microsoft.Agents.AI.AgentResponseUpdate>();
        var states = new ConcurrentQueue<AgentState>();
        var sawBusy = new StrongBox<bool>(false);
        var idleAfterBusy = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        runner.UserMessage += (_, message) => users.Enqueue(message);
        runner.AgentMessage += (_, update) => updates.Enqueue(update);
        runner.StateChanged += (_, state) =>
        {
            states.Enqueue(state);

            if (state == AgentState.Busy)
            {
                sawBusy.Value = true;
            }

            if (state == AgentState.Idle && sawBusy.Value)
            {
                idleAfterBusy.TrySetResult();
            }
        };

        _ = runner.RunAsync();
        runner.Post("hello");

        await idleAfterBusy.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

        Assert.Contains(users, message => message.Text == "hello");
        Assert.NotEmpty(updates);
        Assert.Contains(AgentState.Busy, states);

        await runner.DisposeAsync();
    }

    [Fact(Skip = "flakey")]
    public async Task StateChanges_AreAttributedToTheAgent()
    {
        var receiver = new RecordingReceiver();
        var factory = TestAgents.CreateFactory([receiver]);
        var runner = factory.CreateRunner(factory.CreateSession("test", "beta"));

        var sawBusy = new StrongBox<bool>(false);
        var idleAfterBusy = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        runner.StateChanged += (_, state) =>
        {
            if (state == AgentState.Busy)
            {
                sawBusy.Value = true;
            }

            if (state == AgentState.Idle && sawBusy.Value)
            {
                idleAfterBusy.TrySetResult();
            }
        };

        _ = runner.RunAsync();
        runner.Post("hi");

        await idleAfterBusy.Task.WaitAsync(TimeSpan.FromSeconds(20), TestContext.Current.CancellationToken);

        Assert.Contains(receiver.Events, e => e.AgentId == "beta" && e.State == AgentState.Busy);

        await runner.DisposeAsync();
    }
}

[Collection("TerminalGuiGlobalState")]
public class AgentWorkspaceTests
{
    [Fact]
    public async Task Create_AddsTabAndTracksActive()
    {
        var workspace = TestAgents.CreateWorkspace();

        var first = workspace.Create();
        Assert.NotNull(first);
        Assert.Equal("test-1", first!.Runner.Name);
        Assert.Same(first, workspace.Active);

        var second = workspace.Create("test", "beta");
        Assert.NotNull(second);
        Assert.Same(second, workspace.Active);

        Assert.Throws<InvalidOperationException>(() => workspace.Create("test", "beta"));

        Assert.True(await workspace.Remove("beta"));
        Assert.False(await workspace.Remove("beta"));

        await workspace.DisposeAsync();
    }

    [Fact]
    public async Task SendToActive_RoutesToTheActiveRunner()
    {
        var workspace = TestAgents.CreateWorkspace();

        var first = workspace.Create("test", "alpha")!;
        var second = workspace.Create("test", "beta")!;

        var firstUsers = new ConcurrentQueue<ChatMessage>();
        var secondUsers = new ConcurrentQueue<ChatMessage>();
        first.Runner.UserMessage += (_, message) => firstUsers.Enqueue(message);
        second.Runner.UserMessage += (_, message) => secondUsers.Enqueue(message);

        workspace.SendToActive("to-beta");

        await Task.Delay(500, TestContext.Current.CancellationToken);

        Assert.Empty(firstUsers);
        Assert.Contains(secondUsers, message => message.Text == "to-beta");

        await workspace.DisposeAsync();
    }
}
