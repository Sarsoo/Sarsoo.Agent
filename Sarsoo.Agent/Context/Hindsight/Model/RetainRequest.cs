namespace Sarsoo.Agent.Context.Hindsight.Model;

public class RetainRequest
{
    public required List<RetainItem> Items { get; set; }
    public bool? Async { get; set; }
    public string? OperationId { get; set; }
}