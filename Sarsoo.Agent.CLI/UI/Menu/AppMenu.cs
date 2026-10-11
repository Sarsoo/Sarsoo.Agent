using Sarsoo.Agent.CLI.Session;
using Sarsoo.Agent.Composition;
using Terminal.Gui.Views;

namespace Sarsoo.Agent.CLI.UI.Menu;

public class AppMenu: MenuBar
{
    public AppMenu(IAgentProfileRegistry profiles, AgentWorkspace workspace)
    {
        var items = profiles.Profiles
            .Select(profile => new MenuItem(
                profile.Name,
                profile.Description,
                () => workspace.Create(profile.Name)))
            .ToArray();

        Add(new MenuBarItem("_New", new PopoverMenu(items)));
    }
}
