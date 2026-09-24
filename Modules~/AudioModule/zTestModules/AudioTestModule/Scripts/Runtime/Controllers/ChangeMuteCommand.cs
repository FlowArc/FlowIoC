#if UNITY_EDITOR

using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.AudioModule.Services;

namespace Modules.AudioModule.AudioTestModule.Controllers
{
    /// <summary>Stands in for an ad opening and closing: the toggle on is a Mute, off is its Unmute.</summary>
    internal class ChangeMuteCommand : Command
    {
        [SignalParam] private bool _muted { get; set; }

        [Inject] private IAudioService _audio { get; set; }

        public override void Execute()
        {
            if (_muted)
                _audio.Mute();
            else
                _audio.Unmute();
        }
    }
}

#endif
