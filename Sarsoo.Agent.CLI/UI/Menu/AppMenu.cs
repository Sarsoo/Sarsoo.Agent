using Terminal.Gui.Input;
using Terminal.Gui.Views;

namespace Sarsoo.Agent.CLI.UI.Menu;

public class AppMenu: MenuBar
{
    public AppMenu()
    {
        Add(new MenuItem("_New", Key.N.WithCtrl));
    }
}