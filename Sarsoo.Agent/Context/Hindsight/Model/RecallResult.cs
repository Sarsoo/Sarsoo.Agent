
namespace Sarsoo.Agent.Context.Hindsight.Model;

public class RecallResult
{
    public string Id { get; set; }
    public string Text { get; set; }
    public string? Type { get; set; }
    public List<string>? Entities { get; set; }
    public string? Context { get; set; }
    public string? OccurredStart { get; set; }
    public string? OccurredEnd { get; set; }
    public string? MentionedAt { get; set; }
    public string? DocumentId { get; set; }
    public string? ChunkId { get; set; }
    public List<string>? Tags { get; set; }
    public List<string>? SourceFactIds { get; set; }
}