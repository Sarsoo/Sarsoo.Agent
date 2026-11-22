using Sarsoo.Agent.Tool.Policy;

namespace Sarsoo.Agent.Tool;

public partial class CommandTool
{
    public static CommandTool GetReadonlyKubectl() => new("kubectl", new ReadOnlyKubectlCommandPolicy());
    public static CommandTool GetReadonlyGcx() => new("gcx", new ReadOnlyGcxCommandPolicy());
    public static CommandTool GetReadonlyAws() => new("aws", new ReadOnlyAwsCommandPolicy());
}