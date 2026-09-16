using FlowIoC.BaseModule.Signals;
using FlowIoC.ConsoleModule;
using Modules.DeviceDebuggerModule.Data.ValueObjects;
using Modules.DeviceDebuggerModule.Enums;

namespace Modules.DeviceDebuggerModule.Signals
{
    /// <summary>
    /// What the service and the mediator say to the module's own commands, and what those
    /// commands announce back to the mediator. Nothing here leaves the module: the module has no
    /// public holder at all, because a Service answers the caller it was given.
    ///
    /// There is no Incoming and no Outgoing here. Those two halves say what a module accepts and
    /// what it announces across a boundary, and an internal signal never crosses one.
    /// </summary>
    internal class DeviceDebuggerInternalSignals : ISignalHolder
    {
        public Signal StartCapture = new();
        public Signal StopCapture = new();
        public Signal Initialize = new();

        /// <summary>DebugTab.Last opens whatever was open, or the Console when errors wait.</summary>
        public Signal<DebugTab> Show = new();

        public Signal Hide = new();
        public Signal Toggle = new();

        /// <summary>An option row acted on; the text is the control's value, empty for a button.</summary>
        public Signal<DebugOptionVO, string> ActivateOption = new();

        /// <summary>A Signals-tab row fired; the text is the payload typed beside it.</summary>
        public Signal<DebugSignalVO, string> FireSignal = new();

        public Signal ClearLogs = new();

        /// <summary>Every row in the ring - the button says "Copy all logs".</summary>
        public Signal CopyLogs = new();

        /// <summary>The one row open in the detail.</summary>
        public Signal<ConsoleLog> CopyLog = new();

        public Signal CopyInfo = new();

        /// <summary>The mediator asks when its view registers; the answer is PanelStateChanged.</summary>
        public Signal RequestPanelState = new();

        /// <summary>Announced by the commands, heard by the mediator: the model's state to paint from.</summary>
        public Signal<PanelStateVO> PanelStateChanged = new();
    }
}
