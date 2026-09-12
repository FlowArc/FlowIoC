using FlowIoC.BaseModule.Signals;
using Modules.LoadingModule.Data.ValueObjects;

namespace Modules.LoadingModule.Signals
{
    /// <summary>
    /// The module talking to its own Commands. StepReported and SetTouched fire on every progress
    /// tick of a download, so their command log is off; the lifecycle lines the Commands write are
    /// what the console shows instead.
    ///
    /// There is no Incoming and no Outgoing here. Those two halves say what a module accepts and
    /// what it announces across a boundary, and an internal signal never crosses one.
    /// </summary>
    internal class LoadingInternalSignals : ISignalHolder
    {
        public Signal<string> BeginSet = new();
        public Signal<LoadingStepReportVO> StepReported = new(hideCommandLog: true);
        public Signal<string> SetTouched = new(hideCommandLog: true);
        public Signal<string> WatchSet = new();
    }
}