using Terminal.Gui.Drawing;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;

namespace Sarsoo.Agent.CLI.UI.Chat.Message;

public class BaseMessage: View
{
    public BaseMessage()
    {
        Text = "";
        Width = Dim.Fill();
        BorderStyle = LineStyle.Dotted;
        Height = Dim.Auto();
        
        // Bind mouse wheel to scroll commands — no AddCommand needed.
        // The unhandled command will bubble up to the nearest ancestor
        // whose CommandsToBubbleUp includes it.
        MouseBindings.Add (MouseFlags.WheeledUp, Command.ScrollUp);
        MouseBindings.Add (MouseFlags.WheeledDown, Command.ScrollDown);
    }
}