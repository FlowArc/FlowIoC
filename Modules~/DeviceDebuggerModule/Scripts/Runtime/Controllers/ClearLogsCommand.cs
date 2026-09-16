using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.DeviceDebuggerModule.Models;

namespace Modules.DeviceDebuggerModule.Controllers
{
    internal class ClearLogsCommand : Command
    {
        [Inject] private IDeviceDebuggerModel _model { get; set; }

        public override void Execute() => _model.Logs.Clear();
    }
}
