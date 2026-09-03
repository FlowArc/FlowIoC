using System;
using Modules.CounterModule.Data.ValueObjects;

namespace Modules.CounterModule.Services
{
    /// <summary>
    /// Runs named counters and calls back once a second while they last. A counter runs in
    /// either direction: <see cref="CountDownFrom(CounterRequestVO)"/> counts towards zero, and
    /// <see cref="CountUpFrom(CounterRequestVO)"/> measures forward from a moment already past.
    /// A counter is identified by a string, and several callers may listen to the same one: the
    /// first caller sets how long it runs, the rest only add their callbacks to it.
    ///
    /// Every method may be called before the service is active. What is asked for then is held,
    /// and starts together the moment a time source answers - which is what the checkActive
    /// callback is for.
    /// </summary>
    public interface ICounterService
    {
        /// <summary>True once a time source has answered and counters are ticking.</summary>
        bool IsActive();

        /// <summary>
        /// The time the module is working from, or null while it is not active. Read the time
        /// from here rather than from the device, so that swapping in a server time source
        /// changes every reading at once.
        /// </summary>
        DateTime? GetTime();

        /// <summary>
        /// Counts down from a moment already known - resuming a session, or following a timer
        /// the server owns. Returns false when the request makes no sense, such as a duration of
        /// zero or less.
        /// </summary>
        /// <param name="id">Identifies the counter. An id already running only gains the callbacks.</param>
        /// <param name="duration">How long the countdown runs, in seconds.</param>
        /// <param name="startTime">The UTC moment it began.</param>
        /// <param name="checkActive">Told whether the service is active now, and told again when it becomes active.</param>
        /// <param name="isPercentageTick">True to have <paramref name="counterTick"/> receive 0..1 instead of seconds left.</param>
        /// <param name="counterTick">Called each second with the time left.</param>
        /// <param name="counterComplete">Called once the countdown reaches zero.</param>
        /// <param name="counterStop">Called if the counter is stopped before it finishes.</param>
        /// <param name="elapsedTimeTick">Called each second with the time since it began.</param>
        bool CountDownFrom(string id, int duration, DateTime startTime, Action<bool> checkActive,
            bool isPercentageTick = false, Action<float> counterTick = null, Action counterComplete = null,
            Action counterStop = null, Action<float> elapsedTimeTick = null);

        /// <summary>The same request, already assembled.</summary>
        bool CountDownFrom(CounterRequestVO request);

        /// <summary>
        /// Counts down from this moment. Returns the moment it started from, or null when
        /// the service is not active yet - there is no trustworthy "now" to start from, so use
        /// <see cref="CountDownFrom(CounterRequestVO)"/> with a known start time instead.
        /// </summary>
        DateTime? CountDownInstantly(string id, int duration, bool isPercentageTick = false,
            Action<float> counterTick = null, Action counterComplete = null,
            Action counterStop = null, Action<float> elapsedTimeTick = null);

        /// <summary>
        /// Adds callbacks to a counter that is already running, without restarting it. Does
        /// nothing but log when no counter is running under that id.
        /// </summary>
        void AddCallbacks(string id, Action<float> counterTick = null, bool isPercentageTick = false,
            Action counterComplete = null, Action counterStop = null, Action<float> elapsedTimeTick = null);

        /// <summary>
        /// Removes callbacks previously added. A counter left with nothing listening to it is
        /// dropped, so a screen that unsubscribes on close leaves nothing running behind it.
        /// </summary>
        void RemoveCallbacks(string id, Action<float> counterTick = null, bool isPercentageTick = false,
            Action counterComplete = null, Action counterStop = null, Action<float> elapsedTimeTick = null);

        /// <summary>
        /// Measures time forward from a moment in the past rather than counting down to one.
        /// Leave <paramref name="duration"/> at zero to measure for as long as the game runs.
        /// Returns false when the start time is not actually in the past.
        /// </summary>
        bool CountUpFrom(string id, DateTime startTime, Action<bool> checkActive, int duration = 0,
            Action<float> elapsedTimeTick = null, Action counterComplete = null, Action counterStop = null);

        /// <summary>The same request, already assembled.</summary>
        bool CountUpFrom(CounterRequestVO request);

        /// <summary>
        /// Ends a counter now. Its stop callbacks run, its complete callbacks do not.
        /// </summary>
        void Stop(string id);
    }
}
