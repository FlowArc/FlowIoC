using FlowIoC.BaseModule.Function.ReturnableFunctions;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.Provider.Coroutine;
using Modules.AudioModule.Entities;
using Modules.AudioModule.Models;
using Modules.AudioModule.Shared.Enums;
using UnityEngine;

namespace Modules.AudioModule.Controllers
{
    /// <summary>
    /// Makes one voice under AudioServiceRoot, routed to the channel's mixer group. A music voice
    /// is handed back to the caller; any other joins its channel's voices.
    /// </summary>
    internal class CreateVoiceFunction : FunctionReturn<AudioVoice, AudioChannel>
    {
        [Inject] private IAudioSettingsModel _settings { get; set; }
        [Inject] private IAudioVoiceModel _voices { get; set; }
        [Inject] private ICoroutineProvider _coroutines { get; set; }

        public override AudioVoice Execute(AudioChannel channel)
        {
            var gameObject = new GameObject($"Voice {channel} {_settings.Root.childCount}");
            gameObject.transform.SetParent(_settings.Root, false);

            var source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.outputAudioMixerGroup = _settings.Settings.GroupOf(channel);

            var voice = new AudioVoice(source, channel, _coroutines);

            if (channel != AudioChannel.Music)
                _voices.Add(voice);

            return voice;
        }
    }
}
