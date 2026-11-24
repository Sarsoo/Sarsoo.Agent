using System.Text;
using Microsoft.Agents.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sarsoo.Agent.Observability;
using Sarsoo.Agent.Options;

namespace Sarsoo.Agent.Context.Hindsight;

public class HindsightContextProvider: AIContextProvider
{
    private class HindsightState
    {
        public string? AgentsMd { get; set; }
        private bool _initialized;

        public async Task<string?> GetHindsightContext(ILogger<HindsightContextProvider>? logger,
            CancellationToken cancellationToken = default)
        {
            if (!_initialized)
            {
                await Initialize(logger, cancellationToken);
            }

            return AgentsMd;
        }

        private async Task Initialize(ILogger<HindsightContextProvider>? logger, CancellationToken cancellationToken = default)
        {
            using var trace = Tracing.Source.StartActivity($"{nameof(HindsightContextProvider)}::{nameof(Initialize)}");

            _initialized = true;
        }
    }

    private readonly ProviderSessionState<HindsightState> _sessionState;
    private readonly IOptions<HindsightOptions> _hindsightConfig;
    private readonly HindsightClient _hindsightClient;
    private readonly ILogger<HindsightContextProvider>? _logger;

    public HindsightContextProvider(
        IOptions<HindsightOptions> hindsightConfig,
        HindsightClient hindsightClient,
        ILogger<HindsightContextProvider>? logger = null
    )
    {
        _hindsightConfig = hindsightConfig;
        _hindsightClient = hindsightClient;
        _logger = logger;
        _sessionState = new ProviderSessionState<HindsightState>(
            _ => new HindsightState(),
            GetType().Name
        );
    }

    protected override async ValueTask<AIContext> ProvideAIContextAsync(InvokingContext context, CancellationToken cancellationToken = default)
    {
        using var trace = Tracing.Source.StartActivity($"{nameof(HindsightContextProvider)}::{nameof(ProvideAIContextAsync)}");
        var state = _sessionState.GetOrInitializeState(context.Session);

        var retContext = new AIContext
        {
            Instructions = await state.GetHindsightContext(_logger, cancellationToken)
        };

        return retContext;
    }
}