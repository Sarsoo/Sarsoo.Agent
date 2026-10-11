using Sarsoo.Agent.CLI.Session;
using Sarsoo.Agent.Session;
using Sarsoo.Agent.Session.Orchestration;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Sarsoo.Agent.CLI.UI;

public class AppStatusBar: StatusBar
{
    private readonly AgentWorkspace _workspace;
    private readonly Label _agentState = new()
    {
        Text = "Idle",
        CanFocus = false,
    };

    private AgentSessionRunner? _subscribed;

    public AppStatusBar(AgentWorkspace workspace)
    {
        _workspace = workspace;
        Width = Dim.Fill();
        // Height = Dim.Absolute(3);
        Y = Pos.AnchorEnd();

        SchemeName = "StatusBarIdle";
        
        Add(_agentState);

        _workspace.ActiveChanged += OnActiveChanged;
        OnActiveChanged(this, _workspace.Active);

        // Colours come from the active theme's Menu scheme (StatusBar defaults to Schemes.Menu).
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _workspace.ActiveChanged -= OnActiveChanged;

            if (_subscribed is { } subscribed)
            {
                subscribed.StateChanged -= OnStateChanged;
            }
        }
        
        base.Dispose(disposing);
    }

    private void OnActiveChanged(object? sender, AgentTab? tab)
    {
        if (_subscribed is { } previous)
        {
            previous.StateChanged -= OnStateChanged;
        }

        _subscribed = tab?.Runner;

        if (_subscribed is { } current)
        {
            current.StateChanged += OnStateChanged;
        }

        OnStateChanged(this, _subscribed?.State ?? AgentState.Idle);
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
            AgentState.Blocked => "Blocked",
            AgentState.Idle or _ => "Idle",
        };
        
        SetNeedsDraw();
    }
}
