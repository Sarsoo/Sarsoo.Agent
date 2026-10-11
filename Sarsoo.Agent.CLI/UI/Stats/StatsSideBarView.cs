using Sarsoo.Agent.CLI.Session;
using Sarsoo.Agent.Session;
using Terminal.Gui.Configuration;
using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Sarsoo.Agent.CLI.UI.Stats;

public class StatsSideBarView: View
{
    private readonly View _tokensLabel = new()
    {
        Text = "Tokens",
        TextAlignment = Alignment.Center,
        BorderStyle = LineStyle.Double,
        Width = Dim.Fill(),
        Height = Dim.Auto(),
    };

    private TableView _tableView = new()
    {
        Width = Dim.Fill(),
        Height = Dim.Auto(),
        Style = new()
        {
            ShowHorizontalBottomLine = false,
            
            ShowHeaders = false,
            ShowHorizontalHeaderUnderline = false,
            ShowHorizontalHeaderOverline = false,
            ShowVerticalHeaderLines = false,
            
            ShowVerticalCellLineForFirstColumn = false,
            ShowVerticalCellLineForLastColumn = false,
            ExpandLastColumn = false,
            
            ColumnStyles = new Dictionary<int, ColumnStyle>
            {
                {0, new ColumnStyle()
                {
                    Alignment = Alignment.End,
                }},
                {1, new ColumnStyle()
                {
                    Alignment = Alignment.Start
                }}
            }
        }
    };
    
    public StatsSideBarView(
        StatsTables statsTables
    )
    {
        CanFocus = false;
        BorderStyle = LineStyle.RoundedDotted;
        
        _tableView.Table = statsTables.Tokens;

        _tableView.Y = Pos.Bottom(_tokensLabel);
        Add(_tokensLabel, _tableView);
    }
}