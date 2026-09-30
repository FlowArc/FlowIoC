using System;
using System.Collections.Generic;
using Modules.AudioModule.Shared.Data.ValueObjects;
using UnityEngine;

namespace Modules.AudioModule.Data.ValueObjects
{
    /// <summary>
    /// One sound the banks list, held whole by AudioBankModel: its entry in the bank, the clips a
    /// loaded bank gave it, and the two memories a play needs. It holds a config side and a runtime
    /// side together, which is why it is a plain VO.
    /// </summary>
    internal class AudioSoundVO
    {
        public static readonly IReadOnlyList<AudioClip> NO_CLIPS = Array.Empty<AudioClip>();

        /// <summary>The entry of the bank that listed the key first.</summary>
        public AudioClipCVO Config;

        /// <summary>Empty until a bank listing the key is loaded, and again once it is unloaded.</summary>
        public IReadOnlyList<AudioClip> Clips = NO_CLIPS;

        /// <summary>When the sound last played; kept across an unload so the interval still holds.</summary>
        public float LastPlayed;

        public bool HasPlayed;

        /// <summary>The variant the sound played last, or -1; kept across an unload.</summary>
        public int LastVariant = -1;
    }
}
