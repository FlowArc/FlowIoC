using FlowIoC.BaseModule.Contexts;
using Modules.CameraModule.Controllers;
using Modules.CameraModule.Models.Runtime;
using Modules.CameraModule.Shared.Signals;
using Modules.CameraModule.ViewsMediators;

namespace Modules.CameraModule.RootsContexts
{
    public class CameraSystemContext : Context
    {
        private CameraSignals _cameraSignals;

        public override void SignalBindings()
        {
            base.SignalBindings();
            _cameraSignals = InjectionBinderCrossContext.Bind<CameraSignals>();
        }

        public override void InjectionBindings()
        {
            base.InjectionBindings();
            InjectionBinder.Bind<ICameraModel, CameraModel>();
        }

        public override void MediationBindings()
        {
            base.MediationBindings();
            MediationBinder.Bind<CameraAdapterView>().To<CameraAdapterMediator>();
            MediationBinder.Bind<SingleCameraAdapterView>().To<SingleCameraAdapterMediator>();
        }

        public override void CommandBindings()
        {
            base.CommandBindings();
            CommandBinder.Bind(_cameraSignals.Incoming.RegisterCamera).ToSequence<RegisterCameraCommand>();
            CommandBinder.Bind(_cameraSignals.Incoming.UnregisterCamera).ToSequence<UnregisterCameraCommand>();
            CommandBinder.Bind(_cameraSignals.Incoming.SwitchCamera).ToSequence<SwitchCameraCommand>();
            CommandBinder.Bind(_cameraSignals.Incoming.SetCameraTarget).ToSequence<SetCameraTargetCommand>();
            CommandBinder.Bind(_cameraSignals.Incoming.SetCameraLastPos).ToSequence<SetCameraLastPosCommand>();
        }

        public override void Setup()
        {
            base.Setup();
        }

        public override void Launch()
        {
            base.Launch();
        }
    }
}
