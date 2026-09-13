#if UNITY_EDITOR
using UnityEngine;

namespace FlowIoC.Editor.Console
{
    /// <summary>
    /// The colour a module's channel gets when nobody has chosen one: picked from twelve tones by
    /// the channel's name, so every module is coloured from the day it is created and the same
    /// module is the same colour on every machine and after every regeneration. A module that
    /// wants a colour of its own says so in its card, and the generator takes that instead.
    ///
    /// Twelve hues thirty degrees apart at the same saturation and lightness, so they read on the
    /// console's dark rows and no two neighbours are hard to tell apart. Hashed with FNV-1a rather
    /// than string.GetHashCode, which the runtime is free to change between processes.
    /// </summary>
    internal class FlowChannelPalette
    {
        private readonly Color32[] _tones =
        {
            new Color32(228, 103, 103, 255), // 0    red
            new Color32(228, 166, 103, 255), // 30   orange
            new Color32(228, 228, 103, 255), // 60   yellow
            new Color32(166, 228, 103, 255), // 90   lime
            new Color32(103, 228, 103, 255), // 120  green
            new Color32(103, 228, 166, 255), // 150  mint
            new Color32(103, 228, 228, 255), // 180  cyan
            new Color32(103, 166, 228, 255), // 210  sky
            new Color32(103, 103, 228, 255), // 240  blue
            new Color32(166, 103, 228, 255), // 270  violet
            new Color32(228, 103, 228, 255), // 300  magenta
            new Color32(228, 103, 166, 255) // 330  pink
        };

        internal int Count => _tones.Length;

        internal Color32 ToneAt(int index) => _tones[index];

        internal Color32 Pick(string channelName) => _tones[IndexOf(channelName)];

        /// <summary>
        /// Which of the twelve a name lands on. Case-insensitive, because a channel is found
        /// case-insensitively everywhere else and two spellings of one module are one module.
        /// </summary>
        internal int IndexOf(string channelName)
        {
            const uint offset = 2166136261;
            const uint prime = 16777619;

            uint hash = offset;

            foreach (char character in (channelName ?? string.Empty).ToLowerInvariant())
            {
                hash ^= character;
                hash *= prime;
            }

            return (int) (hash % (uint) _tones.Length);
        }
    }
}
#endif
