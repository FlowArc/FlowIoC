#if UNITY_EDITOR

using System;
using FlowIoC.BaseModule.Injectable.Components;
using FlowIoC.BaseModule.ViewsMediators.View;
using UnityEngine;
using UnityEngine.UI;

namespace Modules.MobileNotificationModule.MobileNotificationTestModule.ViewsMediators
{
    /// <summary>Scene references and raw input: eight buttons and a label.</summary>
    [RequireComponent(typeof(ViewInjector))]
    public class MobileNotificationTestView : MonoBehaviour, IView
    {
        public bool IsRegistered { get; set; }

        [SerializeField] private Button _requestPermissionButton;
        [SerializeField] private Button _scheduleAButton;
        [SerializeField] private Button _scheduleBButton;
        [SerializeField] private Button _cancelAButton;
        [SerializeField] private Button _cancelAllButton;
        [SerializeField] private Button _openSettingsButton;
        [SerializeField] private Button _leaveButton;
        [SerializeField] private Button _returnButton;
        [SerializeField] private Text _statusLabel;

        public Action OnRequestPermission;
        public Action<string> OnSchedule;
        public Action<string> OnCancel;
        public Action OnCancelAll;
        public Action OnOpenSettings;
        public Action OnLeave;
        public Action OnReturn;

        public void SetStatus(string text)
        {
            if (_statusLabel != null)
                _statusLabel.text = text;
        }

        private void OnEnable()
        {
            _requestPermissionButton?.onClick.AddListener(RequestPermission);
            _scheduleAButton?.onClick.AddListener(ScheduleA);
            _scheduleBButton?.onClick.AddListener(ScheduleB);
            _cancelAButton?.onClick.AddListener(CancelA);
            _cancelAllButton?.onClick.AddListener(CancelAll);
            _openSettingsButton?.onClick.AddListener(OpenSettings);
            _leaveButton?.onClick.AddListener(Leave);
            _returnButton?.onClick.AddListener(Return);
        }

        private void OnDisable()
        {
            _requestPermissionButton?.onClick.RemoveListener(RequestPermission);
            _scheduleAButton?.onClick.RemoveListener(ScheduleA);
            _scheduleBButton?.onClick.RemoveListener(ScheduleB);
            _cancelAButton?.onClick.RemoveListener(CancelA);
            _cancelAllButton?.onClick.RemoveListener(CancelAll);
            _openSettingsButton?.onClick.RemoveListener(OpenSettings);
            _leaveButton?.onClick.RemoveListener(Leave);
            _returnButton?.onClick.RemoveListener(Return);
        }

        private void RequestPermission() => OnRequestPermission?.Invoke();
        private void ScheduleA() => OnSchedule?.Invoke("a");
        private void ScheduleB() => OnSchedule?.Invoke("b");
        private void CancelA() => OnCancel?.Invoke("a");
        private void CancelAll() => OnCancelAll?.Invoke();
        private void OpenSettings() => OnOpenSettings?.Invoke();
        private void Leave() => OnLeave?.Invoke();
        private void Return() => OnReturn?.Invoke();
    }
}

#endif
