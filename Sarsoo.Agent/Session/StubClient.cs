using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;

namespace Sarsoo.Agent.Session;

public class StubClient: IChatClient
{
    public void Dispose()
    {
        
    }

    public async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        return new ChatResponse()
        {
            ResponseId = Guid.NewGuid().ToString(),
            Messages = [
                new()
                {
                    MessageId = Guid.NewGuid().ToString(),
                    Role = ChatRole.Assistant,
                    Contents = [
                        new TextReasoningContent("hmmmmm....."),
                    ]
                },
                
                new()
                {
                    MessageId = Guid.NewGuid().ToString(),
                    Role = ChatRole.Assistant,
                    Contents = [
                        new TextContent("wahoo!")
                    ]
                }
            ]
        };
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var idx in Enumerable.Range(0, 10))
        {
            await Task.Delay(100, cancellationToken);
            yield return new ChatResponseUpdate()
            {
                ResponseId = Guid.NewGuid().ToString(),
                MessageId = Guid.NewGuid().ToString(),
                Contents = [
                    new TextReasoningContent($"hmmmmm..... ({idx})"),
                ]
            };
            await Task.Delay(100, cancellationToken);
            yield return new ChatResponseUpdate()
            {
                ResponseId = Guid.NewGuid().ToString(),
                MessageId = Guid.NewGuid().ToString(),
                Contents = [
                    new TextContent($"\n\n\nwahoo! ({idx})"),
                    new TextContent($"\n\n\nwahoo! ({idx})"),
                    new TextContent($"\n\n\nwahoo! ({idx})"),
                    new TextContent($"\n\n\nwahoo! ({idx})"),
                ]
            };
            await Task.Delay(100, cancellationToken);
            yield return new ChatResponseUpdate()
            {
                ResponseId = Guid.NewGuid().ToString(),
                MessageId = Guid.NewGuid().ToString(),
                Contents = [
                    new UsageContent()
                    {
                        Details = new()
                        {
                            InputTokenCount = 10,
                            CachedInputTokenCount = 20,
                            ReasoningTokenCount = 30,
                            OutputTokenCount = 40,
                            TotalTokenCount = 100
                        }
                    },
                ]
            };
        }
    }

    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        return null;
    }
}