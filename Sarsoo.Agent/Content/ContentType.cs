namespace Sarsoo.Agent.Content;

public enum ContentType
{
    Data,
    Error,
    FunctionCall,
    FunctionResult,
    HostedFile,
    HostedVectorStore,
    Text,
    Reasoning,
    Uri,
    Usage,
    ToolCall,
    ToolResult,
    InputRequest,
    InputResponse,
    ToolApprovalRequest,
    ToolApprovalResponse,
    McpServerToolCall,
    McpServerToolResult,
    CodeInterpreterToolCall,
    CodeInterpreterToolResult,
    WebSearchToolCall,
    WebSearchToolResult
}