using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;

namespace Sarsoo.Agent.CLI.UI.Stats;

public class StatsBarView: View
{
    public StatsBarView()
    {
        CanFocus = false;
        BorderStyle = LineStyle.Double;
    }
}