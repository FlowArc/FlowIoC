#if UNITY_EDITOR

using System;
using FlowIoC.BaseModule.Injectable.Components;
using FlowIoC.BaseModule.ViewsMediators.View;
using Modules.HapticModule.Enums;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Modules.HapticModule.HapticTestModule.ViewsMediators
{
    /// <summary>Scene references and raw input: nine buttons in HapticPreset order, a toggle, a label.</summary>
    [RequireComponent(typeof(ViewInjector))]
    public class HapticTestView : MonoBehaviour, IView
    {
        public bool IsRegistered { get; set; }

        [SerializeField] private Button[] _presetButtons;
        [SerializeField] private Toggle _enabledToggle;
        [SerializeField] private Text _statusLabel;

        public Action<HapticPreset> OnPresetPressed;
        public Action<bool> OnEnabledChanged;

        private UnityAction[] _buttonActions;

        public void SetStatus(string text)
        {
            if (_statusLabel != null)
                _statusLabel.text = text;
        }

        public void SetEnabledWithoutNotify(bool on)
        {
            if (_enabledToggle != null)
                _enabledToggle.SetIsOnWithoutNotify(on);
        }

        private void OnEnable()
        {
            if (_presetButtons != null)
            {
                _buttonActions = new UnityAction[_presetButtons.Length];

                for (int i = 0; i < _presetButtons.Length; i++)
                {
                    if (_presetButtons[i] == null) continue;

                    var preset = (HapticPreset) i;
                    _buttonActions[i] = () => OnPresetPressed?.Invoke(preset);
                    _presetButtons[i].onClick.AddListener(_buttonActions[i]);
                }
            }

            if (_enabledToggle != null)
                _enabledToggle.onValueChanged.AddListener(ToggleChanged);
        }

        private void OnDisable()
        {
            if (_presetButtons != null && _buttonActions != null)
            {
                for (int i = 0; i < _presetButtons.Length; i++)
                {
                    if (_presetButtons[i] != null && _buttonActions[i] != null)
                        _presetButtons[i].onClick.RemoveListener(_buttonActions[i]);
                }
            }

            if (_enabledToggle != null)
                _enabledToggle.onValueChanged.RemoveListener(ToggleChanged);
        }

        private void ToggleChanged(bool on) => OnEnabledChanged?.Invoke(on);
    }
}

#endif
