#if UNITY_EDITOR

using System;
using FlowIoC.BaseModule.Injectable.Components;
using FlowIoC.BaseModule.ViewsMediators.View;
using UnityEngine;
using UnityEngine.UI;

namespace Modules.AbTestFlowModule.AbTestFlowTestModule.ViewsMediators
{
    /// <summary>Scene references and raw input. It does not know what a group is.</summary>
    [RequireComponent(typeof(ViewInjector))]
    public class AbTestFlowTestView : MonoBehaviour, IView
    {
        public bool IsRegistered { get; set; }

        [SerializeField] private Text _experimentLabel;
        [SerializeField] private Text _probeLabel;
        [SerializeField] private Button _clearButton;
        [SerializeField] private Button _raiseVersionButton;
        [SerializeField] private Button _rerollButton;

        public Action OnClearPressed;
        public Action OnRaiseVersionPressed;
        public Action OnRerollPressed;

        public void SetExperiment(string text)
        {
            if (_experimentLabel != null)
                _experimentLabel.text = text;
        }

        public void SetProbe(string text)
        {
            if (_probeLabel != null)
                _probeLabel.text = text;
        }

        private void OnEnable()
        {
            if (_clearButton != null)
                _clearButton.onClick.AddListener(ClearClicked);

            if (_raiseVersionButton != null)
                _raiseVersionButton.onClick.AddListener(RaiseVersionClicked);

            if (_rerollButton != null)
                _rerollButton.onClick.AddListener(RerollClicked);
        }

        private void OnDisable()
        {
            if (_clearButton != null)
                _clearButton.onClick.RemoveListener(ClearClicked);

            if (_raiseVersionButton != null)
                _raiseVersionButton.onClick.RemoveListener(RaiseVersionClicked);

            if (_rerollButton != null)
                _rerollButton.onClick.RemoveListener(RerollClicked);
        }

        private void ClearClicked() => OnClearPressed?.Invoke();

        private void RaiseVersionClicked() => OnRaiseVersionPressed?.Invoke();

        private void RerollClicked() => OnRerollPressed?.Invoke();
    }
}

#endif
