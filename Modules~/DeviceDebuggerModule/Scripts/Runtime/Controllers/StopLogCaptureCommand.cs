using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using Modules.DeviceDebuggerModule.Models;

namespace Modules.DeviceDebuggerModule.Controllers
{
    /// <summary>
    /// Takes the hook back when the context goes. FlowLogger is static and outlives a play
    /// session that reloads no domain, so a hook left behind would feed a model nobody reads.
    /// </summary>
    internal class StopLogCaptureCommand : Command
    {
        [Inject] private IDeviceDebuggerModel _model { get; set; }

        public override void Execute()
        {
            if (!_model.IsCapturing) return;

            FlowLogger.OnLogRecorded -= _model.AddLog;
            _model.SetCapturing(false);
        }
    }
}
