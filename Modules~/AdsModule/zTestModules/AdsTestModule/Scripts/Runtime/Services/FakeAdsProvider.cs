#if UNITY_EDITOR

using System.Collections.Generic;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.Provider.Coroutine;
using FlowIoC.ConsoleModule;
using Modules.AdsModule.Data.ValueObjects;
using Modules.AdsModule.Services;
using Modules.AdsModule.Shared.Data.ValueObjects;
using Modules.AdsModule.Shared.Enums;

namespace Modules.AdsModule.AdsTestModule.Services
{
    /// <summary>
    /// The test scene's SDK. It draws nothing and answers from the buttons: the initialize answer
    /// is held until Provider ready or Provider failed, a load lands after half a second (or
    /// fails while Loads fail is on), a show is on screen until Reward + close, Close or Fail to
    /// display - or reports nothing at all while Silent show is on, so the timeout is seen.
    /// </summary>
    public class FakeAdsProvider : IAdsProvider
    {
        private const float LOAD_DELAY_SECONDS = 0.5f;

        [Inject] private ICoroutineProvider _coroutines { get; set; }

        private readonly bool[] _loaded = new bool[2];
        private IAdsProviderListener _listener;
        private bool _answered;

        public readonly List<string> Calls = new();

        public bool LoadsFail;
        public bool SilentShow;

        public string Name => "Fake";

        public bool IsHoldingTheAnswer => _listener != null && !_answered;

        /// <summary>The format on screen, or null.</summary>
        public AdFormat? OnScreen { get; private set; }

        public string OnScreenPlacement { get; private set; } = string.Empty;

        public void Initialize(IAdsProviderListener listener)
        {
            _listener = listener;
            _answered = false;
            Calls.Add("Initialize");
            FlowLogger.Log("Initialize - holding the answer; press Provider ready or Provider failed.");
        }

        /// <summary>Answers the held initialize; a second press answers again, which the module ignores or refuses as it will.</summary>
        public void Answer(bool ready)
        {
            if (_listener == null)
            {
                FlowLogger.Log("Answer - nothing asked yet; the service has not initialized this provider.");
                return;
            }

            _answered = true;
            _listener.OnInitialized(ready);
        }

        public void Load(AdFormat format)
        {
            Calls.Add("Load " + format);

            _coroutines.WaitForSecondsRealTime(LOAD_DELAY_SECONDS, () =>
            {
                if (LoadsFail)
                {
                    _listener.OnLoadFailed(format, "fake load failure");
                    return;
                }

                _loaded[(int) format] = true;
                _listener.OnLoaded(format);
            });
        }

        public bool IsReady(AdFormat format) => _loaded[(int) format];

        public void Show(AdFormat format, string placement)
        {
            Calls.Add($"Show {format}/{placement}");
            _loaded[(int) format] = false;
            OnScreen = format;
            OnScreenPlacement = placement;

            if (SilentShow)
            {
                FlowLogger.Log("Show - silent: reporting nothing, so the module's timeout is seen.");
                return;
            }

            _listener.OnDisplayed(format);
        }

        public void RewardAndClose()
        {
            if (OnScreen == null) return;

            AdFormat format = OnScreen.Value;

            if (format == AdFormat.Rewarded)
                _listener.OnRewardEarned(new AdRewardVO("coins", 10));

            Close();
        }

        public void Close()
        {
            if (OnScreen == null) return;

            AdFormat format = OnScreen.Value;
            OnScreen = null;
            _listener.OnClosed(format);
        }

        public void FailToDisplay()
        {
            if (OnScreen == null) return;

            AdFormat format = OnScreen.Value;
            OnScreen = null;
            _listener.OnDisplayFailed(format, "fake display failure");
        }

        public void PayRevenue()
        {
            if (OnScreen == null) return;

            _listener.OnRevenuePaid(new AdRevenueVO(Name, OnScreen.Value, OnScreenPlacement, "FakeNetwork", "fake-unit", 0.01, "USD", "exact"));
        }

        public void SetConsent(AdsConsentVO consent) => Calls.Add("SetConsent " + consent);

        public void SetMuted(bool muted) => Calls.Add("SetMuted " + muted);

        public void ShowDebugger() => Calls.Add("ShowDebugger");
    }
}

#endif
