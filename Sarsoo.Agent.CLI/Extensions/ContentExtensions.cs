using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Sarsoo.Agent.CLI.UI.Chat.Message.Agent;

namespace Sarsoo.Agent.CLI.Extensions;

public static class ContentExtensions
{
    extension(AIContent content)
    {
        public AgentMessage? GetNewMessage()
        {
            return content switch
            {
                null => null,
                UsageContent => null,
                TextContent textContent => new TextMessage(textContent),
                TextReasoningContent textReasoningContent => new ReasoningMessage(textReasoningContent),
                AlwaysApproveToolApprovalResponseContent alwaysApproveToolApprovalResponseContent =>
                    new AgentMessage<AIContent>(alwaysApproveToolApprovalResponseContent),
                CodeInterpreterToolCallContent codeInterpreterToolCallContent => new AgentMessage<AIContent>(codeInterpreterToolCallContent),
                CodeInterpreterToolResultContent codeInterpreterToolResultContent =>
                    new AgentMessage<AIContent>(codeInterpreterToolResultContent),
                DataContent dataContent => new AgentMessage<AIContent>(dataContent),
                ErrorContent errorContent => new AgentMessage<AIContent>(errorContent),
                FunctionCallContent functionCallContent => new FunctionCallMessage(functionCallContent),
                FunctionResultContent functionResultContent => new FunctionResultMessage(functionResultContent),
                HostedFileContent hostedFileContent => new AgentMessage<AIContent>(hostedFileContent),
                HostedVectorStoreContent hostedVectorStoreContent => new AgentMessage<AIContent>(hostedVectorStoreContent),
                ImageGenerationToolCallContent imageGenerationToolCallContent => new AgentMessage<AIContent>(imageGenerationToolCallContent),
                ImageGenerationToolResultContent imageGenerationToolResultContent =>
                    new AgentMessage<AIContent>(imageGenerationToolResultContent),
                ToolApprovalRequestContent toolApprovalRequestContent => new AgentMessage<AIContent>(toolApprovalRequestContent),
                InputRequestContent inputRequestContent => new AgentMessage<AIContent>(inputRequestContent),
                ToolApprovalResponseContent toolApprovalResponseContent => new AgentMessage<AIContent>(toolApprovalResponseContent),
                InputResponseContent inputResponseContent => new AgentMessage<AIContent>(inputResponseContent),
                McpServerToolCallContent mcpServerToolCallContent => new AgentMessage<AIContent>(mcpServerToolCallContent),
                McpServerToolResultContent mcpServerToolResultContent => new AgentMessage<AIContent>(mcpServerToolResultContent),
                WebSearchToolCallContent webSearchToolCallContent => new AgentMessage<AIContent>(webSearchToolCallContent),
                ToolCallContent toolCallContent => new AgentMessage<AIContent>(toolCallContent),
                WebSearchToolResultContent webSearchToolResultContent => new AgentMessage<AIContent>(webSearchToolResultContent),
                ToolResultContent toolResultContent => new AgentMessage<AIContent>(toolResultContent),
                UriContent uriContent => new AgentMessage<AIContent>(uriContent),
                _ => new AgentMessage()
            };
        }
    }
}