using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Function.Provider;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using Modules.AudioModule.Constants;
using Modules.AudioModule.Models;
using Modules.AudioModule.Shared.Enums;
using UnityEngine;

namespace Modules.AudioModule.Controllers
{
    /// <summary>The player's stored choices into the model, and onto the mixer. Nothing stored means on and full.</summary>
    internal class ApplyStoredSettingsCommand : Command
    {
        [Inject] private IAudioSettingsModel _settings { get; set; }
        [Inject] private IFunctionProvider _functions { get; set; }

        public override void Execute()
        {
            _settings.SetEnabled(AudioBus.Music, PlayerPrefs.GetInt(AudioConstants.PREFS_MUSIC_ENABLED, 1) == 1);
            _settings.SetVolume(AudioBus.Music, PlayerPrefs.GetFloat(AudioConstants.PREFS_MUSIC_VOLUME, 1f));
            _settings.SetEnabled(AudioBus.Sfx, PlayerPrefs.GetInt(AudioConstants.PREFS_SFX_ENABLED, 1) == 1);
            _settings.SetVolume(AudioBus.Sfx, PlayerPrefs.GetFloat(AudioConstants.PREFS_SFX_VOLUME, 1f));

            if (_settings.Settings.Mixer == null)
                FlowLogger.LogWarning("ApplyStoredSettings - CD_AudioSettings has no mixer, so the music and sound levels and the snapshots do nothing. File the module's CD_AudioSettings on AudioServiceRoot, or give yours a mixer.");

            _functions.Call<ApplyBusLevelsFunction>().Execute();
        }
    }
}
