#if UNITY_EDITOR

using FlowIoC.BaseModule.Signals;

namespace Modules.AnalyticsModule.AnalyticsTestModule.Signals
{
    /// <summary>The test scene's buttons in, and the state text out.</summary>
    public class AnalyticsTestInternalSignals : ISignalHolder
    {
        public Signal LogTestEvent = new();
        public Signal LogStep = new();
        public Signal SetProperty = new();
        public Signal SetId = new();
        public Signal ConsentGranted = new();
        public Signal ConsentDenied = new();
        public Signal<bool> AnswerProvider = new();
        public Signal ReportState = new();
        public Signal<string> StateChanged = new();
    }
}

#endif
