using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using Modules.AudioModule.Data.UnityObjects;
using Modules.AudioModule.Models;
using Modules.AudioModule.Shared;
using UnityEngine.Audio;

namespace Modules.AudioModule.Controllers
{
    internal class ApplySnapshotCommand : Command
    {
        [SignalParam] private AudioSnapshot _snapshot { get; set; }

        [Inject] private IAudioSettingsModel _settings { get; set; }

        public override void Execute()
        {
            CD_AudioSettings settings = _settings.Settings;

            if (settings.Mixer == null)
                return;

            AudioMixerSnapshot snapshot = settings.Mixer.FindSnapshot(_snapshot.Name);

            if (snapshot == null)
            {
                FlowLogger.LogWarning($"ApplySnapshot - the mixer {settings.Mixer.name} has no snapshot called {_snapshot}.");
                return;
            }

            snapshot.TransitionTo(settings.SnapshotTransition);
        }
    }
}
