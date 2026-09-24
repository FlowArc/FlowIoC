using System;
using System.Collections.Generic;
using Modules.AudioModule.Entities;
using Modules.AudioModule.Shared.Enums;

namespace Modules.AudioModule.Models
{
    /// <summary>The voices each channel has, and which of the two music voices is playing.</summary>
    internal class AudioVoiceModel : IAudioVoiceModel
    {
        private readonly Dictionary<AudioChannel, List<AudioVoice>> _voices = new();
        private readonly AudioVoice[] _music = new AudioVoice[2];
        private int _currentMusic = -1;

        public IEnumerable<AudioVoice> All
        {
            get
            {
                foreach (List<AudioVoice> voices in _voices.Values)
                foreach (AudioVoice voice in voices)
                    yield return voice;

                foreach (AudioVoice voice in _music)
                {
                    if (voice != null)
                        yield return voice;
                }
            }
        }

        public void Add(AudioVoice voice)
        {
            if (!_voices.TryGetValue(voice.Channel, out List<AudioVoice> voices))
                _voices[voice.Channel] = voices = new List<AudioVoice>();

            voices.Add(voice);
        }

        public IReadOnlyList<AudioVoice> VoicesOf(AudioChannel channel) =>
            _voices.TryGetValue(channel, out List<AudioVoice> voices) ? voices : (IReadOnlyList<AudioVoice>) Array.Empty<AudioVoice>();

        public AudioVoice FindIdle(AudioChannel channel)
        {
            foreach (AudioVoice voice in VoicesOf(channel))
            {
                if (!voice.IsBusy)
                    return voice;
            }

            return null;
        }

        public AudioVoice FindOldest(AudioChannel channel)
        {
            AudioVoice oldest = null;

            foreach (AudioVoice voice in VoicesOf(channel))
            {
                if (oldest == null || voice.StartedAt < oldest.StartedAt)
                    oldest = voice;
            }

            return oldest;
        }

        public void SetMusicVoices(AudioVoice first, AudioVoice second)
        {
            _music[0] = first;
            _music[1] = second;
            _currentMusic = -1;
        }

        public AudioVoice CurrentMusic => _currentMusic >= 0 ? _music[_currentMusic] : null;

        public AudioVoice NextMusic => _music[_currentMusic == 0 ? 1 : 0];

        public void SwapMusic() => _currentMusic = _currentMusic == 0 ? 1 : 0;

        public void ClearMusic() => _currentMusic = -1;
    }
}
