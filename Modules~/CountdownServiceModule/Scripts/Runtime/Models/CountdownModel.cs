using System;
using System.Collections.Generic;
using FlowIoC.BaseModule.Attributes;
using Modules.CountdownServiceModule.Data.ValueObjects;

namespace Modules.CountdownServiceModule.Models
{
    public class CountdownModel : ICountdownModel
    {
        public DateTime Time { get; set; }

        public DateTime StartTime { get; private set; }

        public float StartRealtime { get; private set; }

        public bool IsActive { get; private set; }

        [ShowInModelViewer] public Dictionary<string, CountdownVO> DataMap { get; } = new();

        public object LockObject { get; } = new object();

        /// <summary>
        /// The module becomes active once a time source has answered. Both readings are taken at
        /// the same moment on purpose: every later <see cref="Time"/> is <see cref="StartTime"/>
        /// plus the realtime elapsed since <see cref="StartRealtime"/>, so what the module counts
        /// stays tied to the clock it started from rather than to how often it happens to tick.
        /// </summary>
        public void Activate(DateTime startTime, float startRealtime)
        {
            StartTime = startTime;
            StartRealtime = startRealtime;
            Time = startTime;
            IsActive = true;
        }
    }
}
