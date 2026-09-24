#if UNITY_EDITOR

using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.AudioModule.Services;
using Modules.AudioModule.Shared.Enums;

namespace Modules.AudioModule.AudioTestModule.Controllers
{
    /// <summary>What a settings screen's slider does through a Connector, done here through the Service.</summary>
    internal class ChangeBusVolumeCommand : Command<AudioBus>
    {
        [SignalParam] private float _volume { get; set; }

        [Inject] private IAudioService _audio { get; set; }

        public override void Execute(AudioBus bus) => _audio.SetVolume(bus, _volume);
    }
}

#endif
