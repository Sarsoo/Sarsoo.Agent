using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sarsoo.Agent.CLI.Session;
using Sarsoo.Agent.CLI.UI;
using Sarsoo.Agent.CLI.UI.Chat;
using Sarsoo.Agent.CLI.UI.Chat.Message;
using Sarsoo.Agent.CLI.UI.Menu;
using Sarsoo.Agent.CLI.UI.Stats;
using Sarsoo.Agent.Composition;
using Sarsoo.Agent.Extensions;
using Sarsoo.Agent.Gate;
using Sarsoo.Agent.Session;
using Terminal.Gui.App;

namespace Sarsoo.Agent.CLI.Extensions;

public static class ServiceExtensions
{
    extension(IServiceCollection serviceCollection)
    {
        public IServiceCollection AddUi()
        {
            serviceCollection.AddHostedService<TuiService>()

                .AddSingleton<IApplication>(_ => Application.Create())
                .AddSingleton<MainWindow>()
                .AddSingleton<IToolGate, ToolCallPrompter>()
                .AddSingleton<AppMenu>()
                .AddSingleton<AppStatusBar>()
                
                .AddSingleton<ChatView>()
                .AddSingleton<StatsSideBarView>()
                .AddSingleton<StatsTables>()
                .AddSingleton<MessageView>()
                .AddSingleton<ChatTextField>();
            
            return serviceCollection;
        }
        
        public IServiceCollection AddAgentSessions(IConfiguration configuration)
        {
            serviceCollection.AddAgent(configuration)
                .AddSingleton<AgentSessionContext>(sp => 
                    sp.GetRequiredService<IChatClient>()
                        .AsCodingAgent(serviceProvider: sp)
                )
                .AddSingleton<SessionUiBinding>()
                .AddHostedService<SessionUiBinding>(sp => sp.GetRequiredService<SessionUiBinding>());
            
            return serviceCollection;
        }
    }
}