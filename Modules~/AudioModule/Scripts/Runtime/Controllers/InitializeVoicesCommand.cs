using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Function.Provider;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.AudioModule.Data.ValueObjects;
using Modules.AudioModule.Entities;
using Modules.AudioModule.Models;
using Modules.AudioModule.Shared.Enums;
using UnityEngine;

namespace Modules.AudioModule.Controllers
{
    /// <summary>Makes each channel's first voices, and the two music voices a crossfade moves between.</summary>
    internal class InitializeVoicesCommand : Command
    {
        [Inject] private IAudioSettingsModel _settings { get; set; }
        [Inject] private IAudioVoiceModel _voices { get; set; }
        [Inject] private IFunctionProvider _functions { get; set; }

        public override void Execute()
        {
            foreach (AudioVoicesCVO voices in _settings.Settings.Voices)
            {
                if (voices == null || voices.Channel == AudioChannel.Music)
                    continue;

                int count = Mathf.Min(voices.Initial, voices.Max) - _voices.VoicesOf(voices.Channel).Count;

                for (int i = 0; i < count; i++)
                    Create(voices.Channel);
            }

            _voices.SetMusicVoices(Create(AudioChannel.Music), Create(AudioChannel.Music));
        }

        private AudioVoice Create(AudioChannel channel) =>
            _functions.Call<CreateVoiceFunction>().AddParams(channel).ExecuteAndGetResult<AudioVoice>();
    }
}
