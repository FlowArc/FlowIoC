#if UNITY_EDITOR

using System;
using FlowIoC.BaseModule.Injectable.Components;
using FlowIoC.BaseModule.ViewsMediators.View;
using UnityEngine;
using UnityEngine.UI;

namespace Modules.AudioModule.AudioTestModule.ViewsMediators
{
    /// <summary>Scene references and raw input: a button per flow, the two settings as a toggle and a slider each, a mute toggle, a label.</summary>
    [RequireComponent(typeof(ViewInjector))]
    public class AudioTestView : MonoBehaviour, IView
    {
        public bool IsRegistered { get; set; }

        [SerializeField] private Button _beep;
        [SerializeField] private Button _boomLeft;
        [SerializeField] private Button _boomRight;
        [SerializeField] private Button _themeA;
        [SerializeField] private Button _themeB;
        [SerializeField] private Button _stopMusic;
        [SerializeField] private Button _duck;
        [SerializeField] private Button _normal;
        [SerializeField] private Button _unload;
        [SerializeField] private Toggle _musicOn;
        [SerializeField] private Toggle _sfxOn;
        [SerializeField] private Toggle _muted;
        [SerializeField] private Slider _musicVolume;
        [SerializeField] private Slider _sfxVolume;
        [SerializeField] private Text _status;

        public Action OnBeep;
        public Action<float> OnBoom;
        public Action OnThemeA;
        public Action OnThemeB;
        public Action OnStopMusic;
        public Action OnDuck;
        public Action OnNormal;
        public Action OnUnload;
        public Action<bool> OnMusicOn;
        public Action<bool> OnSfxOn;
        public Action<bool> OnMuted;
        public Action<float> OnMusicVolume;
        public Action<float> OnSfxVolume;

        public void Show(bool musicOn, float musicVolume, bool sfxOn, float sfxVolume, string status)
        {
            _musicOn.SetIsOnWithoutNotify(musicOn);
            _musicVolume.SetValueWithoutNotify(musicVolume);
            _sfxOn.SetIsOnWithoutNotify(sfxOn);
            _sfxVolume.SetValueWithoutNotify(sfxVolume);
            _status.text = status;
        }

        private void OnEnable()
        {
            _beep.onClick.AddListener(Beep);
            _boomLeft.onClick.AddListener(BoomLeft);
            _boomRight.onClick.AddListener(BoomRight);
            _themeA.onClick.AddListener(ThemeA);
            _themeB.onClick.AddListener(ThemeB);
            _stopMusic.onClick.AddListener(StopMusic);
            _duck.onClick.AddListener(Duck);
            _normal.onClick.AddListener(Normal);
            _unload.onClick.AddListener(Unload);
            _musicOn.onValueChanged.AddListener(MusicOn);
            _sfxOn.onValueChanged.AddListener(SfxOn);
            _muted.onValueChanged.AddListener(Muted);
            _musicVolume.onValueChanged.AddListener(MusicVolume);
            _sfxVolume.onValueChanged.AddListener(SfxVolume);
        }

        private void OnDisable()
        {
            _beep.onClick.RemoveListener(Beep);
            _boomLeft.onClick.RemoveListener(BoomLeft);
            _boomRight.onClick.RemoveListener(BoomRight);
            _themeA.onClick.RemoveListener(ThemeA);
            _themeB.onClick.RemoveListener(ThemeB);
            _stopMusic.onClick.RemoveListener(StopMusic);
            _duck.onClick.RemoveListener(Duck);
            _normal.onClick.RemoveListener(Normal);
            _unload.onClick.RemoveListener(Unload);
            _musicOn.onValueChanged.RemoveListener(MusicOn);
            _sfxOn.onValueChanged.RemoveListener(SfxOn);
            _muted.onValueChanged.RemoveListener(Muted);
            _musicVolume.onValueChanged.RemoveListener(MusicVolume);
            _sfxVolume.onValueChanged.RemoveListener(SfxVolume);
        }

        private void Beep() => OnBeep?.Invoke();
        private void BoomLeft() => OnBoom?.Invoke(-8f);
        private void BoomRight() => OnBoom?.Invoke(8f);
        private void ThemeA() => OnThemeA?.Invoke();
        private void ThemeB() => OnThemeB?.Invoke();
        private void StopMusic() => OnStopMusic?.Invoke();
        private void Duck() => OnDuck?.Invoke();
        private void Normal() => OnNormal?.Invoke();
        private void Unload() => OnUnload?.Invoke();
        private void MusicOn(bool on) => OnMusicOn?.Invoke(on);
        private void SfxOn(bool on) => OnSfxOn?.Invoke(on);
        private void Muted(bool on) => OnMuted?.Invoke(on);
        private void MusicVolume(float value) => OnMusicVolume?.Invoke(value);
        private void SfxVolume(float value) => OnSfxVolume?.Invoke(value);
    }
}

#endif
