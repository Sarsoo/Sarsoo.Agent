using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Sarsoo.Agent.CLI.UI.Chat.Message;
using Sarsoo.Agent.CLI.UI.Chat.Message.Agent;
using Terminal.Gui.App;
using Terminal.Gui.Drivers;
using Terminal.Gui.Drawing;

namespace Sarsoo.Agent.Tests;

/// <summary>
///     Regression coverage for streamed <see cref="TextMessage"/> sizing. The message must grow
///     to fit all of its Markdown content (inside the border) without requiring an unrelated
///     layout — previously the last streamed chunk stayed clipped until the message was scrolled
///     off and back on screen.
/// </summary>
[Collection ("TerminalGuiGlobalState")]
public class TextMessageLayoutTests
{
    [Fact]
    public void StreamedContent_IsVisibleWithoutExternalRedraw ()
    {
        string? previous = Environment.GetEnvironmentVariable ("DisableRealDriverIO");
        Environment.SetEnvironmentVariable ("DisableRealDriverIO", "1");

        try
        {
            using IApplication app = Application.Create ().Init (DriverRegistry.Names.ANSI);
            app.Driver!.SetScreenSize (44, 24);

            var view = new MessageView (NullLogger<MessageView>.Instance) { App = app };
            view.Frame = app.Screen;

            var message = new TextMessage (new TextContent ("first paragraph"));
            view.AddMessage (message);
            view.Layout ();

            // Stream a final chunk containing a distinctive line.
            message.SetContent (new TextContent ("\n\nsecond paragraph\n\nFINAL-STREAMED-LINE"));

            // Emulate subsequent application iterations: timers first, then layout.
            for (var i = 0; i < 3; i++)
            {
                app.TimedEvents!.RunTimers ();

                if (view.NeedsLayout)
                {
                    view.Layout ();
                }
            }

            // The message must have grown to fit the streamed content, and the final line must
            // be rendered inside it. (The pre-fix code left the line clipped below the border,
            // so it did not appear in the driver output.)
            app.Driver.ClearContents ();
            app.Driver.Clip = new Region (app.Screen);
            view.Draw ();
            app.Driver.Refresh ();

            Assert.Contains ("FINAL-STREAMED-LINE", app.Driver.ToAnsi ());
        }
        finally
        {
            Environment.SetEnvironmentVariable ("DisableRealDriverIO", previous);
        }
    }
}
