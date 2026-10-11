using Microsoft.Extensions.AI;

namespace Sarsoo.Agent.Options;

/// <summary>
///     Configuration for the advisor agent that the coding agent can consult as a tool. The advisor
///     has no tools of its own and can be pointed at a different model and thinking level.
/// </summary>
public class AdvisorOptions
{
    public const string Key = "Advisor";

    public const string DefaultToolName = "consult_advisor";

    public const string DefaultSystemPrompt = """
        You are a senior technical advisor. Another AI agent that is actively working on a coding task
        consults you for a second opinion.

        You have no tools: you cannot read files, run commands or inspect the repository or environment.
        You only know what the calling agent tells you, so treat its description as the full context and
        say so if it is missing something you would need.

        Give concise, high-signal guidance: the key trade-offs, the risks, and a recommended course of
        action. Prefer a clear recommendation over a survey of options. Do not ask clarifying questions;
        state your assumptions and answer.
        """;

    public const string DefaultToolDescription = """
        Consult a separate advisor model for a second opinion on a hard problem. The advisor has no tools
        and cannot inspect the repository or environment, so include all relevant context in the question.
        Use this when you are stuck, unsure about a design decision, or want to validate an approach before
        proceeding.
        """;

    public bool Enabled { get; set; } = true;

    /// <summary>Model for the advisor. Defaults to the main provider model when unset.</summary>
    public string? Model { get; set; }

    /// <summary>Reasoning effort for the advisor. Defaults to the provider default when unset.</summary>
    public ReasoningEffort? ThinkingLevel { get; set; }

    public string? SystemPrompt { get; set; }

    public string ToolName { get; set; } = DefaultToolName;

    public string? ToolDescription { get; set; }
}
