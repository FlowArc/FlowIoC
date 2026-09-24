using Modules.AudioModule.Data.UnityObjects;
using Modules.AudioModule.Shared.Enums;
using UnityEngine;

namespace Modules.AudioModule.Models
{
    internal interface IAudioSettingsModel
    {
        /// <summary>The settings filed on the Root, or the defaults when none is.</summary>
        CD_AudioSettings Settings { get; }

        /// <summary>AudioServiceRoot's transform, which every voice hangs under.</summary>
        Transform Root { get; }

        bool IsEnabled(AudioBus bus);

        float VolumeOf(AudioBus bus);

        void SetEnabled(AudioBus bus, bool on);

        /// <summary>Stores the level, held between 0 and 1.</summary>
        void SetVolume(AudioBus bus, float volume);

        /// <summary>The level to give the mixer: silence while the bus is off or at zero.</summary>
        float DecibelsOf(AudioBus bus);

        /// <summary>How many Mutes are waiting for their Unmute.</summary>
        int MuteCount { get; }

        void AddMute();

        /// <summary>Takes one Mute back; an Unmute with none waiting changes nothing.</summary>
        void RemoveMute();
    }
}
