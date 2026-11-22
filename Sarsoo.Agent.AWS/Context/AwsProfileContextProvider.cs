using System.Text;
using Amazon.Runtime.CredentialManagement;
using Microsoft.Agents.AI;
using Microsoft.Extensions.Logging;
using Sarsoo.Agent.Observability;

namespace Sarsoo.Agent.AWS.Context;

public class AwsProfileContextProvider : AIContextProvider
{
    private class AwsProfileState
    {
        public string? ClusterContext { get; set; }
        private bool _initialized;
        private CredentialProfileStoreChain? _profiles;

        public async Task<string?> GetClusterContext(ILogger<AwsProfileContextProvider>? logger,
            CancellationToken cancellationToken = default)
        {
            if (!_initialized)
            {
                await Initialize(logger, cancellationToken);
            }

            return ClusterContext;
        }
        
        private async Task Initialize(ILogger<AwsProfileContextProvider>? logger, CancellationToken cancellationToken = default)
        {
            using var trace = Tracing.Source.StartActivity($"{nameof(AwsProfileContextProvider)}::{nameof(Initialize)}");
            var instructionBuilder = new StringBuilder();

            try
            {
                _profiles = new CredentialProfileStoreChain();

                instructionBuilder.AppendLine("This machine has the following AWS profiles configured:");

                instructionBuilder.AppendLine("<available_aws_profiles>");
                foreach (var context in _profiles.ListProfiles())
                {
                    instructionBuilder.AppendLine("  <profile>");
                    instructionBuilder.Append    ("    <name>").Append(context.Name).AppendLine("</name>");
                    if(!string.IsNullOrWhiteSpace(context.Options.RoleArn))
                        instructionBuilder.Append("    <role_arn>").Append(context.Options.RoleArn).AppendLine("</role_arn>");
                    if(!string.IsNullOrWhiteSpace(context.Options.SourceProfile))
                        instructionBuilder.Append("    <source_profile>").Append(context.Options.SourceProfile).AppendLine("</source_profile>");
                    instructionBuilder.AppendLine("  </profile>");
                }
                instructionBuilder.AppendLine("</available_aws_profiles>");
            }
            catch (Exception e)
            {
                logger?.LogError(e, "Error loading AWS Profiles");
                instructionBuilder.AppendLine("There was an error loading the AWS profiles config on this machine. `aws` commands may be non-functional");
            }

            if (instructionBuilder.Length > 0)
            {
                ClusterContext = instructionBuilder.ToString();
            }
            
            _initialized = true;
        }
    }
    
    private readonly ProviderSessionState<AwsProfileState> _sessionState;
    private readonly ILogger<AwsProfileContextProvider>? _logger;

    public AwsProfileContextProvider(ILogger<AwsProfileContextProvider>? logger = null)
    {
        _logger = logger;
        _sessionState = new ProviderSessionState<AwsProfileState>(
            _ => new AwsProfileState(),
            GetType().Name
        );
    }

    protected override async ValueTask<AIContext> ProvideAIContextAsync(InvokingContext context, CancellationToken cancellationToken = default)
    {
        using var trace = Tracing.Source.StartActivity($"{nameof(AwsProfileContextProvider)}::{nameof(ProvideAIContextAsync)}");
        var state = _sessionState.GetOrInitializeState(context.Session);
        
        var retContext = new AIContext
        {
            Instructions = await state.GetClusterContext(_logger, cancellationToken)
        };

        return retContext;
    }
}