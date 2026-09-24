using FlowIoC.BaseModule.Adapters;
using FlowIoC.BaseModule.Constructables;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using Modules.AudioModule.Constants;
using Modules.AudioModule.Data.UnityObjects;
using Modules.AudioModule.RootsContexts;
using Modules.AudioModule.Shared.Enums;
using UnityEngine;

namespace Modules.AudioModule.Models
{
    /// <summary>
    /// PostConstruct only takes CD_AudioSettings off the Root's adapter. The player's choices start
    /// at on and full; ApplyStoredSettingsCommand reads the stored ones in.
    /// </summary>
    internal class AudioSettingsModel : IAudioSettingsModel, IConstructable
    {
        [Inject(nameof(AudioServiceContext))] private GameObject _root { get; set; }

        private readonly bool[] _enabled = {true, true};
        private readonly float[] _volume = {1f, 1f};

        public bool IsPostConstructed { get; set; }

        public bool IsDeconstructed { get; set; }

        public CD_AudioSettings Settings { get; private set; }

        public Transform Root => _root != null ? _root.transform : null;

        public int MuteCount { get; private set; }

        public void PostConstruct()
        {
            RootAdapter adapter = _root != null ? _root.GetComponent<RootAdapter>() : null;

            if (adapter == null)
                FlowLogger.LogError("AudioServiceRoot has no RootAdapter, so CD_AudioSettings cannot be read; the defaults hold and no mixer is used.", _root);

            Load(adapter != null ? adapter.GetScriptable<CD_AudioSettings>() : null);
        }

        /// <summary>The settings to hold; null takes the defaults. Internal so a test loads without a Root.</summary>
        internal void Load(CD_AudioSettings settings)
        {
            Settings = settings != null ? settings : ScriptableObject.CreateInstance<CD_AudioSettings>();
        }

        public bool IsEnabled(AudioBus bus) => _enabled[(int) bus];

        public float VolumeOf(AudioBus bus) => _volume[(int) bus];

        public void SetEnabled(AudioBus bus, bool on) => _enabled[(int) bus] = on;

        public void SetVolume(AudioBus bus, float volume) => _volume[(int) bus] = Mathf.Clamp01(volume);

        public float DecibelsOf(AudioBus bus)
        {
            float volume = _volume[(int) bus];

            if (!_enabled[(int) bus] || volume <= 0.0001f)
                return AudioConstants.SILENT_DECIBELS;

            return Mathf.Max(AudioConstants.SILENT_DECIBELS, 20f * Mathf.Log10(volume));
        }

        public void AddMute() => MuteCount++;

        public void RemoveMute() => MuteCount = Mathf.Max(0, MuteCount - 1);
    }
}
