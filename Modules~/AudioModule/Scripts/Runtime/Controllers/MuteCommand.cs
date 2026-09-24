using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.AudioModule.Models;
using UnityEngine;

namespace Modules.AudioModule.Controllers
{
    /// <summary>Holds every sound where it is. Not stored: a Mute lasts until its Unmute, never past a restart.</summary>
    internal class MuteCommand : Command
    {
        [Inject] private IAudioSettingsModel _settings { get; set; }

        public override void Execute()
        {
            _settings.AddMute();
            AudioListener.pause = true;
        }
    }
}
