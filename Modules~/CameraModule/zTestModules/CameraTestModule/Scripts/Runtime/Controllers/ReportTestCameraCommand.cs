#if UNITY_EDITOR

using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.CameraModule.CameraTestModule.Models;
using Modules.CameraModule.CameraTestModule.Signals;
using Modules.CameraModule.Shared.Enums;

namespace Modules.CameraModule.CameraTestModule.Controllers
{
    /// <summary>Tells the label which camera is live.</summary>
    internal class ReportTestCameraCommand : Command
    {
        [Inject] private ICameraTestModel _model { get; set; }
        [InjectSignal] private CameraTestInternalSignals _signals { get; set; }

        public override void Execute()
        {
            string cube = _model.Live == CameraName.Menu ? "cube A" : "cube B";
            _signals.StateChanged.Dispatch($"Live: {_model.Live} - on {cube}");
        }
    }
}

#endif
