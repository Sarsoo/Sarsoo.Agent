using Sarsoo.Agent.Tool.Policy;

namespace Sarsoo.Agent.Tool;

public partial class CommandTool
{
    public static CommandTool GetReadonlyGcx() => new("gcx", new ReadOnlyGcxCommandPolicy());
}