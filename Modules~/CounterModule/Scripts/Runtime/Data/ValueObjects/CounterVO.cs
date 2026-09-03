using System;
using System.Collections.Generic;

namespace Modules.CounterModule.Data.ValueObjects
{
    /// <summary>
    /// A counter that is running, and everyone listening to it. Several callers may share one
    /// id: the first one sets the duration, and the rest only add themselves to these lists.
    /// </summary>
    public class CounterVO
    {
        public DateTime InitialTime;
        public TimeSpan Duration;

        public DateTime EndTime => InitialTime + Duration;

        /// <summary>
        /// True when nobody asked for time remaining, so this entry is only measuring how long
        /// it has been running. Such an entry finishes on its duration if it has one, and
        /// otherwise runs for as long as the game does.
        /// </summary>
        public bool IsElapsedOnly => TickCallbacks.Count == 0 && TickPercentageCallbacks.Count == 0;

        /// <summary>True once nothing is left that would notice another tick.</summary>
        public bool HasNoListeners =>
            TickCallbacks.Count == 0 &&
            TickPercentageCallbacks.Count == 0 &&
            TickElapsedTimeCallbacks.Count == 0 &&
            CompleteCallbacks.Count == 0;

        public readonly List<Action<bool>> CheckActiveCallbacks = new();
        public readonly List<Action<float>> TickCallbacks = new();
        public readonly List<Action<float>> TickPercentageCallbacks = new();
        public readonly List<Action<float>> TickElapsedTimeCallbacks = new();
        public readonly List<Action> CompleteCallbacks = new();
        public readonly List<Action> StopCallbacks = new();

        /// <summary>
        /// Drops every callback once the counter is over. The lists are emptied rather than
        /// thrown away, so a caller that arrives late finds an entry it can read instead of a
        /// null reference.
        /// </summary>
        public void Clear()
        {
            CheckActiveCallbacks.Clear();
            TickCallbacks.Clear();
            TickPercentageCallbacks.Clear();
            TickElapsedTimeCallbacks.Clear();
            CompleteCallbacks.Clear();
            StopCallbacks.Clear();
        }
    }
}
