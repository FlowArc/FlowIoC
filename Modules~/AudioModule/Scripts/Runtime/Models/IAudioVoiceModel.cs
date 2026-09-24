using System.Collections.Generic;
using Modules.AudioModule.Entities;
using Modules.AudioModule.Shared.Enums;

namespace Modules.AudioModule.Models
{
    internal interface IAudioVoiceModel
    {
        void Add(AudioVoice voice);

        IReadOnlyList<AudioVoice> VoicesOf(AudioChannel channel);

        IEnumerable<AudioVoice> All { get; }

        /// <summary>A voice of the channel that is playing nothing, or null.</summary>
        AudioVoice FindIdle(AudioChannel channel);

        /// <summary>The channel's voice that started first, or null for a channel with none.</summary>
        AudioVoice FindOldest(AudioChannel channel);

        /// <summary>The two music voices a crossfade moves between.</summary>
        void SetMusicVoices(AudioVoice first, AudioVoice second);

        /// <summary>The music voice playing now, or null while no music plays.</summary>
        AudioVoice CurrentMusic { get; }

        /// <summary>The music voice the next track starts on.</summary>
        AudioVoice NextMusic { get; }

        /// <summary>Makes the next voice current - the crossfade handed over.</summary>
        void SwapMusic();

        void ClearMusic();
    }
}
