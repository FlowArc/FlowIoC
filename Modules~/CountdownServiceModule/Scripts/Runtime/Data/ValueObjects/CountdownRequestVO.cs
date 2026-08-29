using System;

namespace Modules.CountdownServiceModule.Data.ValueObjects
{
    /// <summary>
    /// One request into the countdown service: which countdown, how long it runs, and the
    /// callbacks the caller wants to hear about it on. Starting a countdown and subscribing to
    /// one already running both travel in this shape, so the commands behind them read one type.
    /// </summary>
    public class CountdownRequestVO
    {
        public string Id;
        public int Duration;
        public DateTime StartTime;

        /// <summary>Whether <see cref="CountdownTick"/> wants 0..1 rather than seconds left.</summary>
        public bool IsPercentageTick;

        public Action<bool> CheckActive;
        public Action<float> CountdownTick;
        public Action<float> ElapsedTimeTick;
        public Action CountdownComplete;
        public Action CountdownStop;
    }
}
