using System.Text;
using Terminal.Gui.Drawing;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;

namespace Sarsoo.Agent.CLI.UI.Chat.Message;

public class BaseMessage: View
{
    /// <summary>
    ///     Raised when the content of this message is updated (e.g. streamed text appended).
    /// </summary>
    public event EventHandler? ContentChanged;
    public override Rune HotKeySpecifier => (Rune)'\xffff';

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

    /// <summary>
    ///     Raises <see cref="ContentChanged"/> so the containing view can react (e.g. scroll to bottom).
    /// </summary>
    protected void RaiseContentChanged() => ContentChanged?.Invoke(this, EventArgs.Empty);
}