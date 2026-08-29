#if UNITY_EDITOR
using System;
using FlowIoC.BaseModule.Injectable.Components;
using FlowIoC.BaseModule.ViewsMediators.View;
using UnityEngine;
using UnityEngine.UI;

namespace Modules.CountdownServiceModule.CountdownServiceTestModule.ViewsMediators
{
    /// <summary>
    /// Scene references and raw input, and nothing else. It does not know what a countdown is:
    /// it shows the strings it is handed and reports that a button was pressed.
    /// </summary>
    [RequireComponent(typeof(ViewInjector))]
    public class CountdownTestView : MonoBehaviour, IView
    {
        public bool IsRegistered { get; set; }

        [SerializeField] private Text _remainingLabel;
        [SerializeField] private Text _elapsedLabel;
        [SerializeField] private Text _statusLabel;

        [SerializeField] private Button _startButton;
        [SerializeField] private Button _stopButton;

        public Action OnStartPressed;
        public Action OnStopPressed;

        public void SetRemaining(string text) => Write(_remainingLabel, text);

        public void SetElapsed(string text) => Write(_elapsedLabel, text);

        public void SetStatus(string text) => Write(_statusLabel, text);

        private void Awake()
        {
            if (_startButton != null) _startButton.onClick.AddListener(() => OnStartPressed?.Invoke());
            if (_stopButton != null) _stopButton.onClick.AddListener(() => OnStopPressed?.Invoke());
        }

        /// <summary>
        /// A label the scene never got is left out rather than throwing, so the view still runs
        /// in a scene wired up only far enough for what is being tried out.
        /// </summary>
        private void Write(Text label, string text)
        {
            if (label != null) label.text = text;
        }
    }
}
#endif
