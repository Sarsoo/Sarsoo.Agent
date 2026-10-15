namespace Sarsoo.Agent.Tool;

public sealed class ToolResult<T>
{
    public T? Result { get; private set; }
    public bool Success { get; private set; }
    public string? Error { get; private set; }

    public static async Task<ToolResult<T>> From(Func<Task<T?>> func)
    {
        var result = new ToolResult<T>();
        
        try
        {
            result.Result = await func();
            result.Success = true;
        }
        catch (Exception e)
        {
            result.Success = false;
            result.Error = e.ToString();
        }

        return result;
    }
    
    public static ToolResult<T> FromError(string error) =>
        new()
        {
            Error = error,
            Success = false,
            Result = default
        };
}