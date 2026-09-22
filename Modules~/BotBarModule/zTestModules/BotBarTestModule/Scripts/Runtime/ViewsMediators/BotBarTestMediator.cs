#if UNITY_EDITOR
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.ViewsMediators.Mediator;
using Modules.BotBarModule.BotBarTestModule.Signals;

namespace Modules.BotBarModule.BotBarTestModule.ViewsMediators
{
    public class BotBarTestMediator : IMediator
    {
        [Inject] private BotBarTestView _view { get; set; }
        [InjectSignal] private BotBarTestInternalSignals _signals { get; set; }

        public void OnRegister()
        {
            _view.OnSelectShop += SelectShop;
            _view.OnSetClanLocked += SetClanLocked;
            _view.OnIncrementShopBadge += IncrementShopBadge;
            _view.OnClearShopBadge += ClearShopBadge;
            _view.OnSetBarShown += SetBarShown;
            _signals.StatusChanged.AddListener(OnStatusChanged);
        }

        public void OnRemove()
        {
            _view.OnSelectShop -= SelectShop;
            _view.OnSetClanLocked -= SetClanLocked;
            _view.OnIncrementShopBadge -= IncrementShopBadge;
            _view.OnClearShopBadge -= ClearShopBadge;
            _view.OnSetBarShown -= SetBarShown;
            _signals.StatusChanged.RemoveListener(OnStatusChanged);
        }

        private void SelectShop() => _signals.SelectShop.Dispatch();

        private void SetClanLocked(bool locked) => _signals.SetClanLocked.Dispatch(locked);

        private void IncrementShopBadge() => _signals.IncrementShopBadge.Dispatch();

        private void ClearShopBadge() => _signals.ClearShopBadge.Dispatch();

        private void SetBarShown(bool shown) => _signals.SetBarShown.Dispatch(shown);

        private void OnStatusChanged(string text) => _view.SetStatus(text);
    }
}
#endif
