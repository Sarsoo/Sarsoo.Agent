using Microsoft.Extensions.AI;
using Sarsoo.Agent.Session.Stats;
using Terminal.Gui.Views;

namespace Sarsoo.Agent.CLI.Session;

public class TokenSessionStats(SessionStats stats): ITableSource
{
    public string[] ColumnNames => ["Stat", "Value"];
    public int Columns => 2;

    public object this[int row, int col] 
        => (row, col) switch
        {
            (0, 0) => "Input",
            (0, 1) => stats.Usage.InputTokenCount?.ToString() ?? "0",
            (1, 0) => "Cached",
            (1, 1) => stats.Usage.CachedInputTokenCount?.ToString() ?? "0",
            (2, 0) => "Reasoning",
            (2, 1) => stats.Usage.ReasoningTokenCount?.ToString() ?? "0",
            (3, 0) => "Output",
            (3, 1) => stats.Usage.OutputTokenCount?.ToString() ?? "0",
            (4, 0) => "Total",
            (4, 1) => stats.Usage.TotalTokenCount?.ToString() ?? "0",
        };

    public int Rows => 5;
}