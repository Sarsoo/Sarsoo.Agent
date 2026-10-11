using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Sarsoo.Agent.Advisor;
using Sarsoo.Agent.Client;
using Sarsoo.Agent.Composition;
using Sarsoo.Agent.Gate;
using Sarsoo.Agent.Integrations;
using Sarsoo.Agent.Options;

namespace Sarsoo.Agent.Tests;

internal sealed class RecordingClientResolver : IClientResolver
{
    public string? Model { get; private set; }
    public ReasoningEffort? ThinkingLevel { get; private set; }

    public IChatClient GetClient(
        string? model = null,
        ReasoningEffort? thinkingLevel = null,
        Action<ChatOptions>? configureOptions = null
    )
    {
        Model = model;
        ThinkingLevel = thinkingLevel;
        return new FakeChatClient();
    }
}

internal sealed class RecordingAdvisorToolFactory : IAdvisorFactory
{
    public int CreateCount { get; private set; }

    public AIFunction CreateTool()
    {
        CreateCount++;
        return AIFunctionFactory.Create(() => "advice", new AIFunctionFactoryOptions { Name = "consult_advisor" });
    }

    public AIContextProvider CreateContext()
    {
        return new AdvisorContextProvider(new());
    }
}

public class AdvisorToolFactoryTests
{
    [Fact]
    public void Create_UsesConfiguredModelThinkingLevelAndToolMetadata()
    {
        var services = new ServiceCollection().AddTransient<IAdvisorFactory, RecordingAdvisorToolFactory>().BuildServiceProvider();
        var resolver = new RecordingClientResolver();
        var options = Microsoft.Extensions.Options.Options.Create(new AdvisorOptions
        {
            Model = "advisor-model",
            ThinkingLevel = ReasoningEffort.High,
            ToolName = "ask_expert",
            ToolDescription = "custom description"
        });

        var factory = new AdvisorFactory(resolver, options, NullLoggerFactory.Instance, services);
        var tool = factory.CreateTool();

        Assert.Equal("advisor-model", resolver.Model);
        Assert.Equal(ReasoningEffort.High, resolver.ThinkingLevel);
        Assert.Equal("ask_expert", tool.Name);
        Assert.Equal("custom description", tool.Description);
    }

    [Fact]
    public async Task Create_InvokesAdvisorAgentAsTool()
    {
        var services = new ServiceCollection().AddTransient<IAdvisorFactory, RecordingAdvisorToolFactory>().BuildServiceProvider();
        var options = Microsoft.Extensions.Options.Options.Create(new AdvisorOptions());

        var factory = new AdvisorFactory(new FakeClientResolver(), options, NullLoggerFactory.Instance, services);
        var tool = factory.CreateTool();

        var result = await tool.InvokeAsync(
            new AIFunctionArguments { ["query"] = "What should I do?" },
            TestContext.Current.CancellationToken);

        Assert.Contains("fake-response", result?.ToString());
    }

    [Fact]
    public void AdvisorOptions_BindFromConfiguration()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Advisor:Enabled"] = "true",
                ["Advisor:Model"] = "o3",
                ["Advisor:ThinkingLevel"] = "High",
                ["Advisor:ToolName"] = "ask_expert"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddTransient<IAdvisorFactory, RecordingAdvisorToolFactory>();
        services.Configure<AdvisorOptions>(configuration.GetSection(AdvisorOptions.Key));

        var options = services.BuildServiceProvider()
            .GetRequiredService<Microsoft.Extensions.Options.IOptions<AdvisorOptions>>()
            .Value;

        Assert.True(options.Enabled);
        Assert.Equal("o3", options.Model);
        Assert.Equal(ReasoningEffort.High, options.ThinkingLevel);
        Assert.Equal("ask_expert", options.ToolName);
    }

    [Fact]
    public void Create_FallsBackToDefaults()
    {
        var services = new ServiceCollection().AddTransient<IAdvisorFactory, RecordingAdvisorToolFactory>().BuildServiceProvider();
        var resolver = new RecordingClientResolver();
        var options = Microsoft.Extensions.Options.Options.Create(new AdvisorOptions());

        var factory = new AdvisorFactory(resolver, options, NullLoggerFactory.Instance, services);
        var tool = factory.CreateTool();

        Assert.Null(resolver.Model);
        Assert.Null(resolver.ThinkingLevel);
        Assert.Equal(AdvisorOptions.DefaultToolName, tool.Name);
        Assert.Equal(AdvisorOptions.DefaultToolDescription, tool.Description);
    }
}

public class AgentFactoryAdvisorTests
{
    private static AgentFactory CreateFactory(
        AgentProfile profile,
        AdvisorOptions advisorOptions
    )
    {
        var services = new ServiceCollection().AddTransient<IAdvisorFactory, RecordingAdvisorToolFactory>().BuildServiceProvider();

        var gate = new ToolGateEnforcer(
            Microsoft.Extensions.Options.Options.Create(new ToolGateOptions()),
            Array.Empty<IToolGate>(),
            NullLogger<ToolGateEnforcer>.Instance);

        return new AgentFactory(
            services,
            new FakeClientResolver(),
            new AgentProfileRegistry([profile]),
            Microsoft.Extensions.Options.Options.Create(advisorOptions),
            gate,
            Array.Empty<IStateChangeReceiver>(),
            NullLoggerFactory.Instance);
    }

    [Fact]
    public void CreateSession_AddsAdvisorTool_WhenProfileIncludesIt()
    {
        var advisor = new RecordingAdvisorToolFactory();
        var factory = CreateFactory(
            new AgentProfile("with-advisor", "d", builder => builder, IncludeAdvisor: true),
            new AdvisorOptions());

        factory.CreateSession("with-advisor", "alpha");

        Assert.Equal(1, advisor.CreateCount);
    }

    [Fact]
    public void CreateSession_SkipsAdvisorTool_WhenProfileDoesNotIncludeIt()
    {
        var advisor = new RecordingAdvisorToolFactory();
        var factory = CreateFactory(
            new AgentProfile("no-advisor", "d", builder => builder),
            new AdvisorOptions());

        factory.CreateSession("no-advisor", "alpha");

        Assert.Equal(0, advisor.CreateCount);
    }

    [Fact]
    public void CreateSession_SkipsAdvisorTool_WhenDisabled()
    {
        var advisor = new RecordingAdvisorToolFactory();
        var factory = CreateFactory(
            new AgentProfile("with-advisor", "d", builder => builder, IncludeAdvisor: true),
            new AdvisorOptions { Enabled = false });

        factory.CreateSession("with-advisor", "alpha");

        Assert.Equal(0, advisor.CreateCount);
    }
}
