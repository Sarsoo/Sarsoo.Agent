namespace Sarsoo.Agent.Context.Hindsight.Model;

public class RetainResponse
{
    public bool Success { get; set; }
    public string BankId { get; set; }
    public int ItemsCount { get; set; }
    public bool Async { get; set; }
    public string? OperationId { get; set; }
    public List<string>? OperationIds { get; set; }
}