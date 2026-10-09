using System.Runtime.CompilerServices;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Sarsoo.Agent.Content;

namespace Sarsoo.Agent.Extensions;

public static class MessageExtensions
{
    extension(AgentResponseUpdate update)
    {
        public string Thinking
        {
            get
            {
                var list = update.Contents;
                    
                DefaultInterpolatedStringHandler builder = new(list.Count, 0, null, stackalloc char[512]);
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i] is TextReasoningContent text)
                    {
                        builder.AppendLiteral(text.Text);
                    }
                }

                return builder.ToStringAndClear();
            }
        }
    }

    extension(AIContent content)
    {
        public ContentType ContentType
        {
            get
            {
                return content switch
                {
                    AlwaysApproveToolApprovalResponseContent => throw new NotImplementedException(),
                    CodeInterpreterToolCallContent => ContentType.CodeInterpreterToolCall,
                    CodeInterpreterToolResultContent => ContentType.CodeInterpreterToolResult,
                    DataContent => ContentType.Data,
                    ErrorContent => ContentType.Error,
                    FunctionCallContent => ContentType.FunctionCall,
                    FunctionResultContent => ContentType.FunctionResult,
                    HostedFileContent => ContentType.HostedFile,
                    HostedVectorStoreContent => ContentType.HostedVectorStore,
                    ImageGenerationToolCallContent => throw new NotImplementedException(),
                    ImageGenerationToolResultContent => throw new NotImplementedException(),
                    ToolApprovalRequestContent => ContentType.ToolApprovalRequest,
                    ToolApprovalResponseContent => ContentType.ToolApprovalResponse,
                    InputRequestContent => ContentType.InputRequest,
                    InputResponseContent => ContentType.InputResponse,
                    McpServerToolCallContent => ContentType.McpServerToolCall,
                    McpServerToolResultContent => ContentType.McpServerToolResult,
                    TextContent => ContentType.Text,
                    TextReasoningContent => ContentType.Reasoning,
                    WebSearchToolCallContent => ContentType.WebSearchToolCall,
                    WebSearchToolResultContent => ContentType.WebSearchToolResult,
                    ToolCallContent => ContentType.ToolCall,
                    ToolResultContent => ContentType.ToolResult,
                    UriContent => ContentType.Uri,
                    UsageContent => ContentType.Usage,
                    _ => throw new ArgumentOutOfRangeException(nameof(content))
                };
            }
        }
    }
}