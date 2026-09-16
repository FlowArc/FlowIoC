using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using Modules.DeviceDebuggerModule.Constants;
using Modules.DeviceDebuggerModule.Models;

namespace Modules.DeviceDebuggerModule.Controllers
{
    /// <summary>
    /// Hooks the model's ring onto FlowLogger. Runs from the service's PostConstruct, during the
    /// module's own binding pass, so the rows of every Root bound after this one are kept. The
    /// hook is the Model's method itself - no signal in between, because the framework logs every
    /// dispatch and a signal per row would record itself for ever.
    /// </summary>
    internal class StartLogCaptureCommand : Command
    {
        [Inject] private IDeviceDebuggerModel _model { get; set; }

        public override void Execute()
        {
            if (!DeviceDebuggerConstants.IS_AVAILABLE || _model.IsCapturing) return;

            FlowLogger.OnLogRecorded += _model.AddLog;
            _model.SetCapturing(true);
        }
    }
}
