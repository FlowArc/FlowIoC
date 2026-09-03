using System;

namespace Modules.CounterModule.Data.ValueObjects
{
    /// <summary>
    /// One request into the counter service: which counter, how long it runs, and the
    /// callbacks the caller wants to hear about it on. Starting a counter and subscribing to
    /// one already running both travel in this shape, so the commands behind them read one type.
    /// </summary>
    public class CounterRequestVO
    {
        public string Id;
        public int Duration;
        public DateTime StartTime;

        /// <summary>Whether <see cref="CounterTick"/> wants 0..1 rather than seconds left.</summary>
        public bool IsPercentageTick;

        public Action<bool> CheckActive;
        public Action<float> CounterTick;
        public Action<float> ElapsedTimeTick;
        public Action CounterComplete;
        public Action CounterStop;
    }
}
