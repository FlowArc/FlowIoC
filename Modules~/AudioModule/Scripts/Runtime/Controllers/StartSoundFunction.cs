using System.Collections.Generic;
using FlowIoC.BaseModule.Function.Provider;
using FlowIoC.BaseModule.Function.ReturnableFunctions;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using Modules.AudioModule.Data.ValueObjects;
using Modules.AudioModule.Entities;
using Modules.AudioModule.Enums;
using Modules.AudioModule.Models;
using Modules.AudioModule.Shared;
using Modules.AudioModule.Shared.Data.ValueObjects;
using Modules.AudioModule.Shared.Enums;
using UnityEngine;

namespace Modules.AudioModule.Controllers
{
    /// <summary>
    /// Every decision one sound effect involves, shared by Play and PlayAt: the key known and its
    /// bank loaded, the sound off or still inside its interval, a variant, and a voice - idle,
    /// newly made under the channel's limit, or taken from the oldest when the channel is full.
    /// True when the sound started.
    /// </summary>
    internal class StartSoundFunction : FunctionReturn<bool, AudioKey, bool, Vector3>
    {
        [Inject] private IAudioSettingsModel _settings { get; set; }
        [Inject] private IAudioBankModel _banks { get; set; }
        [Inject] private IAudioVoiceModel _voices { get; set; }
        [Inject] private IFunctionProvider _functions { get; set; }

        public override bool Execute(AudioKey key, bool atPosition, Vector3 position)
        {
            if (!_banks.TryGetSound(key, out AudioClipCVO sound))
            {
                FlowLogger.LogWarning($"Play - no bank has a sound for {key}. Add a row for it to the bank of the module that declares it.");
                return false;
            }

            if (sound.Channel == AudioChannel.Music)
            {
                FlowLogger.LogWarning($"Play - {key} is on the Music channel. Music plays through IAudioService.Commands.PlayMusic.");
                return false;
            }

            if (!_settings.IsEnabled(AudioBus.Sfx))
                return false;

            if (_banks.StateOf(key.Bank) != AudioBankState.Loaded)
            {
                FlowLogger.LogWarning($"Play - {key} was asked for before the {key.Bank} bank was loaded. Tick Preload At Boot on the bank, or bind IAudioService.Commands.LoadBank before this step.");
                return false;
            }

            IReadOnlyList<AudioClip> clips = _banks.ClipsOf(key);

            if (clips.Count == 0)
            {
                FlowLogger.LogWarning($"Play - {key} has no clip that loaded. Give its row a clip in the {key.Bank} bank.");
                return false;
            }

            float now = Time.unscaledTime;

            if (_banks.IsCoolingDown(key, now, sound.MinInterval))
                return false;

            AudioVoice voice = TakeVoice(sound.Channel);

            if (voice == null)
            {
                FlowLogger.Log($"Play - {key} dropped: every {sound.Channel} voice is playing and the channel rejects new sounds.");
                return false;
            }

            AudioClip clip = clips[_banks.PickVariant(key, clips.Count)];
            voice.Play(key, clip, sound, atPosition ? position : _settings.Root.position, sound.Volume);
            _banks.MarkPlayed(key, now);
            return true;
        }

        private AudioVoice TakeVoice(AudioChannel channel)
        {
            AudioVoice idle = _voices.FindIdle(channel);

            if (idle != null)
                return idle;

            AudioVoicesCVO limits = _settings.Settings.VoicesFor(channel);

            if (_voices.VoicesOf(channel).Count < limits.Max)
                return _functions.Call<CreateVoiceFunction>().AddParams(channel).ExecuteAndGetResult<AudioVoice>();

            if (limits.Steal == AudioSteal.Reject)
                return null;

            AudioVoice oldest = _voices.FindOldest(channel);
            oldest?.Stop();
            return oldest;
        }
    }
}
