using System.Collections.Generic;
using Modules.AnalyticsModule.Enums;
using Modules.AnalyticsModule.Services;

namespace Modules.AnalyticsModule.Data.ValueObjects
{
    /// <summary>
    /// One plugged provider: where it is, and the events that arrived before it was ready. The
    /// queue is capped so an SDK that never answers cannot grow it for ever; past the cap the
    /// oldest event goes, and the count of what went is reported once at the flush.
    /// </summary>
    public class ProviderSlotVO
    {
        public const int PENDING_CAP = 200;

        private readonly Queue<AnalyticsEventVO> _pending = new();

        public IAnalyticsProvider Provider { get; }

        public AnalyticsProviderState State { get; set; }

        /// <summary>How many the cap pushed out since the last flush.</summary>
        public int Dropped { get; private set; }

        public int PendingCount => _pending.Count;

        public ProviderSlotVO(IAnalyticsProvider provider)
        {
            Provider = provider;
            State = AnalyticsProviderState.Plugged;
        }

        /// <summary>Queues the event; true when the cap pushed the oldest one out to make room.</summary>
        public bool Enqueue(AnalyticsEventVO analyticsEvent)
        {
            bool dropped = false;

            if (_pending.Count >= PENDING_CAP)
            {
                _pending.Dequeue();
                Dropped++;
                dropped = true;
            }

            _pending.Enqueue(analyticsEvent);
            return dropped;
        }

        public bool TryDequeue(out AnalyticsEventVO analyticsEvent)
        {
            if (_pending.Count == 0)
            {
                analyticsEvent = null;
                return false;
            }

            analyticsEvent = _pending.Dequeue();
            return true;
        }

        /// <summary>Empties the queue and says how many it held.</summary>
        public int ClearPending()
        {
            int count = _pending.Count;
            _pending.Clear();
            return count;
        }

        /// <summary>Reads the drop count and resets it, so it is reported once.</summary>
        public int TakeDropped()
        {
            int dropped = Dropped;
            Dropped = 0;
            return dropped;
        }
    }
}
