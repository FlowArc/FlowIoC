using System;
using System.Collections.Generic;
using Modules.AudioModule.Shared.Enums;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Modules.AudioModule.Shared.Data.ValueObjects
{
    /// <summary>
    /// One sound in a bank: the key it answers to, the clips it picks from, and how it plays.
    /// How loud, where and whether it is 3D are said here once, not at every place that plays it.
    /// </summary>
    [Serializable]
    public class AudioClipCVO
    {
        [AudioKeyId]
        public string Key;

        [Tooltip("One clip, or several variants: a play picks one at random and never the same one twice in a row.")]
        public List<AssetReferenceT<AudioClip>> Clips = new();

        [Range(0f, 1f)]
        public float Volume = 1f;

        public AudioChannel Channel = AudioChannel.Sfx;

        public bool Loop;

        [Range(0f, 1f), Tooltip("0 plays flat wherever it is played; 1 plays from the point PlayAt names.")]
        public float SpatialBlend;

        [Min(0f), Tooltip("Within this distance a 3D sound plays at full volume.")]
        public float MinDistance = 1f;

        [Min(0f), Tooltip("Beyond this distance a 3D sound is no longer heard.")]
        public float MaxDistance = 30f;

        [Min(0f), Tooltip("Seconds that must pass before this sound plays again. A play inside the gap is dropped.")]
        public float MinInterval;
    }
}
