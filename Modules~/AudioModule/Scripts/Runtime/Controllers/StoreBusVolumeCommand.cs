using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Function.Provider;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.AudioModule.Constants;
using Modules.AudioModule.Models;
using Modules.AudioModule.Shared.Enums;
using UnityEngine;

namespace Modules.AudioModule.Controllers
{
    /// <summary>The bus is fixed where the step is bound; the level, 0 to 1, comes off the signal.</summary>
    internal class StoreBusVolumeCommand : Command<AudioBus>
    {
        [SignalParam] private float _volume { get; set; }

        [Inject] private IAudioSettingsModel _settings { get; set; }
        [Inject] private IFunctionProvider _functions { get; set; }

        public override void Execute(AudioBus bus)
        {
            _settings.SetVolume(bus, _volume);
            PlayerPrefs.SetFloat(bus == AudioBus.Music ? AudioConstants.PREFS_MUSIC_VOLUME : AudioConstants.PREFS_SFX_VOLUME, _settings.VolumeOf(bus));
            _functions.Call<ApplyBusLevelsFunction>().Execute();
        }
    }
}
