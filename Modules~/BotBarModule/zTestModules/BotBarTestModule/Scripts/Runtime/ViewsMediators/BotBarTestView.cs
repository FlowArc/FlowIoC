#if UNITY_EDITOR
using System;
using FlowIoC.BaseModule.Injectable.Components;
using FlowIoC.BaseModule.ViewsMediators.View;
using UnityEngine;
using UnityEngine.UI;

namespace Modules.BotBarModule.BotBarTestModule.ViewsMediators
{
    /// <summary>Scene references and raw input: seven buttons and a label.</summary>
    [RequireComponent(typeof(ViewInjector))]
    public class BotBarTestView : MonoBehaviour, IView
    {
        public bool IsRegistered { get; set; }

        [SerializeField] private Button _selectShopButton;
        [SerializeField] private Button _lockClanButton;
        [SerializeField] private Button _unlockClanButton;
        [SerializeField] private Button _badgeShopButton;
        [SerializeField] private Button _clearBadgeButton;
        [SerializeField] private Button _hideButton;
        [SerializeField] private Button _showButton;
        [SerializeField] private Text _statusLabel;

        public Action OnSelectShop;
        public Action<bool> OnSetClanLocked;
        public Action OnIncrementShopBadge;
        public Action OnClearShopBadge;
        public Action<bool> OnSetBarShown;

        public void SetStatus(string text)
        {
            if (_statusLabel != null)
                _statusLabel.text = text;
        }

        private void OnEnable()
        {
            _selectShopButton.onClick.AddListener(SelectShop);
            _lockClanButton.onClick.AddListener(LockClan);
            _unlockClanButton.onClick.AddListener(UnlockClan);
            _badgeShopButton.onClick.AddListener(BadgeShop);
            _clearBadgeButton.onClick.AddListener(ClearBadge);
            _hideButton.onClick.AddListener(HideBar);
            _showButton.onClick.AddListener(ShowBar);
        }

        private void OnDisable()
        {
            _selectShopButton.onClick.RemoveListener(SelectShop);
            _lockClanButton.onClick.RemoveListener(LockClan);
            _unlockClanButton.onClick.RemoveListener(UnlockClan);
            _badgeShopButton.onClick.RemoveListener(BadgeShop);
            _clearBadgeButton.onClick.RemoveListener(ClearBadge);
            _hideButton.onClick.RemoveListener(HideBar);
            _showButton.onClick.RemoveListener(ShowBar);
        }

        public void SelectShop() => OnSelectShop?.Invoke();

        public void LockClan() => OnSetClanLocked?.Invoke(true);

        public void UnlockClan() => OnSetClanLocked?.Invoke(false);

        public void BadgeShop() => OnIncrementShopBadge?.Invoke();

        public void ClearBadge() => OnClearShopBadge?.Invoke();

        public void HideBar() => OnSetBarShown?.Invoke(false);

        public void ShowBar() => OnSetBarShown?.Invoke(true);
    }
}
#endif
