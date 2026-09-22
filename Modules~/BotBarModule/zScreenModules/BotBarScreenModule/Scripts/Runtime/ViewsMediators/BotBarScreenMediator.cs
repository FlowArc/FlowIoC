using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.ViewsMediators.Mediator;
using FlowIoC.ScreenModule.Enums;
using FlowIoC.ScreenModule.Extensions;
using FlowIoC.ScreenModule.ViewsMediators.Screen;
using Modules.BotBarModule.BotBarScreenModule.Signals;
using Modules.BotBarModule.Shared.Enums;

namespace Modules.BotBarModule.BotBarScreenModule.ViewsMediators
{
    /// <summary>
    /// Applies what the System announces to the bar and reports a tap. Nothing here decides:
    /// whether the tapped tab may be selected is the System's, and the answer comes back as
    /// ApplySelection or as nothing at all.
    /// </summary>
    public class BotBarScreenMediator : IMediator
    {
        [Inject] private BotBarScreenView _view { get; set; }
        [InjectSignal] private BotBarScreenSignals _signals { get; set; }

        public void OnRegister()
        {
            _view.ShowCompleted += OnScreenShown;
            _view.HideCompleted += OnScreenHidden;

            _signals.Incoming.ApplySelection.AddListener(OnApplySelection);
            _signals.Incoming.ApplyTabState.AddListener(OnApplyTabState);
            _signals.Incoming.ApplyBadge.AddListener(OnApplyBadge);
            _signals.Incoming.Show.AddListener(OnShow);
            _signals.Incoming.Hide.AddListener(OnHide);
        }

        public void OnRemove()
        {
            _view.ShowCompleted -= OnScreenShown;
            _view.HideCompleted -= OnScreenHidden;

            _signals.Incoming.ApplySelection.RemoveListener(OnApplySelection);
            _signals.Incoming.ApplyTabState.RemoveListener(OnApplyTabState);
            _signals.Incoming.ApplyBadge.RemoveListener(OnApplyBadge);
            _signals.Incoming.Show.RemoveListener(OnShow);
            _signals.Incoming.Hide.RemoveListener(OnHide);
        }

        private void OnScreenShown(IScreenBody screen) => _view.TabTapped += OnTabTapped;

        private void OnScreenHidden(IScreenBody screen) => _view.TabTapped -= OnTabTapped;

        // On stage, animating or not: a badge that lands during the slide in is not dropped.
        private bool _isUp => _view.Data.HasState(ScreenState.InUse);

        private void OnApplySelection(string previous, string current)
        {
            if (!_isUp) return;
            _view.ShowSelection(previous, current);
        }

        private void OnApplyTabState(string key, BotBarTabState state)
        {
            if (!_isUp) return;
            _view.ShowTabState(key, state);
        }

        private void OnApplyBadge(string key, int count)
        {
            if (!_isUp) return;
            _view.ShowBadge(key, count);
        }

        private void OnShow()
        {
            if (!_isUp) return;
            _view.ShowBar(true, animate: true);
        }

        private void OnHide()
        {
            if (!_isUp) return;
            _view.ShowBar(false, animate: true);
        }

        // Strict: a tap landing while the bar slides in or out does not become a signal.
        private void OnTabTapped(string key)
        {
            if (_view.Data.State != ScreenState.AvailableToSendSignal) return;
            _signals.Outgoing.TabTapped.Dispatch(key);
        }
    }
}