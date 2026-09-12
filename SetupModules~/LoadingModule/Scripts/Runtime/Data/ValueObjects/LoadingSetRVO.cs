using System.Collections.Generic;
using System.Threading.Tasks;
using Modules.LoadingModule.Shared.Enums;

namespace Modules.LoadingModule.Data.ValueObjects
{
    /// <summary>One set's runtime state. Built once from the config; Begin resets the steps.</summary>
    public class LoadingSetRVO
    {
        public LoadingSetCVO Config;
        public LoadingSetState State;
        public float BeganAt;
        public float LastReportAt;
        public float EndedAt;
        public string FailedStep;
        public LoadingStepRVO LastTouchedStep;

        /// <summary>Publish announced this run's Began signal already.</summary>
        public bool BeginAnnounced;

        /// <summary>The stall warning for the current silence went out; the next report clears it.</summary>
        public bool StallWarned;

        /// <summary>A WatchStallCommand coroutine is alive for this set.</summary>
        public bool WatcherRunning;

        public TaskCompletionSource<bool> Awaiter;

        public readonly List<LoadingStepRVO> Steps = new();

        public string Key => Config.Key;
    }
}
