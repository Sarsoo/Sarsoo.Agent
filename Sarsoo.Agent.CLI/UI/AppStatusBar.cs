using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Sarsoo.Agent.CLI.UI;

public class AppStatusBar: StatusBar
{
    public AppStatusBar()
    {
        Width = Dim.Fill();
        // Height = Dim.Absolute(3);
        Y = Pos.AnchorEnd();

        // Colours come from the active theme's Menu scheme (StatusBar defaults to Schemes.Menu).
    }
}