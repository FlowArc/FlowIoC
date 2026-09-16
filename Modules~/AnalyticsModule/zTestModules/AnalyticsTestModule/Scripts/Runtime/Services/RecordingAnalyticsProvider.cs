#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using FlowIoC.ConsoleModule;
using Modules.AnalyticsModule.Data.ValueObjects;
using Modules.AnalyticsModule.Services;

namespace Modules.AnalyticsModule.AnalyticsTestModule.Services
{
    /// <summary>
    /// The Editor's provider, and the sample of a game's own: records every call, logs each on
    /// this module's channel, and holds its Initialize callback until a button answers - so the
    /// queue and the flush are watched in the Flow Console.
    /// </summary>
    public class RecordingAnalyticsProvider : IAnalyticsProvider
    {
        private Action<bool> _pendingReady;

        public readonly List<AnalyticsEventVO> Events = new();
        public readonly List<string> Calls = new();

        public string Name => "Recording";

        public bool IsHoldingTheAnswer => _pendingReady != null;

        public void Initialize(Action<bool> ready)
        {
            _pendingReady = ready;
            FlowLogger.Log("Initialize - holding the answer until Provider ready or Provider failed is pressed.");
        }

        /// <summary>Answers the held callback; nothing waiting is a plain log.</summary>
        public void Answer(bool ready)
        {
            Action<bool> pending = _pendingReady;
            _pendingReady = null;

            if (pending == null)
            {
                FlowLogger.Log($"Answer - nothing is waiting for {ready}.");
                return;
            }

            pending(ready);
        }

        public void Log(AnalyticsEventVO analyticsEvent)
        {
            Events.Add(analyticsEvent);
            Record("Log " + analyticsEvent);
        }

        public void SetUserProperty(string name, string value) => Record($"SetUserProperty {name}={value}");

        public void SetUserId(string userId) => Record("SetUserId " + userId);

        public void SetConsent(AnalyticsConsentVO consent) => Record("SetConsent " + consent);

        private void Record(string call)
        {
            Calls.Add(call);
            FlowLogger.Log("Recording - " + call);
        }
    }
}

#endif
