using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.DeviceDebuggerModule.Models;

namespace Modules.DeviceDebuggerModule.Controllers
{
    internal class HidePanelCommand : Command
    {
        [Inject] private IDeviceDebuggerModel _model { get; set; }

        public override void Execute()
        {
            if (!_model.IsOpen) return;

            _model.SetOpen(false);
        }
    }
}
