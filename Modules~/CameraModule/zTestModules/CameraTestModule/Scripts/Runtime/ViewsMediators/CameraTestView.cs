#if UNITY_EDITOR

using System;
using FlowIoC.BaseModule.Injectable.Components;
using FlowIoC.BaseModule.ViewsMediators.View;
using UnityEngine;
using UnityEngine.UI;

namespace Modules.CameraModule.CameraTestModule.ViewsMediators
{
    /// <summary>Scene references and raw input: a switch, a move and a zoom button, and a label.</summary>
    [RequireComponent(typeof(ViewInjector))]
    public class CameraTestView : MonoBehaviour, IView
    {
        public bool IsRegistered { get; set; }

        [SerializeField] private Button _switchButton;
        [SerializeField] private Button _moveButton;
        [SerializeField] private Button _zoomButton;
        [SerializeField] private Text _statusLabel;

        public Action OnSwitchPressed;
        public Action OnMovePressed;
        public Action OnZoomPressed;

        public void SetStatus(string text)
        {
            if (_statusLabel != null)
                _statusLabel.text = text;
        }

        private void OnEnable()
        {
            if (_switchButton != null)
                _switchButton.onClick.AddListener(SwitchPressed);

            if (_moveButton != null)
                _moveButton.onClick.AddListener(MovePressed);

            if (_zoomButton != null)
                _zoomButton.onClick.AddListener(ZoomPressed);
        }

        private void OnDisable()
        {
            if (_switchButton != null)
                _switchButton.onClick.RemoveListener(SwitchPressed);

            if (_moveButton != null)
                _moveButton.onClick.RemoveListener(MovePressed);

            if (_zoomButton != null)
                _zoomButton.onClick.RemoveListener(ZoomPressed);
        }

        private void SwitchPressed() => OnSwitchPressed?.Invoke();

        private void MovePressed() => OnMovePressed?.Invoke();

        private void ZoomPressed() => OnZoomPressed?.Invoke();
    }
}

#endif
