#if UNITY_EDITOR

using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.ViewsMediators.Mediator;
using Modules.AudioModule.AudioTestModule.Data.ValueObjects;
using Modules.AudioModule.AudioTestModule.Signals;
using UnityEngine;

namespace Modules.AudioModule.AudioTestModule.ViewsMediators
{
    public class AudioTestMediator : IMediator
    {
        [Inject] private AudioTestView _view { get; set; }

        [InjectSignal] private AudioTestInternalSignals _signals { get; set; }

        public void OnRegister()
        {
            _view.OnBeep += Beep;
            _view.OnBoom += Boom;
            _view.OnThemeA += ThemeA;
            _view.OnThemeB += ThemeB;
            _view.OnStopMusic += StopMusic;
            _view.OnDuck += Duck;
            _view.OnNormal += Normal;
            _view.OnUnload += Unload;
            _view.OnMusicOn += MusicOn;
            _view.OnSfxOn += SfxOn;
            _view.OnMuted += Muted;
            _view.OnMusicVolume += MusicVolume;
            _view.OnSfxVolume += SfxVolume;
            _signals.StateChanged.AddListener(OnStateChanged);
        }

        public void OnRemove()
        {
            _view.OnBeep -= Beep;
            _view.OnBoom -= Boom;
            _view.OnThemeA -= ThemeA;
            _view.OnThemeB -= ThemeB;
            _view.OnStopMusic -= StopMusic;
            _view.OnDuck -= Duck;
            _view.OnNormal -= Normal;
            _view.OnUnload -= Unload;
            _view.OnMusicOn -= MusicOn;
            _view.OnSfxOn -= SfxOn;
            _view.OnMuted -= Muted;
            _view.OnMusicVolume -= MusicVolume;
            _view.OnSfxVolume -= SfxVolume;
            _signals.StateChanged.RemoveListener(OnStateChanged);
        }

        private void Beep() => _signals.Beep.Dispatch();
        private void Boom(float x) => _signals.Boom.Dispatch(new Vector3(x, 0f, 0f));
        private void ThemeA() => _signals.ThemeA.Dispatch();
        private void ThemeB() => _signals.ThemeB.Dispatch();
        private void StopMusic() => _signals.StopMusic.Dispatch();
        private void Duck() => _signals.Duck.Dispatch();
        private void Normal() => _signals.Normal.Dispatch();
        private void Unload() => _signals.Unload.Dispatch();
        private void MusicOn(bool on) => _signals.MusicEnabled.Dispatch(on);
        private void SfxOn(bool on) => _signals.SfxEnabled.Dispatch(on);
        private void Muted(bool on) => _signals.Muted.Dispatch(on);
        private void MusicVolume(float value) => _signals.MusicVolume.Dispatch(value);
        private void SfxVolume(float value) => _signals.SfxVolume.Dispatch(value);

        private void OnStateChanged(AudioTestStateVO state) =>
            _view.Show(state.MusicOn, state.MusicVolume, state.SfxOn, state.SfxVolume,
                $"Music {(state.MusicOn ? "on" : "off")} {Mathf.RoundToInt(state.MusicVolume * 100f)}%  -  "
                + $"Sound {(state.SfxOn ? "on" : "off")} {Mathf.RoundToInt(state.SfxVolume * 100f)}%  -  "
                + $"bank {(state.BankLoaded ? "loaded" : "unloaded")}");
    }
}

#endif
