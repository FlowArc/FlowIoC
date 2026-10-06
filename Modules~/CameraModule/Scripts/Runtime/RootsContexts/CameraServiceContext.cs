using FlowIoC.BaseModule.Contexts;
using Modules.CameraModule.Controllers;
using Modules.CameraModule.Models.Runtime;
using Modules.CameraModule.Services;
using Modules.CameraModule.Signals;
using Modules.CameraModule.ViewsMediators;

namespace Modules.CameraModule.RootsContexts
{
    public class CameraServiceContext : Context
    {
        private CameraInternalSignals _signals;

        public override void SignalBindings()
        {
            base.SignalBindings();
            _signals = InjectionBinder.Bind<CameraInternalSignals>();
        }

        public override void InjectionBindings()
        {
            base.InjectionBindings();
            InjectionBinder.Bind<ICameraModel, CameraModel>();
            InjectionBinderCrossContext.Bind<ICameraService, CameraService>();
        }

        public override void MediationBindings()
        {
            base.MediationBindings();
            MediationBinder.Bind<CameraAdapterView>().To<CameraAdapterMediator>();
        }

        public override void CommandBindings()
        {
            base.CommandBindings();
            CommandBinder.Bind(_signals.RegisterCamera).ToSequence<RegisterCameraCommand>();
            CommandBinder.Bind(_signals.UnregisterCamera).ToSequence<UnregisterCameraCommand>();
            CommandBinder.Bind(_signals.SwitchCamera).ToSequence<SwitchCameraCommand>();
            CommandBinder.Bind(_signals.SetCameraTarget).ToSequence<SetCameraTargetCommand>();
            CommandBinder.Bind(_signals.RememberPosition).ToSequence<SetCameraLastPosCommand>();
            CommandBinder.Bind(_signals.MoveCamera).ToSequence<MoveCameraCommand>();
            CommandBinder.Bind(_signals.SetDistance).ToSequence<SetCameraDistanceCommand>();
        }
    }
}
