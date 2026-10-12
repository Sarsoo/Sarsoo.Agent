using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Sarsoo.Agent.CLI.UI.Chat.Message;
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

        fixture.View.Viewport = fixture.View.Viewport with { Y = 0 };
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
