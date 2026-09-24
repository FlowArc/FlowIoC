using System.Collections.Generic;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using Modules.AudioModule.Entities;
using Modules.AudioModule.Enums;
using Modules.AudioModule.Models;
using Modules.AudioModule.Shared;
using Modules.AudioModule.Shared.Data.ValueObjects;
using UnityEngine;

namespace Modules.AudioModule.Controllers
{
    /// <summary>
    /// Hands the music over to a track: the new one fades in on the idle music voice while the
    /// one playing fades out. The track already playing is left alone, so a screen that asks for
    /// its music every time it opens does not restart it.
    /// </summary>
    internal class PlayMusicCommand : Command
    {
        [SignalParam] private AudioKey _key { get; set; }

        [Inject] private IAudioSettingsModel _settings { get; set; }
        [Inject] private IAudioBankModel _banks { get; set; }
        [Inject] private IAudioVoiceModel _voices { get; set; }

        public override void Execute()
        {
            if (!_banks.TryGetSound(_key, out AudioClipCVO sound))
            {
                FlowLogger.LogWarning($"PlayMusic - no bank has a sound for {_key}. Add a row for it to the bank of the module that declares it.");
                return;
            }

            if (_banks.StateOf(_key.Bank) != AudioBankState.Loaded)
            {
                FlowLogger.LogWarning($"PlayMusic - {_key} was asked for before the {_key.Bank} bank was loaded. Bind IAudioService.Commands.LoadBank before this step.");
                return;
            }

            IReadOnlyList<AudioClip> clips = _banks.ClipsOf(_key);

            if (clips.Count == 0)
            {
                FlowLogger.LogWarning($"PlayMusic - {_key} has no clip that loaded. Give its row a clip in the {_key.Bank} bank.");
                return;
            }

            AudioVoice current = _voices.CurrentMusic;

            if (current != null && current.Key == _key && current.IsBusy)
                return;

            float crossfade = _settings.Settings.MusicCrossfade;
            AudioVoice next = _voices.NextMusic;

            next.Play(_key, clips[_banks.PickVariant(_key, clips.Count)], sound, _settings.Root.position, 0f);
            next.FadeTo(sound.Volume, crossfade, false);
            current?.FadeTo(0f, crossfade, true);

            _voices.SwapMusic();
            _banks.MarkPlayed(_key, Time.unscaledTime);
        }
    }
}
