namespace Sarsoo.Agent.Context.Hindsight.Model;

public class RecallRequest
{
    public string Query { get; set; }
    public string? Budget { get; set; }
    public int? MaxTokens { get; set; }
    public DateTime? Timestamp { get; set; }
    public List<string>? Tags { get; set; }
    public string? TagsMatch { get; set; }
}