using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.DeviceDebuggerModule.Enums;
using Modules.DeviceDebuggerModule.Models;
using Modules.DeviceDebuggerModule.Signals;

namespace Modules.DeviceDebuggerModule.Controllers
{
    /// <summary>The trigger's tap: closed opens on the last tab, open closes.</summary>
    internal class TogglePanelCommand : Command
    {
        [Inject] private IDeviceDebuggerModel _model { get; set; }
        [InjectSignal] private DeviceDebuggerInternalSignals _signals { get; set; }

        public override void Execute()
        {
            if (_model.IsOpen) _signals.Hide.Dispatch();
            else _signals.Show.Dispatch(DebugTab.Last);
        }
    }
}
