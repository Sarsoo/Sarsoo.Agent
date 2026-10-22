using System.Diagnostics;
using System.Text;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;

namespace Sarsoo.Agent.CLI.UI.Chat.Message;

public class BaseMessage: View
{
    /// <summary>
    ///     Minimum interval between coalesced UI updates. Bursts of updates arriving faster than this
    ///     are collapsed into a single apply of the latest snapshot. The first update in an idle
    ///     period is applied on the next iteration rather than after the interval.
    /// </summary>
    public static TimeSpan UpdateInterval { get; set; } = TimeSpan.FromMilliseconds (33);

    private readonly Lock _updateLock = new ();
    private bool _updateDirty;
    private bool _updateScheduled;
    private int _updateGeneration;
    private long _nextApplyTicks;
    private IApplication? _updateApp;
    private object? _updateToken;

    /// <summary>
    ///     Raised on the UI thread after a coalesced content update has been applied.
    /// </summary>
    public event EventHandler? ContentChanged;
    public override Rune HotKeySpecifier => (Rune)'\xffff';

    public BaseMessage()
    {
        Text = "";
        Width = Dim.Fill();
        BorderStyle = LineStyle.Dotted;
        Height = Dim.Auto();
        
        // Bind mouse wheel to scroll commands — no AddCommand needed.
        // The unhandled command will bubble up to the nearest ancestor
        // whose CommandsToBubbleUp includes it.
        MouseBindings.Add (MouseFlags.WheeledUp, Command.ScrollUp);
        MouseBindings.Add (MouseFlags.WheeledDown, Command.ScrollDown);

        Disposing += (_, _) => CancelPendingUpdate ();
    }

    /// <summary>
    ///     Raises <see cref="ContentChanged"/> so the containing view can react (e.g. lay out).
    /// </summary>
    protected void RaiseContentChanged() => ContentChanged?.Invoke(this, EventArgs.Empty);

    /// <summary>
    ///     Marks this message's visual state as stale and schedules at most one
    ///     <see cref="ApplyContentUpdate"/> for the next UI tick.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Streamed updates are snapshots, not events: only the newest one matters. Marshalling
    ///         every one through <see cref="IApplication.Invoke"/> queues an unbounded number of
    ///         zero-delay timeouts, and <c>TimedEvents.RunTimers</c> drains all due callbacks in a
    ///         single main-loop iteration — so a producer that outpaces the drain keeps the loop from
    ///         ever drawing or reading input. Coalescing bounds the queue to one entry per message and
    ///         rates the work to <see cref="UpdateInterval"/>, while still guaranteeing the latest
    ///         snapshot is applied.
    ///     </para>
    ///     <para>Safe to call from any thread.</para>
    /// </remarks>
    protected void RequestContentUpdate ()
    {
        lock (_updateLock)
        {
            _updateDirty = true;

            IApplication? app = App;

            if (_updateScheduled)
            {
                // Same owner: the queued apply will read the snapshot set by the caller.
                if (app is null || ReferenceEquals (_updateApp, app))
                {
                    return;
                }

                // The message now belongs to a different application. Abandon the old schedule
                // (its callback no-ops via the generation check) and schedule on the new one.
                _updateGeneration++;
                _updateScheduled = false;

                if (_updateApp is { } previousApp && _updateToken is { } previousToken)
                {
                    previousApp.RemoveTimeout (previousToken);
                }

                _updateApp = null;
                _updateToken = null;
            }

            if (app is null)
            {
                // Not in a running application yet (the message is added to the tree on the UI
                // thread). Stay dirty and leave scheduling to the first layout pass; the flag is
                // not latched here because there is nothing to wait for.
                return;
            }

            _updateScheduled = true;
            _updateApp = app;

            // Leading edge: apply on the next iteration, then no more often than UpdateInterval
            // while the burst lasts. A zero-delay timeout is due immediately, so the main loop's
            // timer drain still terminates after it — later updates are scheduled in the future.
            long now = Stopwatch.GetTimestamp ();
            long waitTicks = Math.Max (0, _nextApplyTicks - now);
            int generation = ++_updateGeneration;

            // Terminal.Gui's TimedEvents.AddTimeout only enqueues under its own lock (it never
            // runs a callback inline), so scheduling from a producer thread while holding this
            // lock cannot re-enter Flush or invert lock order.
            _updateToken = app.AddTimeout (TimeSpan.FromSeconds ((double)waitTicks / Stopwatch.Frequency),
                                           () => Flush (generation));
        }
    }

    /// <summary>
    ///     Applies the latest snapshot to the view. Runs on the application's UI thread.
    /// </summary>
    protected virtual void ApplyContentUpdate () { }

    private bool Flush (int generation)
    {
        lock (_updateLock)
        {
            // A callback from a superseded schedule (message re-attached, or the pending update was
            // cancelled) must not clear the state of the schedule that replaced it.
            if (generation != _updateGeneration)
            {
                return false;
            }

            _updateScheduled = false;
            _updateApp = null;
            _updateToken = null;
            _nextApplyTicks = Stopwatch.GetTimestamp () + (long)(UpdateInterval.TotalSeconds * Stopwatch.Frequency);

            if (!_updateDirty)
            {
                return false;
            }

            // Cleared before the snapshot is read: an update arriving while ApplyContentUpdate
            // runs sees _updateScheduled == false and schedules a fresh apply instead of being lost.
            _updateDirty = false;
        }

        ApplyContentUpdate ();

        // Raised on the UI thread, once per applied snapshot rather than once per streamed chunk.
        // Consumers (e.g. the message list) can therefore touch the view tree without marshalling.
        RaiseContentChanged ();

        return false;
    }

    /// <inheritdoc/>
    /// <remarks>
    ///     Retries an update that was requested before the message had an application. It must not
    ///     apply inline: mutating the content during layout raises <c>ContentSizeChanged</c> and
    ///     asks for another layout pass from inside the current one. Instead the pending flag is
    ///     released and the update is re-scheduled onto a normal timer tick.
    /// </remarks>
    protected override void OnSubViewLayout (LayoutEventArgs args)
    {
        base.OnSubViewLayout (args);

        bool reschedule;

        lock (_updateLock)
        {
            reschedule = _updateDirty && !_updateScheduled;
        }

        if (reschedule)
        {
            RequestContentUpdate ();
        }
    }

    private void CancelPendingUpdate ()
    {
        IApplication? app;
        object? token;

        lock (_updateLock)
        {
            app = _updateApp;
            token = _updateToken;
            _updateApp = null;
            _updateToken = null;
            _updateScheduled = false;
            _updateDirty = false;
            _updateGeneration++;
        }

        if (app is { } scheduledApp && token is { })
        {
            scheduledApp.RemoveTimeout (token);
        }
    }
}
