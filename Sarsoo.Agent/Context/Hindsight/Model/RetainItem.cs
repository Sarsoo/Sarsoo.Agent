namespace Sarsoo.Agent.Context.Hindsight.Model;

public class RetainItem
{
    public required string Content { get; set; }
    public string? DocumentId { get; set; }
    public string? Context { get; set; }
    public DateTime? Timestamp { get; set; }
    public bool? ResolveEntities { get; set; }
    public List<string>? Tags { get; set; }
    public List<string>? ObservationScopes { get; set; }
    public string? Strategy { get; set; }
    public string? UpdateMode { get; set; }
}