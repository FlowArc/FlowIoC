#if UNITY_EDITOR

using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.ViewsMediators.Mediator;
using Modules.AdsModule.AdsTestModule.Signals;

namespace Modules.AdsModule.AdsTestModule.ViewsMediators
{
    public class AdsTestMediator : IMediator
    {
        [Inject] private AdsTestView _view { get; set; }

        [InjectSignal] private AdsTestInternalSignals _signals { get; set; }

        public void OnRegister()
        {
            _view.OnAnswerProvider += AnswerProvider;
            _view.OnInitialize += Initialize;
            _view.OnShowRewarded += ShowRewarded;
            _view.OnShowInterstitial += ShowInterstitial;
            _view.OnRewardAndClose += RewardAndClose;
            _view.OnClose += Close;
            _view.OnFailToDisplay += FailToDisplay;
            _view.OnPayRevenue += PayRevenue;
            _view.OnConsentAll += ConsentAll;
            _view.OnLoadsFail += LoadsFail;
            _view.OnSilentShow += SilentShow;
            _view.OnAdsRemoved += AdsRemoved;
            _signals.StateChanged.AddListener(OnStateChanged);

            // The view may register after the Roots have launched, so the first report is asked
            // for here as well as from Launch, or the label could keep its placeholder.
            _signals.ReportState.Dispatch();
        }

        public void OnRemove()
        {
            _view.OnAnswerProvider -= AnswerProvider;
            _view.OnInitialize -= Initialize;
            _view.OnShowRewarded -= ShowRewarded;
            _view.OnShowInterstitial -= ShowInterstitial;
            _view.OnRewardAndClose -= RewardAndClose;
            _view.OnClose -= Close;
            _view.OnFailToDisplay -= FailToDisplay;
            _view.OnPayRevenue -= PayRevenue;
            _view.OnConsentAll -= ConsentAll;
            _view.OnLoadsFail -= LoadsFail;
            _view.OnSilentShow -= SilentShow;
            _view.OnAdsRemoved -= AdsRemoved;
            _signals.StateChanged.RemoveListener(OnStateChanged);
        }

        private void AnswerProvider(bool ready) => _signals.AnswerProvider.Dispatch(ready);
        private void Initialize() => _signals.Initialize.Dispatch();
        private void ShowRewarded() => _signals.ShowRewarded.Dispatch();
        private void ShowInterstitial() => _signals.ShowInterstitial.Dispatch();
        private void RewardAndClose() => _signals.RewardAndClose.Dispatch();
        private void Close() => _signals.Close.Dispatch();
        private void FailToDisplay() => _signals.FailToDisplay.Dispatch();
        private void PayRevenue() => _signals.PayRevenue.Dispatch();
        private void ConsentAll() => _signals.ConsentAll.Dispatch();
        private void LoadsFail(bool on) => _signals.SetLoadsFail.Dispatch(on);
        private void SilentShow(bool on) => _signals.SetSilentShow.Dispatch(on);
        private void AdsRemoved(bool on) => _signals.SetAdsRemoved.Dispatch(on);

        private void OnStateChanged(string text) => _view.SetStatus(text);
    }
}

#endif
