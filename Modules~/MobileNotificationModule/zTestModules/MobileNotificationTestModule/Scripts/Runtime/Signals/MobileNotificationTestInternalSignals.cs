#if UNITY_EDITOR

using FlowIoC.BaseModule.Signals;

namespace Modules.MobileNotificationModule.MobileNotificationTestModule.Signals
{
    /// <summary>The test scene's buttons in, and the state line out.</summary>
    public class MobileNotificationTestInternalSignals : ISignalHolder
    {
        public Signal RequestPermission = new();
        public Signal<string> ScheduleTest = new();
        public Signal<string> CancelTest = new();
        public Signal CancelAll = new();
        public Signal OpenSettings = new();
        public Signal Leave = new();
        public Signal Return = new();
        public Signal ReportState = new();
        public Signal<string> StateChanged = new();
    }
}

#endif
