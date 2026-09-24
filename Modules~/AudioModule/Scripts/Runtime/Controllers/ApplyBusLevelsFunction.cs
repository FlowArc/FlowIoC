using FlowIoC.BaseModule.Function.VoidFunctions;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using Modules.AudioModule.Data.UnityObjects;
using Modules.AudioModule.Models;
using Modules.AudioModule.Shared.Enums;

namespace Modules.AudioModule.Controllers
{
    /// <summary>Writes both buses' levels onto the mixer's exposed volumes, in decibels.</summary>
    internal class ApplyBusLevelsFunction : FunctionVoid
    {
        [Inject] private IAudioSettingsModel _settings { get; set; }

        public override void Execute()
        {
            CD_AudioSettings settings = _settings.Settings;

            if (settings.Mixer == null)
                return;

            Apply(settings, settings.MusicVolumeParameter, AudioBus.Music);
            Apply(settings, settings.SfxVolumeParameter, AudioBus.Sfx);
        }

        private void Apply(CD_AudioSettings settings, string parameter, AudioBus bus)
        {
            if (!settings.Mixer.SetFloat(parameter, _settings.DecibelsOf(bus)))
                FlowLogger.LogWarning($"ApplyBusLevels - the mixer {settings.Mixer.name} exposes no parameter called {parameter}, so the {bus} level does nothing. Expose the group's volume under that name, or change the name on CD_AudioSettings.");
        }
    }
}
