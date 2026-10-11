using Sarsoo.Agent.CLI.Session;
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

    private readonly TableView _tableView = new()
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
            ExpandLastColumn = true,
            
            ColumnStyles = new Dictionary<int, ColumnStyle>
            {
                {0, new ColumnStyle()
                {
                    Alignment = Alignment.Start,
                }},
                {1, new ColumnStyle()
                {
                    Alignment = Alignment.End
                }}
            }
        }
    };

    private readonly AgentWorkspace _workspace;
    
    public StatsSideBarView(AgentWorkspace workspace)
    {
        _workspace = workspace;
        CanFocus = false;
        BorderStyle = LineStyle.RoundedDotted;
        
        _tableView.Y = Pos.Bottom(_tokensLabel);
        
        Add(_tokensLabel, _tableView);

        _workspace.ActiveChanged += OnActiveChanged;
        OnActiveChanged(this, _workspace.Active);
    }

    public void Bind(TokenSessionStats? stats)
    {
        _tableView.Table = stats;
        _tableView.SetNeedsDraw();
    }

    private void OnActiveChanged(object? sender, AgentTab? tab) => Bind(tab?.Stats);

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _workspace.ActiveChanged -= OnActiveChanged;
        }
        
        base.Dispose(disposing);
    }
}
