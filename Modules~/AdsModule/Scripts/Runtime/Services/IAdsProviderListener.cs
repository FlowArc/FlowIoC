using Modules.AdsModule.Data.ValueObjects;
using Modules.AdsModule.Shared.Data.ValueObjects;
using Modules.AdsModule.Shared.Enums;

namespace Modules.AdsModule.Services
{
    /// <summary>
    /// The module's ear. A provider reports every SDK callback here, on the main thread, and the
    /// module turns each into a step. RewardEarned comes before Closed: a plug over an SDK that
    /// cannot promise the order holds its Closed until the reward answer has arrived.
    /// </summary>
    public interface IAdsProviderListener
    {
        void OnInitialized(bool ready);

        void OnLoaded(AdFormat format);

        void OnLoadFailed(AdFormat format, string error);

        void OnDisplayed(AdFormat format);

        void OnDisplayFailed(AdFormat format, string error);

        void OnClicked(AdFormat format);

        void OnRewardEarned(AdRewardVO reward);

        void OnClosed(AdFormat format);

        void OnRevenuePaid(AdRevenueVO revenue);
    }
}
