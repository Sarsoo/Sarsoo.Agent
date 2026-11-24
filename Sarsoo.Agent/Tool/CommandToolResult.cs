namespace Sarsoo.Agent.Tool;

public sealed class CommandToolResult
{
    public string? StdOut { get; internal init; }
    public string? StdError { get; internal init; }
    public int? ExitCode { get; internal init; }
    public bool Success { get; internal init; }
    public string? Error { get; internal init; }

    // public static async Task<ToolResult<T>> From(Func<Task<T?>> func)
    // {
    //     var result = new ToolResult<T>();
    //
    //     try
    //     {
    //         result.Result = await func();
    //         result.Success = true;
    //     }
    //     catch (Exception e)
    //     {
    //         result.Success = false;
    //         result.Error = e.ToString();
    //     }
    //
    //     return result;
    // }

    public static CommandToolResult FromError(string error) =>
        new()
        {
            Error = error,
            Success = false
        };
}