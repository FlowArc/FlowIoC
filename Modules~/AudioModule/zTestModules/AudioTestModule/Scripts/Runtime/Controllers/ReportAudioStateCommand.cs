#if UNITY_EDITOR

using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.AudioModule.AudioTestModule.Data.ValueObjects;
using Modules.AudioModule.AudioTestModule.RootsContexts;
using Modules.AudioModule.AudioTestModule.Signals;
using Modules.AudioModule.Services;
using Modules.AudioModule.Shared.Enums;

namespace Modules.AudioModule.AudioTestModule.Controllers
{
    internal class ReportAudioStateCommand : Command
    {
        [Inject] private IAudioService _audio { get; set; }
        [InjectSignal] private AudioTestInternalSignals _signals { get; set; }

        public override void Execute() =>
            _signals.StateChanged.Dispatch(new AudioTestStateVO(
                _audio.IsEnabled(AudioBus.Music), _audio.GetVolume(AudioBus.Music),
                _audio.IsEnabled(AudioBus.Sfx), _audio.GetVolume(AudioBus.Sfx),
                _audio.IsBankLoaded(AudioTestContext.BANK)));
    }
}

#endif
