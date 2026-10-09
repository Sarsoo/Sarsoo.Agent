using System.Threading.Channels;
using Microsoft.Agents.AI;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sarsoo.Agent.CLI.UI.Chat.Message;
using Sarsoo.Agent.Session;

namespace Sarsoo.Agent.CLI.Session;

public class AgentSessionLifetime(
    SAgent agent, 
    MessageView messageView, 
    ILogger<AgentSessionLifetime> logger
): BackgroundService
{
    private readonly Channel<string> _userMessages = Channel.CreateUnbounded<string>();
    private readonly Channel<AgentResponseUpdate> _agentMessages = Channel.CreateUnbounded<AgentResponseUpdate>();
    
    private AgentSession? _session;
    
    public void PassMessage(string message)
    {
        logger.LogInformation("Passing message to agent: {message}", message);
        _userMessages.Writer.TryWrite(message);
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Beginning agent lifetime monitor");
        return Task.WhenAll(ProcessUserMessages(stoppingToken), ProcessAgentMessages(stoppingToken));
    }

    private async Task ProcessUserMessages(CancellationToken stoppingToken)
    {
        try
        {
            logger.LogInformation("Creating agent session");
            agent.Init();
            _session = await agent.Harness.CreateSessionAsync(stoppingToken).ConfigureAwait(false);

            logger.LogInformation("Waiting for first user message");
            await foreach (var userMessage in _userMessages.Reader.ReadAllAsync(stoppingToken).ConfigureAwait(false))
            {
                logger.LogInformation("Iterating agent with user message: {message}", userMessage);
                messageView.AddMessage(new UserMessage()
                {
                    Text = userMessage
                });
                messageView.SetNeedsDraw();

                await foreach (var agentResponse in agent
                                   .RunStreamingAsync(userMessage, session: _session, cancellationToken: stoppingToken)
                                   .ConfigureAwait(false))
                {
                    logger.LogDebug("Queuing agent update: {agent_message}", agentResponse);
                    _agentMessages.Writer.TryWrite(agentResponse);
                }
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error processing user messages");
        }
    }

    private async Task ProcessAgentMessages(CancellationToken stoppingToken)
    {
        try
        {
            await foreach (var agentResponseUpdate in _agentMessages.Reader.ReadAllAsync(stoppingToken)
                               .ConfigureAwait(false))
            {
                messageView.AddAgentMessage(agentResponseUpdate);
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error processing agent messages");
        }
    }

    public override void Dispose()
    {
        base.Dispose();
        
        try
        {
            if(_userMessages?.Writer.TryComplete() is false)
            {
                _userMessages?.Writer.Complete();
            }
            
            if(_agentMessages?.Writer.TryComplete() is false)
            {
                _agentMessages?.Writer.Complete();
            }
        }
        catch(Exception ex)
        {
            logger.LogError(ex, "Exception thrown on disposal");
        }
    }
}