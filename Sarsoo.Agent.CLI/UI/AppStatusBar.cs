using Sarsoo.Agent.Session;
using Sarsoo.Agent.Session.Orchestration;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Sarsoo.Agent.CLI.UI;

public class AppStatusBar: StatusBar
{
    private readonly AgentInteractiveLifetime _agentSessionLifetime;

    private readonly Label _agentState = new()
    {
        Text = "Idle",
        CanFocus = false,
    };
    
    public AppStatusBar(AgentInteractiveLifetime agentSessionLifetime)
    {
        _agentSessionLifetime = agentSessionLifetime;
        Width = Dim.Fill();
        // Height = Dim.Absolute(3);
        Y = Pos.AnchorEnd();

        SchemeName = "StatusBarIdle";
        
        Add(_agentState);
        
        _agentSessionLifetime.StateChanged += OnStateChanged;

        // Colours come from the active theme's Menu scheme (StatusBar defaults to Schemes.Menu).
    }

    protected override void Dispose(bool disposing)
    {
        _agentSessionLifetime.StateChanged -= OnStateChanged;
        
        base.Dispose(disposing);
    }

    private void OnStateChanged(object? sender, AgentState e)
    {
        SchemeName = e switch
        {
            AgentState.Busy => "StatusBarBusy",
            AgentState.Idle or _ => "StatusBarIdle",
        };
        
        _agentState.Text = e switch
        {
            AgentState.Busy => "Working...",
            AgentState.Idle or _ => "Idle",
        };
        
        SetNeedsDraw();
    }
}