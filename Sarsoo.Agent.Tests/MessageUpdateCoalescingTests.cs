using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Sarsoo.Agent.CLI.UI.Chat.Message;
using Sarsoo.Agent.CLI.UI.Chat.Message.Agent;
using Terminal.Gui.App;
using Terminal.Gui.Drivers;
using Terminal.Gui.Drawing;
using Terminal.Gui.Views;

namespace Sarsoo.Agent.Tests;

/// <summary>
///     Coverage for the coalescing contract on <see cref="BaseMessage"/>: a fast stream of content
///     updates must not queue one application callback per chunk (which lets the main loop's timer
///     drain starve drawing and input), while the latest snapshot must still be applied.
/// </summary>
[Collection ("TerminalGuiGlobalState")]
public class MessageUpdateCoalescingTests
{
    /// <summary>
    ///     Applies allowed for a ~40ms producer carrying 200 chunks: the leading edge plus roughly one
    ///     per <see cref="BaseMessage.UpdateInterval"/>. Generous so a slow CI box does not flake.
    /// </summary>
    private const int MaxExpectedAppliesForBurst = 20;

    [Fact]
    public void QueuedCallbackCount_DoesNotGrowWithChunkCount ()
    {
        using var fixture = new Fixture ();
        CountingTextMessage message = fixture.NewTextMessage ("seed");

        var baseline = fixture.App.TimedEvents!.Timeouts.Count;

        for (var i = 0; i < 10; i++)
        {
            message.SetContent (new TextContent ("x"));
        }

        var afterFirstBurst = fixture.App.TimedEvents!.Timeouts.Count;
        Assert.True (afterFirstBurst > baseline, "The first burst must actually queue a coalesced apply.");

        for (var i = 0; i < 500; i++)
        {
            message.SetContent (new TextContent ("x"));
        }

        // The five hundred extra chunks must be absorbed by the already-queued update rather than
        // queuing five hundred more zero-delay callbacks.
        Assert.Equal (afterFirstBurst, fixture.App.TimedEvents!.Timeouts.Count);
    }

    [Fact]
    public void BurstThenDrain_AppliesLatestSnapshotExactlyOnce ()
    {
        using var fixture = new Fixture ();
        CountingTextMessage message = fixture.NewTextMessage ("seed");

        const int chunks = 500;

        for (var i = 0; i < chunks; i++)
        {
            message.SetContent (new TextContent ("x"));
        }

        fixture.App.TimedEvents!.RunTimers ();

        // Dropped intermediate states are irrelevant; the newest one must land, once.
        Assert.Equal (1, message.Applies);
        Assert.Equal ("seed" + new string ('x', chunks), MarkdownOf (message).Text);
    }

    [Fact]
    public void UpdateAfterAnApply_IsRateLimitedButNotLost ()
    {
        using var fixture = new Fixture ();
        CountingTextMessage message = fixture.NewTextMessage ("seed");

        message.SetContent (new TextContent ("a"));
        fixture.App.TimedEvents!.RunTimers ();
        Assert.Equal (1, message.Applies);

        // Immediately after an apply the next one is spaced by UpdateInterval, so a tight loop of
        // timer drains must not turn into a per-chunk repaint.
        message.SetContent (new TextContent ("b"));
        fixture.App.TimedEvents!.RunTimers ();
        Assert.Equal (1, message.Applies);

        DrainUntil (fixture.App, () => message.Applies == 2);

        Assert.Equal (2, message.Applies);
        Assert.Equal ("seedab", MarkdownOf (message).Text);
    }

    [Fact]
    public void UpdateRequestedBeforeAttaching_IsAppliedOnceAttached ()
    {
        using var fixture = new Fixture ();

        // No App yet: the message is built on the runner thread, before the tab adds it to the tree.
        var message = new CountingTextMessage (new TextContent ("seed"));
        message.SetContent (new TextContent ("-before-attach"));

        fixture.View.AddMessage (message);
        fixture.View.Layout ();
        fixture.App.TimedEvents!.RunTimers ();

        Assert.Equal (1, message.Applies);
        Assert.Equal ("seed-before-attach", MarkdownOf (message).Text);
    }

    [Fact]
    public void MessageMovedToAnotherApplication_ReschedulesOnTheNewOne ()
    {
        using var fixture = new Fixture ();
        CountingTextMessage message = fixture.NewTextMessage ("seed");

        message.SetContent (new TextContent ("a"));   // scheduled on fixture.App
        Assert.True (fixture.App.TimedEvents!.Timeouts.Count > 0);

        using IApplication other = Application.Create ().Init (DriverRegistry.Names.ANSI);
        other.Driver!.SetScreenSize (44, 24);

        message.App = other;
        message.SetContent (new TextContent ("b"));   // must migrate the pending schedule

        // The abandoned callback must not clear the new schedule or apply early.
        fixture.App.TimedEvents.RunTimers ();
        Assert.Equal (0, message.Applies);

        DrainUntil (other, () => message.Applies == 1);

        Assert.Equal (1, message.Applies);
        Assert.Equal ("seedab", MarkdownOf (message).Text);
    }

    [Fact]
    public void BackgroundProducer_DoesNotOutgrowTheQueue ()
    {
        using var fixture = new Fixture ();
        CountingTextMessage message = fixture.NewTextMessage ("seed");

        const int chunks = 200;

        var producer = new Thread (() =>
                                   {
                                       for (var i = 0; i < chunks; i++)
                                       {
                                           message.SetContent (new TextContent ("x"));

                                           if (i % 5 == 0)
                                           {
                                               Thread.Sleep (1);
                                           }
                                       }
                                   });

        producer.Start ();

        while (producer.IsAlive)
        {
            fixture.App.TimedEvents!.RunTimers ();

            if (fixture.View.NeedsLayout)
            {
                fixture.View.Layout ();
            }

            Thread.Sleep (1);
        }

        producer.Join ();
        DrainUntil (fixture.App, () => MarkdownOf (message).Text.Length == "seed".Length + chunks);

        // The producer thread must be absorbed into the frame-rate pace, not applied per chunk.
        Assert.True (message.Applies <= MaxExpectedAppliesForBurst,
                     $"Applied {message.Applies} times for {chunks} chunks (expected <= {MaxExpectedAppliesForBurst}).");

        // Nothing produced during the stream may be lost.
        Assert.Equal ("seed" + new string ('x', chunks), MarkdownOf (message).Text);
    }

    /// <summary>
    ///     Runs application iterations until <paramref name="condition"/> holds, rather than sleeping
    ///     for a fixed interval, so the rate-limit assertions do not race a slow CI machine.
    /// </summary>
    private static void DrainUntil (IApplication app, Func<bool> condition)
    {
        for (var i = 0; i < 200 && !condition (); i++)
        {
            Thread.Sleep (10);
            app.TimedEvents!.RunTimers ();
        }

        Assert.True (condition (), "Timed out waiting for the coalesced update to be applied.");
    }

    private static Markdown MarkdownOf (TextMessage message) => Assert.IsType<Markdown> (Assert.Single (message.SubViews));

    private sealed class CountingTextMessage (TextContent content) : TextMessage (content)
    {
        private int _applies;

        public int Applies => Volatile.Read (ref _applies);

        protected override void ApplyContentUpdate ()
        {
            Interlocked.Increment (ref _applies);
            base.ApplyContentUpdate ();
        }
    }

    private sealed class Fixture : IDisposable
    {
        private readonly string? _previousDriverIo;

        public Fixture ()
        {
            _previousDriverIo = Environment.GetEnvironmentVariable ("DisableRealDriverIO");
            Environment.SetEnvironmentVariable ("DisableRealDriverIO", "1");

            App = Application.Create ().Init (DriverRegistry.Names.ANSI);
            App.Driver!.SetScreenSize (44, 24);

            View = new MessageView (NullLogger<MessageView>.Instance) { App = App, Frame = App.Screen };
        }

        public IApplication App { get; }
        public MessageView View { get; }

        public CountingTextMessage NewTextMessage (string text)
        {
            var message = new CountingTextMessage (new TextContent (text));
            View.AddMessage (message);
            View.Layout ();

            return message;
        }

        public void Dispose ()
        {
            App.Dispose ();
            Environment.SetEnvironmentVariable ("DisableRealDriverIO", _previousDriverIo);
        }
    }
}
