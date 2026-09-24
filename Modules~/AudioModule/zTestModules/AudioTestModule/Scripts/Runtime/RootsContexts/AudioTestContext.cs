#if UNITY_EDITOR
using FlowIoC.BaseModule.Contexts;
using Modules.AudioModule.AudioTestModule.Controllers;
using Modules.AudioModule.AudioTestModule.Signals;
using Modules.AudioModule.AudioTestModule.ViewsMediators;
using Modules.AudioModule.Services;
using Modules.AudioModule.Shared;
using Modules.AudioModule.Shared.Enums;

namespace Modules.AudioModule.AudioTestModule.RootsContexts
{
    /// <summary>
    /// Every flow here is what a game binds for the same thing, so the scene is also the sample:
    /// the bank loaded before the music that needs it, a sound named where the step is bound, a
    /// point taken off the signal for a 3D sound, a snapshot for a popup.
    /// </summary>
    public class AudioTestContext : Context
    {
        /// <summary>
        /// The bank this module's sounds are in. A test module has no FlowModule constant, so the
        /// name is written out; a game module passes FlowModule.GameplayModule instead.
        /// </summary>
        public const string BANK = "AudioTestModule";

        private AudioTestInternalSignals _signals;

        public override void SignalBindings()
        {
            base.SignalBindings();

            _signals = InjectionBinder.Bind<AudioTestInternalSignals>();
        }

        public override void MediationBindings()
        {
            base.MediationBindings();

            MediationBinder.Bind<AudioTestView>().To<AudioTestMediator>();
        }

        public override void CommandBindings()
        {
            base.CommandBindings();

            CommandBinder.Bind(_signals.Start)
                .ToSequence<IAudioService.Commands.LoadBank>(BANK)
                .ToSequence<IAudioService.Commands.PlayMusic>(AudioKey.AudioTest.ThemeA)
                .ToSequence<ReportAudioStateCommand>();

            CommandBinder.Bind(_signals.Beep).ToSequence<IAudioService.Commands.Play>(AudioKey.AudioTest.Beep);
            CommandBinder.Bind(_signals.Boom).ToSequence<IAudioService.Commands.PlayAt>(AudioKey.AudioTest.Boom);

            CommandBinder.Bind(_signals.ThemeA)
                .ToSequence<IAudioService.Commands.LoadBank>(BANK)
                .ToSequence<IAudioService.Commands.PlayMusic>(AudioKey.AudioTest.ThemeA)
                .ToSequence<ReportAudioStateCommand>();

            CommandBinder.Bind(_signals.ThemeB)
                .ToSequence<IAudioService.Commands.LoadBank>(BANK)
                .ToSequence<IAudioService.Commands.PlayMusic>(AudioKey.AudioTest.ThemeB)
                .ToSequence<ReportAudioStateCommand>();

            CommandBinder.Bind(_signals.StopMusic).ToSequence<IAudioService.Commands.StopMusic>();
            CommandBinder.Bind(_signals.Duck).ToSequence<IAudioService.Commands.ApplySnapshot>(AudioSnapshot.Ducked);
            CommandBinder.Bind(_signals.Normal).ToSequence<IAudioService.Commands.ApplySnapshot>(AudioSnapshot.Default);

            CommandBinder.Bind(_signals.Unload)
                .ToSequence<IAudioService.Commands.UnloadBank>(BANK)
                .ToSequence<ReportAudioStateCommand>();

            CommandBinder.Bind(_signals.MusicEnabled)
                .ToSequence<ChangeBusEnabledCommand>(AudioBus.Music)
                .ToSequence<ReportAudioStateCommand>();

            CommandBinder.Bind(_signals.SfxEnabled)
                .ToSequence<ChangeBusEnabledCommand>(AudioBus.Sfx)
                .ToSequence<ReportAudioStateCommand>();

            CommandBinder.Bind(_signals.MusicVolume).ToSequence<ChangeBusVolumeCommand>(AudioBus.Music);
            CommandBinder.Bind(_signals.SfxVolume).ToSequence<ChangeBusVolumeCommand>(AudioBus.Sfx);
            CommandBinder.Bind(_signals.Muted).ToSequence<ChangeMuteCommand>();
            CommandBinder.Bind(_signals.ReportState).ToSequence<ReportAudioStateCommand>();
        }

        public override void Launch()
        {
            base.Launch();

            _signals.Start.Dispatch();
        }
    }
}
#endif
