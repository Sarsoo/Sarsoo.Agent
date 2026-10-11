using System.ComponentModel;
using System.Text;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace Sarsoo.Agent.Advisor;

public class AdvisorFunction(
    AIAgent agent,
    AdvisorSharedContext sharedContext
)
{
    public async Task<string?> GetAdvice(string? query)
    {
        var messages = sharedContext
            .GetContext()
            .Where(m => m.Role != ChatRole.System)
            .Select(m => m.Clone()).ToList();
        
        if (!string.IsNullOrWhiteSpace(query))
        {
            var modelQuery = $"""
                              <query>
                              {query}
                              </query>
                              """;
            messages.Add(new(ChatRole.User, query));
        }
        
        var response = await agent.RunAsync(messages);

        var res = new StringBuilder();
        
        var messageIds = messages
            .Where(m => m.Role == ChatRole.Assistant)
            .Select(m => m.MessageId).ToList();
        
        foreach(var message in response.Messages.Where(m => m.Role == ChatRole.Assistant && !messageIds.Contains(m.MessageId)))
        {
            res.AppendLine(message.Text);
        }

        return res.ToString();
    }
}