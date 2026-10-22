using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Sarsoo.Agent.CLI.UI.Chat.Message;
using Sarsoo.Agent.CLI.UI.Chat.Message.Agent;
using Terminal.Gui.App;
using Terminal.Gui.Drivers;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using System.Drawing;

namespace Sarsoo.Agent.Tests;

/// <summary>
///     Regression coverage for message list scrolling. A wheel over a message border (inside the
///     message frame but outside its viewport) used to be dropped by application mouse dispatch, so
///     only the message content area scrolled. Every wheel over the list must scroll the
///     <see cref="MessageView"/> viewport exactly once.
/// </summary>
[Collection ("TerminalGuiGlobalState")]
public class MessageViewScrollTests
{
    [Fact]
    public void Wheel_Up_OnMessageBorder_ScrollsMessageViewport ()
    {
        using var fixture = new Fixture ();

        // The list auto-scrolls to the bottom as messages are added, so the last message is on
        // screen. Its top border is inside the frame but outside the viewport.
        Assert.True (fixture.View.Viewport.Y > 0);

        var before = fixture.View.Viewport.Y;
        var point = BorderPoint (fixture.View, fixture.View.SubViews.Count - 1);

        var mouse = new Mouse { ScreenPosition = point, Flags = MouseFlags.WheeledUp };
        fixture.App.Mouse.RaiseMouseEvent (mouse);

        Assert.True (mouse.Handled);
        Assert.Equal (before - 1, fixture.View.Viewport.Y);
    }

    [Fact]
    public void Wheel_Down_OnMessageContent_ScrollsExactlyOnce ()
    {
        using var fixture = new Fixture ();

        // Move away from the bottom the way a user would, so the list stops following content.
        fixture.View.InvokeCommand (Command.Home);
        fixture.View.Layout ();

        var point = InnerPoint (fixture.View, 0);

        var mouse = new Mouse { ScreenPosition = point, Flags = MouseFlags.WheeledDown };
        fixture.App.Mouse.RaiseMouseEvent (mouse);

        // The hook consumes the wheel, so the (still bound) subview scroll path cannot also fire.
        Assert.True (mouse.Handled);

        // Exactly one: the application-level hook must also suppress the subview's own scroll.
        Assert.Equal (1, fixture.View.Viewport.Y);
    }

    [Fact]
    public void Wheel_Down_AtBottom_IsClamped ()
    {
        using var fixture = new Fixture ();

        var atBottom = fixture.View.Viewport.Y;
        Assert.True (atBottom > 0);

        var point = InnerPoint (fixture.View, fixture.View.SubViews.Count - 1);

        fixture.App.Mouse.RaiseMouseEvent (new Mouse { ScreenPosition = point, Flags = MouseFlags.WheeledDown });

        Assert.Equal (atBottom, fixture.View.Viewport.Y);
    }

    [Fact]
    public void Wheel_OverOneView_DoesNotScrollAnother ()
    {
        using var fixture = new Fixture (twoViews: true);

        var second = fixture.Second!;
        var leftBefore = fixture.View.Viewport.Y;
        var rightBefore = second.Viewport.Y;
        Assert.True (leftBefore > 0);
        Assert.True (rightBefore > 0);

        var point = BorderPoint (fixture.View, fixture.View.SubViews.Count - 1);

        fixture.App.Mouse.RaiseMouseEvent (new Mouse { ScreenPosition = point, Flags = MouseFlags.WheeledUp });

        Assert.Equal (leftBefore - 1, fixture.View.Viewport.Y);
        Assert.Equal (rightBefore, second.Viewport.Y);
    }

    [Fact]
    public void ContentUpdate_DoesNotStealTheViewport_WhenUserScrolledAway ()
    {
        using var fixture = new Fixture ();

        // Grow the list past the viewport, then park the viewport at the top.
        var message = new TextMessage (new TextContent (string.Join ('\n', Enumerable.Repeat ("line", 40))));
        fixture.View.AddMessage (message);
        fixture.App.LayoutAndDraw ();

        fixture.View.InvokeCommand (Command.Home);
        Assert.Equal (0, fixture.View.Viewport.Y);

        // Streaming more content must not yank the user back to the bottom.
        for (var i = 0; i < 20; i++)
        {
            message.SetContent (new TextContent ("\nmore"));
        }

        fixture.App.TimedEvents!.RunTimers ();

        if (fixture.View.NeedsLayout)
        {
            fixture.View.Layout ();
        }

        Assert.Equal (0, fixture.View.Viewport.Y);

        // End returns to the bottom and resumes following.
        fixture.View.InvokeCommand (Command.End);
        Assert.True (fixture.View.Viewport.Y > 0);
    }

    [Fact]
    public void StickyBottom_StaysAtTheBottomAcrossLayoutPasses ()
    {
        using var fixture = new Fixture ();

        var expected = Math.Max (0, fixture.View.GetContentHeight () - fixture.View.Viewport.Height);
        Assert.Equal (expected, fixture.View.Viewport.Y);

        // Note: Terminal.Gui keeps NeedsLayout set on an idle view in this harness (reproduced on the
        // pre-change code too), so "settled" cannot be asserted via NeedsLayout. What matters for
        // autoscroll is that repeated passes do not drift the viewport or oscillate.
        for (var i = 0; i < 3; i++)
        {
            fixture.App.TimedEvents!.RunTimers ();
            fixture.App.LayoutAndDraw ();

            Assert.Equal (expected, fixture.View.Viewport.Y);
            Assert.Equal (Math.Max (0, fixture.View.GetContentHeight () - fixture.View.Viewport.Height),
                          fixture.View.Viewport.Y);
        }
    }

    [Fact]
    public void ViewportGrowing_WhileScrolledAway_ClampsToTheLastRow ()
    {
        using var fixture = new Fixture ();

        fixture.View.InvokeCommand (Command.Home);
        fixture.View.InvokeCommand (Command.ScrollDown);
        fixture.App.LayoutAndDraw ();
        Assert.Equal (1, fixture.View.Viewport.Y);

        // The terminal grows, so the largest valid scroll offset shrinks below the parked offset.
        fixture.View.Height = Dim.Absolute (fixture.View.Frame.Height + 100);
        fixture.App.LayoutAndDraw ();

        var maxY = Math.Max (0, fixture.View.GetContentHeight () - fixture.View.Viewport.Height);
        Assert.Equal (maxY, fixture.View.Viewport.Y);
    }

    private static Point BorderPoint (MessageView view, int index)
    {
        var frame = view.SubViews.ElementAt (index).FrameToScreen ();

        return new Point (frame.X + 2, frame.Y);
    }

    private static Point InnerPoint (MessageView view, int index)
    {
        // ViewportToScreen is the inner top-left, so this is independent of border thickness.
        return view.SubViews.ElementAt (index).ViewportToScreen (new Point (1, 0));
    }

    private sealed class Fixture : IDisposable
    {
        private readonly SessionToken _token;
        private readonly string? _previousDriverIo;

        public Fixture (int messageCount = 12, bool twoViews = false)
        {
            _previousDriverIo = Environment.GetEnvironmentVariable ("DisableRealDriverIO");
            Environment.SetEnvironmentVariable ("DisableRealDriverIO", "1");

            App = Application.Create ().Init (DriverRegistry.Names.ANSI);
            App.Driver!.SetScreenSize (44, 24);

            var top = new Window { Width = Dim.Fill (), Height = Dim.Fill (), App = App };
            View = new MessageView (NullLogger<MessageView>.Instance) { Width = Dim.Fill (), Height = Dim.Fill () };

            if (twoViews)
            {
                View.Width = Dim.Percent (50);
                Second = new MessageView (NullLogger<MessageView>.Instance)
                {
                    X = Pos.Right (View), Width = Dim.Fill (), Height = Dim.Fill ()
                };
                top.Add (View, Second);
            }
            else
            {
                top.Add (View);
            }

            _token = App.Begin (top)!;

            for (var i = 0; i < messageCount; i++)
            {
                View.AddMessage (new UserMessage (new ChatMessage (ChatRole.User, $"user {i}\nsecond {i}")));
                Second?.AddMessage (new UserMessage (new ChatMessage (ChatRole.User, $"other {i}\nsecond {i}")));
            }

            App.LayoutAndDraw ();
        }

        public IApplication App { get; }
        public MessageView View { get; }
        public MessageView? Second { get; }

        public void Dispose ()
        {
            App.End (_token);
            App.Dispose ();
            Environment.SetEnvironmentVariable ("DisableRealDriverIO", _previousDriverIo);
        }
    }
}
