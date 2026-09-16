using System.Collections.Generic;
using Modules.AnalyticsModule.Data.ValueObjects;
using Modules.AnalyticsModule.Services;

namespace Modules.AnalyticsModule.Models
{
    /// <summary>
    /// The module's state: a slot per plugged provider, whether the service has launched, and
    /// the current user id, consent and user properties - current values, not per-slot pending
    /// lists, so a slot that becomes ready receives whatever is current and a slot already ready
    /// receives each change as it comes. Only events are queued per slot, because only events
    /// must not be duplicated or reordered.
    /// </summary>
    public interface IAnalyticsModel
    {
        IReadOnlyList<ProviderSlotVO> Slots { get; }

        bool IsLaunched { get; }

        string UserId { get; }

        AnalyticsConsentVO? Consent { get; }

        IReadOnlyDictionary<string, string> UserProperties { get; }

        bool TryGetSlot(IAnalyticsProvider provider, out ProviderSlotVO slot);

        ProviderSlotVO Plug(IAnalyticsProvider provider);

        bool Unplug(IAnalyticsProvider provider);

        void MarkLaunched();

        void SetUserId(string userId);

        void SetConsent(AnalyticsConsentVO consent);

        void SetUserProperty(string name, string value);
    }
}
