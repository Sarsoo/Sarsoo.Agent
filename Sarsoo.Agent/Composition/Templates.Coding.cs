using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sarsoo.Agent.Session;

namespace Sarsoo.Agent.Composition;

public static partial class Templates
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddCodingAgent() =>
            services.AddSingleton(new AgentProfile(
                "coding",
                "Coding agent with shell, filesystem, AGENTS.md and skills.",
                builder => builder.AsCodingAgent()
                )
        );
    }
    
    extension(IChatClient client)
    {
        public AgentSessionContext AsCodingAgent(
            string name,
            ILoggerFactory? loggerFactory = null,
            IServiceProvider? serviceProvider = null
        )
            => new AgentBuilder(client, loggerFactory, serviceProvider)
                .AsCodingAgent()
                .Build(name);
    }
    
    extension(AgentBuilder builder)
    {
        public AgentBuilder AsCodingAgent()
            => builder
                .WithSystemPrompt("""
                                  You are an expert coding assistant operating inside reeve, a coding agent harness. 
                                  You help users by reading files, executing commands, editing code, and writing new files.
                                  
                                  # Tool Choice
                                  - You have access to run arbitrary commands with the `bash` tool. 
                                  - You also have filesystem access provided by the `file_access_*` tools.
                                  - Every bash tool invocation prompts for explicit approval from the user.
                                  - Readonly `file_access_*` tools do not prompt the user.
                                  - Prefer using the `file_access_*` tools for file system operations.
                                  - If a bash command could be achieved with the `file_access_*` tools, do not use the `bash` tool, even if 
                                    this results in multiple `file_access_*` tool invocations.
                                  - Only use the `bash` tool for commands that cannot be accomplished with the `file_access_*` tools 
                                    including command executions and complex file operations using bash-only constructs.
                                    - Valid examples = piping/globbing/"find -exec" and similar
                                  """)
                .WithBashShell()
                .WithAdvisor()
                .WithFileSystem()
                .WithAgentsMd()
                .WithSkills();
    }
}