using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.AudioModule.Models;

namespace Modules.AudioModule.Controllers
{
    internal class StopMusicCommand : Command
    {
        [Inject] private IAudioSettingsModel _settings { get; set; }
        [Inject] private IAudioVoiceModel _voices { get; set; }

        public override void Execute()
        {
            _voices.CurrentMusic?.FadeTo(0f, _settings.Settings.MusicCrossfade, true);
            _voices.ClearMusic();
        }
    }
}
