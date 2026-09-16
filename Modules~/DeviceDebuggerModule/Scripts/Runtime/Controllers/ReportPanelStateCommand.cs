using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using Modules.DeviceDebuggerModule.Data.ValueObjects;
using Modules.DeviceDebuggerModule.Models;
using Modules.DeviceDebuggerModule.Signals;

namespace Modules.DeviceDebuggerModule.Controllers
{
    /// <summary>
    /// The last step of every sequence that changed what the panel shows, and the answer to the
    /// mediator's request when its view registers: the model's state on one signal.
    /// </summary>
    internal class ReportPanelStateCommand : Command
    {
        [Inject] private IDeviceDebuggerModel _model { get; set; }
        [InjectSignal] private DeviceDebuggerInternalSignals _signals { get; set; }

        public override void Execute()
        {
            _signals.PanelStateChanged.Dispatch(new PanelStateVO
            {
                Config = _model.Config,
                Logs = _model.Logs,
                Options = _model.Options,
                Signals = _model.Signals,
                Info = _model.Info,
                Channels = FlowLogger.Channels.All,
                ActiveTab = _model.ActiveTab,
                IsOpen = _model.IsOpen,
                OptionsVersion = _model.OptionsVersion
            });
        }
    }
}
