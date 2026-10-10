using Microsoft.Extensions.Logging;
using Sarsoo.Agent.CLI.Session;
using Sarsoo.Agent.CLI.UI.Chat.Tool;
using Sarsoo.Agent.Session.Orchestration;
using Terminal.Gui.Drawing;
using Terminal.Gui.Input;
using Terminal.Gui.Resources;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Sarsoo.Agent.CLI.UI.Chat;

public class ChatTextField: TextField
{
    private readonly AgentInteractiveLifetime _agentSession;
    private readonly ILogger<ChatTextField> _logger;

    public ChatTextField(AgentInteractiveLifetime agentSession, ILogger<ChatTextField> logger)
    {
        _agentSession = agentSession;
        _logger = logger;
        BorderStyle = LineStyle.Single;
        
        KeyBindings.ReplaceCommands(Key.Enter, Command.Accept);
        
        AddCommand (Command.Accept, OnEnter);
    }

    private bool? OnEnter()
    {
        var message = Value;

        if (!string.IsNullOrWhiteSpace(message))
        {
            _logger.LogInformation("Passing message from UI to agent: {message}", message);
            Value = "";
            _agentSession.PassMessage(message);
        }
        
        return null;
    }
}