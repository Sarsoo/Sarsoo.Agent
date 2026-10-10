using Sarsoo.Agent.CLI.Session;
using Terminal.Gui.Configuration;
using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Sarsoo.Agent.CLI.UI.Stats;

public class StatsSideBarView: View
{
    private readonly TableSessionStats _stats;
    
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
        Width = Dim.Percent(100),
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
    
    public StatsSideBarView(TableSessionStats stats)
    {
        _stats = stats;
        
        CanFocus = false;
        BorderStyle = LineStyle.RoundedDotted;
        
        _tableView.Table = stats.Tokens;
        stats.Updated += OnStatsOnUpdated;

        _tableView.Y = Pos.Bottom(_tokensLabel);
        Add(_tokensLabel, _tableView);
    }

    private void OnStatsOnUpdated(object? sender, EventArgs args)
    {
        _tableView.SetNeedsDraw();
    }

    protected override void Dispose(bool disposing)
    {
        _stats.Updated -= OnStatsOnUpdated;
        
        base.Dispose(disposing);
    }
}