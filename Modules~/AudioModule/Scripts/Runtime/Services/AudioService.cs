using System;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.AudioModule.Enums;
using Modules.AudioModule.Models;
using Modules.AudioModule.Shared;
using Modules.AudioModule.Shared.Enums;
using Modules.AudioModule.Signals;
using UnityEngine;

namespace Modules.AudioModule.Services
{
    /// <summary>
    /// The surface hands every call to a command through a signal, so each one is a step the Flow
    /// Console shows and every decision is taken in a Command. A setting goes through the public
    /// Incoming - the same signal a Connector carries a settings screen's toggle to - so a call
    /// and a toggle take one road.
    /// </summary>
    public class AudioService : IAudioService
    {
        [Inject] private IAudioSettingsModel _settings { get; set; }
        [Inject] private IAudioBankModel _banks { get; set; }
        [InjectSignal] private AudioInternalSignals _internalSignals { get; set; }
        [InjectSignal] private AudioSignals _signals { get; set; }

        public void Play(AudioKey key) => _internalSignals.Play.Dispatch(key);

        public void PlayAt(AudioKey key, Vector3 position) => _internalSignals.PlayAt.Dispatch(key, position);

        public void PlayMusic(AudioKey key) => _internalSignals.PlayMusic.Dispatch(key);

        public void StopMusic() => _internalSignals.StopMusic.Dispatch();

        public void LoadBank(string module, Action<bool> done = null) => _internalSignals.LoadBank.Dispatch(module, done ?? (_ => { }));

        public void UnloadBank(string module) => _internalSignals.UnloadBank.Dispatch(module);

        public bool IsBankLoaded(string module) => _banks.StateOf(module) == AudioBankState.Loaded;

        public void ApplySnapshot(AudioSnapshot snapshot) => _internalSignals.ApplySnapshot.Dispatch(snapshot);

        public bool IsEnabled(AudioBus bus) => _settings.IsEnabled(bus);

        public float GetVolume(AudioBus bus) => _settings.VolumeOf(bus);

        public void SetEnabled(AudioBus bus, bool on) =>
            (bus == AudioBus.Music ? _signals.Incoming.SetMusicEnabled : _signals.Incoming.SetSfxEnabled).Dispatch(on);

        public void SetVolume(AudioBus bus, float volume) =>
            (bus == AudioBus.Music ? _signals.Incoming.SetMusicVolume : _signals.Incoming.SetSfxVolume).Dispatch(volume);

        public void Mute() => _signals.Incoming.Mute.Dispatch();

        public void Unmute() => _signals.Incoming.Unmute.Dispatch();
    }
}
