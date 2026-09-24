using Modules.AudioModule.Controllers;
using Modules.AudioModule.Models;
using Modules.AudioModule.Services;
using Modules.AudioModule.Shared.Enums;
using Modules.AudioModule.Signals;
using Modules.AudioModule.ViewsMediators;
using FlowIoC.BaseModule.Contexts;

namespace Modules.AudioModule.RootsContexts
{
    public class AudioServiceContext : Context
    {
        private AudioSignals _signals;

        private AudioInternalSignals _internalSignals;

        public override void SignalBindings()
        {
            base.SignalBindings();

            _internalSignals = InjectionBinder.Bind<AudioInternalSignals>();
            _signals = InjectionBinderCrossContext.Bind<AudioSignals>();
        }

        public override void InjectionBindings()
        {
            base.InjectionBindings();

            InjectionBinder.Bind<IAudioSettingsModel, AudioSettingsModel>();
            InjectionBinder.Bind<IAudioBankModel, AudioBankModel>();
            InjectionBinder.Bind<IAudioVoiceModel, AudioVoiceModel>();

            // The one type other modules reference directly, which is what makes this a Service.
            InjectionBinderCrossContext.Bind<IAudioService, AudioService>();
        }

        public override void MediationBindings()
        {
            base.MediationBindings();

            MediationBinder.Bind<ButtonClickSoundView>().To<ButtonClickSoundMediator>();
        }

        public override void CommandBindings()
        {
            base.CommandBindings();

            CommandBinder.Bind(_internalSignals.Initialize)
                .ToSequence<InitializeVoicesCommand>()
                .ToSequence<FindBanksCommand>()
                .ToSequence<ApplyStoredSettingsCommand>()
                .ToSequence<PreloadBanksCommand>();

            CommandBinder.Bind(_internalSignals.Play).ToSequence<PlaySoundCommand>();
            CommandBinder.Bind(_internalSignals.PlayAt).ToSequence<PlaySoundAtCommand>();
            CommandBinder.Bind(_internalSignals.PlayMusic).ToSequence<PlayMusicCommand>();
            CommandBinder.Bind(_internalSignals.StopMusic).ToSequence<StopMusicCommand>();
            CommandBinder.Bind(_internalSignals.LoadBank).ToSequence<LoadBankCommand>();
            CommandBinder.Bind(_internalSignals.UnloadBank).ToSequence<UnloadBankCommand>();
            CommandBinder.Bind(_internalSignals.ApplySnapshot).ToSequence<ApplySnapshotCommand>();

            CommandBinder.Bind(_signals.Incoming.SetMusicEnabled).ToSequence<StoreBusEnabledCommand>(AudioBus.Music);
            CommandBinder.Bind(_signals.Incoming.SetSfxEnabled).ToSequence<StoreBusEnabledCommand>(AudioBus.Sfx);
            CommandBinder.Bind(_signals.Incoming.SetMusicVolume).ToSequence<StoreBusVolumeCommand>(AudioBus.Music);
            CommandBinder.Bind(_signals.Incoming.SetSfxVolume).ToSequence<StoreBusVolumeCommand>(AudioBus.Sfx);
            CommandBinder.Bind(_signals.Incoming.Mute).ToSequence<MuteCommand>();
            CommandBinder.Bind(_signals.Incoming.Unmute).ToSequence<UnmuteCommand>();
        }

        public override void Setup()
        {
            base.Setup();

            // The module brings itself up a frame after binding, where a mixer takes a level:
            // the voices, the banks the project has, the stored choices, then the preloads.
            _internalSignals.Initialize.Dispatch();
        }
    }
}
