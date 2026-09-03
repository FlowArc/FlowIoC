using System;
using System.Collections.Generic;
using Modules.CounterModule.Data.ValueObjects;

namespace Modules.CounterModule.Models
{
    public interface ICounterModel
    {
        /// <summary>The time the module works from, advanced once a second by the tick.</summary>
        DateTime Time { get; set; }

        /// <summary>The time the module became active at.</summary>
        DateTime StartTime { get; }

        /// <summary>The realtime reading taken at that same moment.</summary>
        float StartRealtime { get; }

        /// <summary>True once a time source has answered and the counters are running.</summary>
        bool IsActive { get; }

        /// <summary>Every counter currently running, by id.</summary>
        Dictionary<string, CounterVO> DataMap { get; }

        /// <summary>Held while the map is read or written, so a callback cannot change it midway.</summary>
        object LockObject { get; }

        void Activate(DateTime startTime, float startRealtime);
    }
}
