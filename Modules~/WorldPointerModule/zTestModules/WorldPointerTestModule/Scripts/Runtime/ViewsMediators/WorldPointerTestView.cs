#if UNITY_EDITOR
using System;
using FlowIoC.BaseModule.Injectable.Components;
using FlowIoC.BaseModule.ViewsMediators.View;
using UnityEngine;
using UnityEngine.UI;

namespace Modules.WorldPointerModule.WorldPointerTestModule.ViewsMediators
{
    /// <summary>
    /// Scene references and raw motion, nothing else: the camera pivot turns, the cubes stand
    /// still, and the buttons report that they were pressed. It does not know what a pointer is,
    /// and it draws none - the sample screen does. Buttons are wired in OnEnable and unwired in
    /// OnDisable.
    /// </summary>
    [RequireComponent(typeof(ViewInjector))]
    public class WorldPointerTestView : MonoBehaviour, IView
    {
        public bool IsRegistered { get; set; }

        [SerializeField] private Transform _cameraPivot;
        [SerializeField] private float _orbitDegreesPerSecond = 20f;
        [SerializeField] private Transform[] _targets;
        [SerializeField] private Button _registerButton;
        [SerializeField] private Button _changeContentButton;
        [SerializeField] private Toggle _hiddenToggle;
        [SerializeField] private Button _toggleScreenButton;
        [SerializeField] private Button _unregisterAllButton;
        [SerializeField] private Text _countLabel;

        public Transform[] Targets => _targets;

        public Action OnRegisterPressed;
        public Action OnChangeContentPressed;
        public Action<bool> OnHiddenToggled;
        public Action OnToggleScreenPressed;
        public Action OnUnregisterAllPressed;

        public void SetCount(string text)
        {
            if (_countLabel != null) _countLabel.text = text;
        }

        private void OnEnable()
        {
            if (_registerButton != null) _registerButton.onClick.AddListener(RegisterPressed);
            if (_changeContentButton != null) _changeContentButton.onClick.AddListener(ChangeContentPressed);
            if (_hiddenToggle != null) _hiddenToggle.onValueChanged.AddListener(HiddenToggled);
            if (_toggleScreenButton != null) _toggleScreenButton.onClick.AddListener(ToggleScreenPressed);
            if (_unregisterAllButton != null) _unregisterAllButton.onClick.AddListener(UnregisterAllPressed);
        }

        private void OnDisable()
        {
            if (_registerButton != null) _registerButton.onClick.RemoveListener(RegisterPressed);
            if (_changeContentButton != null) _changeContentButton.onClick.RemoveListener(ChangeContentPressed);
            if (_hiddenToggle != null) _hiddenToggle.onValueChanged.RemoveListener(HiddenToggled);
            if (_toggleScreenButton != null) _toggleScreenButton.onClick.RemoveListener(ToggleScreenPressed);
            if (_unregisterAllButton != null) _unregisterAllButton.onClick.RemoveListener(UnregisterAllPressed);
        }

        private void Update()
        {
            if (_cameraPivot != null)
                _cameraPivot.Rotate(0f, _orbitDegreesPerSecond * Time.deltaTime, 0f, Space.World);
        }

        private void RegisterPressed() => OnRegisterPressed?.Invoke();

        private void ChangeContentPressed() => OnChangeContentPressed?.Invoke();

        private void HiddenToggled(bool hidden) => OnHiddenToggled?.Invoke(hidden);

        private void ToggleScreenPressed() => OnToggleScreenPressed?.Invoke();

        private void UnregisterAllPressed() => OnUnregisterAllPressed?.Invoke();
    }
}
#endif