using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sarsoo.Agent.CLI.Session;
using Sarsoo.Agent.CLI.UI;
using Sarsoo.Agent.CLI.UI.Chat;
using Sarsoo.Agent.CLI.UI.Chat.Message;
using Sarsoo.Agent.CLI.UI.Menu;
using Sarsoo.Agent.CLI.UI.Stats;
using Sarsoo.Agent.Extensions;
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
                .AddSingleton<AppMenu>()
                .AddSingleton<AppStatusBar>()
                
                .AddSingleton<ChatView>()
                .AddSingleton<StatsBarView>()
                .AddSingleton<MessageView>()
                .AddSingleton<ChatTextField>();
            
            return serviceCollection;
        }
        
        public IServiceCollection AddAgentSessions(IConfiguration configuration)
        {
            serviceCollection.AddAgent(configuration)
                .AddSingleton<AgentSessionLifetime>()
                .AddHostedService<AgentSessionLifetime>(sp => sp.GetRequiredService<AgentSessionLifetime>());
            
            return serviceCollection;
        }
    }
}