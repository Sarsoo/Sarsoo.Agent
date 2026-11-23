using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sarsoo.Agent.CLI.Services;
using Sarsoo.Agent.CLI.Session.Inline;
using Sarsoo.Agent.CLI.Session.TUI;
using Sarsoo.Agent.CLI.UI;
using Sarsoo.Agent.CLI.UI.Inline;
using Sarsoo.Agent.CLI.UI.TUI;
using Sarsoo.Agent.CLI.UI.TUI.Chat;
using Sarsoo.Agent.CLI.UI.TUI.Menu;
using Sarsoo.Agent.CLI.UI.TUI.Stats;
using Sarsoo.Agent.Composition;
using Sarsoo.Agent.DevOps.Extensions;
using Sarsoo.Agent.Extensions;
using Sarsoo.Agent.Gate;
using Sarsoo.Agent.Session;
using Terminal.Gui.App;

namespace Sarsoo.Agent.CLI.Extensions;

public static class ServiceExtensions
{
    extension(IServiceCollection serviceCollection)
    {
        public IServiceCollection AddTui()
        {
            serviceCollection
                .AddHostedService<ConfigValidation>()
                .AddHostedService<TuiService>()

                .AddSingleton<IApplication>(_ => Application.Create())
                .AddSingleton<MainWindow>()
                .AddSingleton<IToolGate, ToolCallPrompter>()
                .AddSingleton<AppMenu>()
                .AddSingleton<AppStatusBar>()
                
                .AddSingleton<ChatView>()
                .AddSingleton<StatsSideBarView>()
                .AddSingleton<ChatTextField>()

                .AddSingleton<AgentWorkspace>();
            
            return serviceCollection;
        }
        public IServiceCollection AddInlineUi()
        {
            serviceCollection.AddHostedService<InlineService>()
                .AddSingleton<TerminalDispatcher>();
                // .AddHostedService<TestInjection>();

            return serviceCollection;
        }
        
        public IServiceCollection AddAgentSessions(IConfiguration configuration)
        {
            serviceCollection.AddAgent(configuration)
                .AddDevOpsAgent(configuration);

            return serviceCollection;
        }
    }
}