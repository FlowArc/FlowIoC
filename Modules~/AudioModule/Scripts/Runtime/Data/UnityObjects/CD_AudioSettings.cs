using System.Collections.Generic;
using Modules.AudioModule.Data.ValueObjects;
using Modules.AudioModule.Shared.Enums;
using UnityEngine;
using UnityEngine.Audio;

namespace Modules.AudioModule.Data.UnityObjects
{
    /// <summary>
    /// How the module plays: the mixer, a group per channel, the voices each channel has, and
    /// the fade times. Filed in the Scriptables of AudioServiceRoot. The module ships one wired to
    /// Mixer_Audio; a game that wants its own files it on the Root in its scene instead of editing
    /// this one, so an update of the module never meets the game's settings.
    /// </summary>
    [CreateAssetMenu(fileName = "CD_AudioSettings", menuName = "FlowIoC/AudioModule/Data/CD_AudioSettings")]
    public class CD_AudioSettings : ScriptableObject
    {
        public AudioMixer Mixer;

        public AudioMixerGroup MusicGroup;
        public AudioMixerGroup SfxGroup;
        public AudioMixerGroup UiGroup;
        public AudioMixerGroup AmbientGroup;

        [Tooltip("The mixer's exposed parameter for the music level, in decibels.")]
        public string MusicVolumeParameter = "MusicVolume";

        [Tooltip("The mixer's exposed parameter for the sound effects level, in decibels.")]
        public string SfxVolumeParameter = "SfxVolume";

        public List<AudioVoicesCVO> Voices = new()
        {
            new AudioVoicesCVO {Channel = AudioChannel.Sfx, Initial = 6, Max = 16},
            new AudioVoicesCVO {Channel = AudioChannel.Ui, Initial = 2, Max = 4},
            new AudioVoicesCVO {Channel = AudioChannel.Ambient, Initial = 1, Max = 4}
        };

        [Min(0f), Tooltip("Seconds one music track takes to hand over to the next.")]
        public float MusicCrossfade = 1f;

        [Min(0f), Tooltip("Seconds the mixer takes to move to a snapshot.")]
        public float SnapshotTransition = 0.5f;

        /// <summary>The voices a channel has; a channel the list leaves out gets the defaults.</summary>
        public AudioVoicesCVO VoicesFor(AudioChannel channel)
        {
            foreach (AudioVoicesCVO voices in Voices)
            {
                if (voices != null && voices.Channel == channel)
                    return voices;
            }

            return new AudioVoicesCVO {Channel = channel};
        }

        public AudioMixerGroup GroupOf(AudioChannel channel)
        {
            switch (channel)
            {
                case AudioChannel.Music: return MusicGroup;
                case AudioChannel.Ui: return UiGroup != null ? UiGroup : SfxGroup;
                case AudioChannel.Ambient: return AmbientGroup != null ? AmbientGroup : SfxGroup;
                default: return SfxGroup;
            }
        }
    }
}
