using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

namespace Sarsoo.Agent.Tests;

/// <summary>
///     A deterministic, fast <see cref="IChatClient"/> for tests: one text update plus one usage update.
/// </summary>
public sealed class FakeChatClient : IChatClient
{
    public void Dispose()
    {
    }

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default
    )
        => Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "fake-response")));

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        await Task.Yield();

        yield return new ChatResponseUpdate
        {
            MessageId = "message-1",
            Role = ChatRole.Assistant,
            Contents = [new TextContent("hello")]
        };

        yield return new ChatResponseUpdate
        {
            MessageId = "message-1",
            Role = ChatRole.Assistant,
            Contents =
            [
                new UsageContent
                {
                    Details = new UsageDetails
                    {
                        InputTokenCount = 1,
                        OutputTokenCount = 2,
                        TotalTokenCount = 3
                    }
                }
            ]
        };
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;
}
