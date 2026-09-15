#if UNITY_EDITOR

using System;
using FlowIoC.BaseModule.Injectable.Components;
using FlowIoC.BaseModule.ViewsMediators.View;
using UnityEngine;
using UnityEngine.UI;

namespace Modules.LocalSaveModule.LocalSaveTestModule.ViewsMediators
{
    /// <summary>Scene references and raw input. It does not know what the number means.</summary>
    [RequireComponent(typeof(ViewInjector))]
    public class LocalSaveTestView : MonoBehaviour, IView
    {
        public bool IsRegistered { get; set; }

        [SerializeField] private Text _counterLabel;
        [SerializeField] private Button _incrementButton;

        public Action OnIncrementPressed;

        public void SetCounter(string text)
        {
            if (_counterLabel != null)
                _counterLabel.text = text;
        }

        private void Awake()
        {
            if (_incrementButton != null)
                _incrementButton.onClick.AddListener(() => OnIncrementPressed?.Invoke());
        }
    }
}

#endif
