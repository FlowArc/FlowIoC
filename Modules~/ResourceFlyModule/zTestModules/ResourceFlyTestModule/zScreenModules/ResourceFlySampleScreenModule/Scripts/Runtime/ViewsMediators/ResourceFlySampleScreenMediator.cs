#if UNITY_EDITOR
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.ViewsMediators.Mediator;
using FlowIoC.ScreenModule.Enums;
using FlowIoC.ScreenModule.ViewsMediators.Screen;
using UnityEngine;
using Modules.ResourceFlyModule.ResourceFlyTestModule.ResourceFlySampleScreenModule.Signals;

namespace Modules.ResourceFlyModule.ResourceFlyTestModule.ResourceFlySampleScreenModule.ViewsMediators
{
    /// <summary>
    /// Passes presses on; nothing is decided here. It listens to the view while the screen is up,
    /// and a press while the screen animates is not a signal.
    /// </summary>
    public class ResourceFlySampleScreenMediator : IMediator
    {
        [Inject] private ResourceFlySampleScreenView _view { get; set; }
        [InjectSignal] private ResourceFlySampleScreenInternalSignals _internalSignals { get; set; }

        private bool _canSend => _view.Data.State == ScreenState.AvailableToSendSignal;

        public void OnRegister()
        {
            _view.ShowCompleted += OnScreenShown;
            _view.HideCompleted += OnScreenHidden;
        }

        public void OnRemove()
        {
            _view.ShowCompleted -= OnScreenShown;
            _view.HideCompleted -= OnScreenHidden;
        }

        private void OnScreenShown(IScreenBody screen)
        {
            _view.ScatterPressed += OnScatter;
            _view.DirectPressed += OnDirect;
            _view.CurvedPressed += OnCurved;
            _view.BanknotePressed += OnBanknote;
            _view.ItemPressed += OnItem;
            _view.StonesPressed += OnStones;
        }

        private void OnScreenHidden(IScreenBody screen)
        {
            _view.ScatterPressed -= OnScatter;
            _view.DirectPressed -= OnDirect;
            _view.CurvedPressed -= OnCurved;
            _view.BanknotePressed -= OnBanknote;
            _view.ItemPressed -= OnItem;
            _view.StonesPressed -= OnStones;
        }

        private void OnScatter(int amount)
        {
            if (_canSend) _internalSignals.FlyScatter.Dispatch(amount);
        }

        private void OnDirect(int amount)
        {
            if (_canSend) _internalSignals.FlyDirect.Dispatch(amount);
        }

        private void OnCurved(int amount)
        {
            if (_canSend) _internalSignals.FlyCurved.Dispatch(amount);
        }

        private void OnBanknote(int amount)
        {
            if (_canSend) _internalSignals.FlyBanknote.Dispatch(amount);
        }

        private void OnItem(Sprite sprite)
        {
            if (_canSend) _internalSignals.FlyItem.Dispatch(sprite);
        }

        private void OnStones(int amount)
        {
            if (_canSend) _internalSignals.FlyStones.Dispatch(amount);
        }
    }
}
#endif
