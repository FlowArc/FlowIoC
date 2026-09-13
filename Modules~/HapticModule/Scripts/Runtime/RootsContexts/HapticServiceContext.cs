using FlowIoC.BaseModule.Contexts;
using Modules.HapticModule.Controllers;
using Modules.HapticModule.Models;
using Modules.HapticModule.Services;
using Modules.HapticModule.Signals;

namespace Modules.HapticModule.RootsContexts
{
    public class HapticServiceContext : Context
    {
        private HapticInternalSignals _internalSignals;

        public override void SignalBindings()
        {
            base.SignalBindings();

            _internalSignals = InjectionBinder.Bind<HapticInternalSignals>();
        }

        public override void InjectionBindings()
        {
            base.InjectionBindings();

            InjectionBinder.Bind<IHapticModel, HapticModel>();

            // One platform, one player. The commands never learn which; the Editor gets the one
            // that logs, so a flow can be read in the Flow Console without a device.
#if UNITY_IOS && !UNITY_EDITOR
            InjectionBinder.Bind<IHapticPlayer, IosHapticPlayer>();
#elif UNITY_ANDROID && !UNITY_EDITOR
            InjectionBinder.Bind<IHapticPlayer, AndroidHapticPlayer>();
#else
            InjectionBinder.Bind<IHapticPlayer, SilentHapticPlayer>();
#endif

            // The one type other modules reference directly, which is what makes this a Service.
            InjectionBinderCrossContext.Bind<IHapticService, HapticService>();
        }

        public override void CommandBindings()
        {
            base.CommandBindings();

            CommandBinder.Bind(_internalSignals.Initialize)
                .ToSequence<InitializeHapticCommand>();

            CommandBinder.Bind(_internalSignals.Play)
                .ToSequence<PlayHapticCommand>();

            CommandBinder.Bind(_internalSignals.SetEnabled)
                .ToSequence<SetHapticsEnabledCommand>();
        }

        public override void Setup()
        {
            base.Setup();

            // The module brings itself up: the stored choice into the model, the platform readied.
            _internalSignals.Initialize.Dispatch();
        }
    }
}