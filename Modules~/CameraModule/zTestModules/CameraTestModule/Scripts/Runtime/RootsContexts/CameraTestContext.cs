#if UNITY_EDITOR
using FlowIoC.BaseModule.Contexts;
using Modules.CameraModule.CameraTestModule.Controllers;
using Modules.CameraModule.CameraTestModule.Models;
using Modules.CameraModule.CameraTestModule.Signals;
using Modules.CameraModule.CameraTestModule.ViewsMediators;
using Modules.CameraModule.Services;
using Modules.CameraModule.Shared.Enums;

namespace Modules.CameraModule.CameraTestModule.RootsContexts
{
    /// <summary>
    /// Two cubes, two cameras: Menu on cube A, which the shipped Root brings, and Gameplay on cube
    /// B, added beside it. Launch remembers where each camera's target starts, so a move wanders
    /// around its own cube; Switch goes from one camera to the other, Move glides the live one,
    /// Zoom eases its distance.
    /// </summary>
    public class CameraTestContext : Context
    {
        private CameraTestInternalSignals _signals;

        public override void SignalBindings()
        {
            base.SignalBindings();
            _signals = InjectionBinder.Bind<CameraTestInternalSignals>();
        }

        public override void InjectionBindings()
        {
            base.InjectionBindings();
            InjectionBinder.Bind<ICameraTestModel, CameraTestModel>();
        }

        public override void MediationBindings()
        {
            base.MediationBindings();
            MediationBinder.Bind<CameraTestView>().To<CameraTestMediator>();
        }

        public override void CommandBindings()
        {
            base.CommandBindings();

            CommandBinder.Bind(_signals.Prepare)
                .ToSequence<ICameraService.Commands.Switch>(CameraName.Gameplay)
                .ToSequence<ICameraService.Commands.RememberPosition>(CameraName.Gameplay)
                .ToSequence<ICameraService.Commands.Switch>(CameraName.Menu)
                .ToSequence<ICameraService.Commands.RememberPosition>(CameraName.Menu)
                .ToSequence<ReportTestCameraCommand>();

            CommandBinder.Bind(_signals.SwitchRequested)
                .ToSequence<SwitchTestCameraCommand>()
                .ToSequence<ReportTestCameraCommand>();

            CommandBinder.Bind(_signals.MoveRequested).ToSequence<MoveTestCameraCommand>();
            CommandBinder.Bind(_signals.ZoomRequested).ToSequence<ZoomTestCameraCommand>();
        }

        public override void Launch()
        {
            base.Launch();
            _signals.Prepare.Dispatch();
        }
    }
}
#endif
