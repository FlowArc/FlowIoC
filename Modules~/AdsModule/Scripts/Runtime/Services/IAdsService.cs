using System;
using Modules.AdsModule.Data.ValueObjects;
using Modules.AdsModule.Enums;
using Modules.AdsModule.Shared.Enums;

namespace Modules.AdsModule.Services
{
    /// <summary>
    /// The module's one counterpart. Injecting this interface is the sanctioned cross-module
    /// reference: a game's Command calls ShowRewarded at the point where it decided the player
    /// may watch for a reward, and reads the answer in the callback. The steps a game binds
    /// instead of writing that Command sit under <see cref="Commands"/>, one file each beside
    /// this one. Plug and Unplug are for a provider's hosted context, never for a game's Command.
    /// </summary>
    public partial interface IAdsService
    {
        AdsProviderState ProviderState { get; }

        string ProviderName { get; }

        /// <summary>Whether an ad of the format is loaded and showable now; what a watch-ad button reads.</summary>
        bool IsReady(AdFormat format);

        bool AdsRemoved { get; }

        /// <summary>Only needed when CD_Ads.InitializeOnLaunch is off; bound after the game's consent step.</summary>
        void Initialize();

        /// <summary>Shows the loaded rewarded ad. done fires once, on the main thread, possibly in this call.</summary>
        void ShowRewarded(string placement, Action<AdResultVO> done = null);

        /// <summary>Shows the loaded interstitial, unless ads are removed or the interval has not passed.</summary>
        void ShowInterstitial(string placement, Action<AdResultVO> done = null);

        /// <summary>Handed on to the provider as given, before it initializes if it has not yet; the module resolves nothing.</summary>
        void SetConsent(AdsConsentVO consent);

        /// <summary>The "no ads" purchase. Not persisted here: the game re-applies it at boot from its own save.</summary>
        void SetAdsRemoved(bool removed);

        void SetMuted(bool muted);

        /// <summary>The provider's own debug screen; nothing without a provider.</summary>
        void ShowProviderDebugger();

        /// <summary>Called by a provider's hosted context in its Setup, never by a game's Command.</summary>
        void Plug(IAdsProvider provider);

        void Unplug(IAdsProvider provider);

        /// <summary>
        /// The steps a game binds in a sequence of its own. They sit inside the interface so that
        /// the one name a game knows - the Service it injects - is also where its steps are found,
        /// and the flow reads from the Context: which placement, after which step. Each step is a
        /// file of its own, <c>IAdsService.Commands.&lt;Step&gt;.cs</c>.
        /// </summary>
        public static partial class Commands
        {
        }
    }
}
