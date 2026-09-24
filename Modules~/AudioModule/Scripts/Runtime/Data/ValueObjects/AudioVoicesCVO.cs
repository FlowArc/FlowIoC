using System;
using Modules.AudioModule.Enums;
using Modules.AudioModule.Shared.Enums;
using UnityEngine;

namespace Modules.AudioModule.Data.ValueObjects
{
    /// <summary>How many sounds one channel plays at once, and what happens when it is full.</summary>
    [Serializable]
    public class AudioVoicesCVO
    {
        public AudioChannel Channel = AudioChannel.Sfx;

        [Min(1), Tooltip("Voices made when the game starts.")]
        public int Initial = 4;

        [Min(1), Tooltip("The most sounds this channel plays at once. More voices are made up to this count.")]
        public int Max = 12;

        public AudioSteal Steal = AudioSteal.Oldest;
    }
}
