using Sarsoo.Agent.CLI.UI.Chat;
using Sarsoo.Agent.CLI.UI.Menu;
using Sarsoo.Agent.CLI.UI.Stats;
using Terminal.Gui.Drawing;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Sarsoo.Agent.CLI.UI;

public class MainWindow: Window
{
    private readonly AppMenu _menu;
    private readonly AppStatusBar _appStatus;
    private readonly ChatView _chat;
    private readonly StatsSideBarView _statsSide;

    public MainWindow(
        AppMenu menu, 
        AppStatusBar appStatus, 
        ChatView chat,
        StatsSideBarView statsSide
    )
    {
        _menu = menu;
        _appStatus = appStatus;
        _chat = chat;
        _statsSide = statsSide;
        
        Title = "reeve";
        BorderStyle = LineStyle.None;

        var container = new View()
        {
            Y = Pos.Bottom(menu),
            Height = Dim.Fill(appStatus),
            Width = Dim.Fill(),
            CanFocus = true
        };

        chat.Height = Dim.Fill();
        chat.Width = Dim.Percent(80);
        statsSide.Height = Dim.Fill();
        statsSide.Width = Dim.Fill();
        
        statsSide.X = Pos.Right(chat);
        
        container.Add(chat, statsSide);
        Add(menu, container, appStatus);
        
        _chat.FocusText();
    }
}