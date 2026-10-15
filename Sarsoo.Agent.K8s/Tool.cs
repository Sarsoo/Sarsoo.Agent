namespace Sarsoo.Agent.K8s;

public partial class Tool
{
    private readonly ClientIndex _index;

    public Tool(ClientIndex index)
    {
        _index = index;
    }
}