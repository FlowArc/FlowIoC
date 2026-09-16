using System;
using Modules.AnalyticsModule.Data.ValueObjects;

namespace Modules.AnalyticsModule.Services
{
    /// <summary>
    /// One analytics SDK behind the service - the socket. A provider is plugged by its own
    /// hosted context, is asked to initialize once the service launches, and from then on
    /// receives every call the service takes. It never decides whether an event is sent; it
    /// sends what it is given, the way its SDK wants it. A game's own plug is one class
    /// implementing this and one <c>[AllowAsSubContext]</c> context that plugs it in Setup.
    /// </summary>
    public interface IAnalyticsProvider
    {
        /// <summary>The name the console and the model show - "Firebase", "Facebook", "Recording".</summary>
        string Name { get; }

        /// <summary>
        /// Brings the SDK up and reports once, on the main thread, whether it is usable. Until the
        /// report the service queues for this provider; a false report drops the queue.
        /// </summary>
        void Initialize(Action<bool> ready);

        void Log(AnalyticsEventVO analyticsEvent);

        /// <summary>An SDK with no notion of user properties implements this as a no-op and logs once that it ignores the call.</summary>
        void SetUserProperty(string name, string value);

        void SetUserId(string userId);

        void SetConsent(AnalyticsConsentVO consent);
    }
}
