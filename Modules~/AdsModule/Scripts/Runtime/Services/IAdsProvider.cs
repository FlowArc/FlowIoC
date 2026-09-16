using Modules.AdsModule.Data.ValueObjects;
using Modules.AdsModule.Shared.Enums;

namespace Modules.AdsModule.Services
{
    /// <summary>
    /// One ad SDK behind the service - a mediation, so there is one. A provider is plugged by its
    /// own hosted context, is asked to initialize once the service launches (or when the game says
    /// so), and from then on loads and shows what it is told. It never decides whether an ad is
    /// shown; it reports every step to the listener it was given, on the main thread.
    /// </summary>
    public interface IAdsProvider
    {
        /// <summary>The name the console and the model show - "AppLovin MAX", "Fake".</summary>
        string Name { get; }

        /// <summary>
        /// Brings the SDK up and reports once through listener.OnInitialized. From then on every
        /// load, show, reward, close and revenue is reported to the same listener. SetConsent may
        /// have been called before this, and the provider honours it: MAX wants consent set
        /// before InitializeSdk.
        /// </summary>
        void Initialize(IAdsProviderListener listener);

        /// <summary>Asks the SDK for one ad of the format. An SDK that loads by itself reports Loaded when it has one.</summary>
        void Load(AdFormat format);

        bool IsReady(AdFormat format);

        /// <summary>Shows the loaded ad. What follows is reported: Displayed or DisplayFailed, then Closed.</summary>
        void Show(AdFormat format, string placement);

        void SetConsent(AdsConsentVO consent);

        void SetMuted(bool muted);

        /// <summary>The SDK's own debug screen - MAX's mediation debugger. A no-op where there is none.</summary>
        void ShowDebugger();
    }
}
