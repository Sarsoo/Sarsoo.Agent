using Microsoft.Extensions.AI;

namespace Sarsoo.Agent.Advisor;

public class AdvisorSharedContext
{
    private List<ChatMessage> _recentMessages = new();
    private Lock _lock = new();
    
    public void SetContext(List<ChatMessage> newContext)
    {
        lock (_lock)
        {
            _recentMessages.AddRange(newContext);
            _recentMessages = [.. _recentMessages.TakeLast(20)];
        }
    }

    public IReadOnlyList<ChatMessage> GetContext()
    {
        IReadOnlyList<ChatMessage>? context = null;
        lock (_lock)
        {
            context = _recentMessages.AsReadOnly();
        }
        
        return context;
    }
}