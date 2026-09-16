#if UNITY_EDITOR

using System;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.AnalyticsModule.AnalyticsTestModule.Services;
using Modules.AnalyticsModule.Data.ValueObjects;
using Modules.AnalyticsModule.Services;

namespace Modules.AnalyticsModule.AnalyticsTestModule.Controllers
{
    /// <summary>An event built from state, the way a game Command logs one: three parameters of three kinds.</summary>
    public class LogTestEventCommand : Command
    {
        [Inject] private IAnalyticsService _analytics { get; set; }
        [Inject] private RecordingAnalyticsProvider _provider { get; set; }

        public override void Execute() =>
            _analytics.Log(new AnalyticsEventVO("test_event")
                .With("count", _provider.Events.Count + 1)
                .With("at", DateTime.Now.ToString("HH:mm:ss"))
                .With("flag", true));
    }
}

#endif
