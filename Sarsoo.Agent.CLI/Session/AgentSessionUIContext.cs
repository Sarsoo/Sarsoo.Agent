using Sarsoo.Agent.CLI.UI.Chat.Message;
using Sarsoo.Agent.Session;
using Sarsoo.Agent.Session.Orchestration;

namespace Sarsoo.Agent.CLI.Session;

public class AgentSessionUIContext: IAsyncDisposable
{
    public AgentInteractiveLifetime Loop { get; init; }
    public AgentSessionContext Context { get; init; }
    public MessageView MessageView { get; init; }
    public SessionUiBinding SessionUiBinding { get; init; }
    private CancellationTokenSource _cancellationTokenSource = new();

    public void Cancel()
    {
        _cancellationTokenSource.Cancel();
    }

    public string Id => Context.Id;

    public void Execute()
    {
        _ = Task.Run(async () =>
        {
            await SessionUiBinding.ExecuteAsync(_cancellationTokenSource.Token);
            await Loop.ExecuteAsync(_cancellationTokenSource.Token);
        }, 
            _cancellationTokenSource.Token);
    }

    public async ValueTask DisposeAsync()
    {
        await CastAndDispose(Loop);
        await Context.DisposeAsync();
        await CastAndDispose(MessageView);
        await CastAndDispose(SessionUiBinding);

        return;

        static async ValueTask CastAndDispose(IDisposable resource)
        {
            if (resource is IAsyncDisposable resourceAsyncDisposable)
                await resourceAsyncDisposable.DisposeAsync();
            else
                resource.Dispose();
        }
    }
}