using Sarsoo.Agent.Session;
using Terminal.Gui.Views;

namespace Sarsoo.Agent.CLI.Session;

public class TokenSessionStats(AgentSessionContext session): ITableSource
{
    public string[] ColumnNames => ["Stat", "Value"];
    public int Columns => 2;

    public object this[int row, int col] 
        => (row, col) switch
        {
            (0, 0) => "Input",
            (0, 1) => session.Stats.Usage.InputTokenCount?.ToString("N0") ?? "0",
            (1, 0) => "Cached",
            (1, 1) => session.Stats.Usage.CachedInputTokenCount?.ToString("N0") ?? "0",
            (2, 0) => "Reasoning",
            (2, 1) => session.Stats.Usage.ReasoningTokenCount?.ToString("N0") ?? "0",
            (3, 0) => "Output",
            (3, 1) => session.Stats.Usage.OutputTokenCount?.ToString("N0") ?? "0",
            (4, 0) => "Total",
            (4, 1) => session.Stats.Usage.TotalTokenCount?.ToString("N0") ?? "0",
            _ => "-"
        };

    public int Rows => 5;
}
