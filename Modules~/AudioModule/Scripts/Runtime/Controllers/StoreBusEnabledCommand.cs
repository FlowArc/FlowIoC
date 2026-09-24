using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Function.Provider;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.AudioModule.Constants;
using Modules.AudioModule.Models;
using Modules.AudioModule.Shared.Enums;
using UnityEngine;

namespace Modules.AudioModule.Controllers
{
    /// <summary>
    /// The bus is fixed where the step is bound; the choice comes off the signal. Music turned off
    /// keeps playing unheard, so turning it on again finds it where it would have been.
    /// </summary>
    internal class StoreBusEnabledCommand : Command<AudioBus>
    {
        [SignalParam] private bool _on { get; set; }

        [Inject] private IAudioSettingsModel _settings { get; set; }
        [Inject] private IFunctionProvider _functions { get; set; }

        public override void Execute(AudioBus bus)
        {
            _settings.SetEnabled(bus, _on);
            PlayerPrefs.SetInt(bus == AudioBus.Music ? AudioConstants.PREFS_MUSIC_ENABLED : AudioConstants.PREFS_SFX_ENABLED, _on ? 1 : 0);
            _functions.Call<ApplyBusLevelsFunction>().Execute();
        }
    }
}
