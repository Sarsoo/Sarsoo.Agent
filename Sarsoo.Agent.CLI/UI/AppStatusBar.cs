using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using Attribute = Terminal.Gui.Drawing.Attribute;

namespace Sarsoo.Agent.CLI.UI;

public class AppStatusBar: StatusBar
{
    public AppStatusBar()
    {
        Width = Dim.Fill();
        // Height = Dim.Absolute(3);
        Y = Pos.AnchorEnd();
        
        SetAttribute (new Attribute (Color.Red, Color.Black));
    }
}