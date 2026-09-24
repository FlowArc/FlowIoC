using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.AudioModule.Models;
using UnityEngine;

namespace Modules.AudioModule.Controllers
{
    /// <summary>Takes one Mute back; the sound goes on only once no Mute is left waiting.</summary>
    internal class UnmuteCommand : Command
    {
        [Inject] private IAudioSettingsModel _settings { get; set; }

        public override void Execute()
        {
            _settings.RemoveMute();
            AudioListener.pause = _settings.MuteCount > 0;
        }
    }
}
