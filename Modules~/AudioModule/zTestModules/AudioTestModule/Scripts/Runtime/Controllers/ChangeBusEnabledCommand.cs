#if UNITY_EDITOR

using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.AudioModule.Services;
using Modules.AudioModule.Shared.Enums;

namespace Modules.AudioModule.AudioTestModule.Controllers
{
    /// <summary>What a settings screen's toggle does through a Connector, done here through the Service.</summary>
    internal class ChangeBusEnabledCommand : Command<AudioBus>
    {
        [SignalParam] private bool _on { get; set; }

        [Inject] private IAudioService _audio { get; set; }

        public override void Execute(AudioBus bus) => _audio.SetEnabled(bus, _on);
    }
}

#endif
