using System.Collections.Generic;
using Modules.AnalyticsModule.Data.ValueObjects;
using Modules.AnalyticsModule.Services;

namespace Modules.AnalyticsModule.Models
{
    internal class AnalyticsModel : IAnalyticsModel
    {
        private readonly List<ProviderSlotVO> _slots = new();
        private readonly Dictionary<string, string> _userProperties = new();

        public IReadOnlyList<ProviderSlotVO> Slots => _slots;

        public bool IsLaunched { get; private set; }

        public string UserId { get; private set; }

        public AnalyticsConsentVO? Consent { get; private set; }

        public IReadOnlyDictionary<string, string> UserProperties => _userProperties;

        public bool TryGetSlot(IAnalyticsProvider provider, out ProviderSlotVO slot)
        {
            foreach (ProviderSlotVO candidate in _slots)
            {
                if (!ReferenceEquals(candidate.Provider, provider)) continue;

                slot = candidate;
                return true;
            }

            slot = null;
            return false;
        }

        public ProviderSlotVO Plug(IAnalyticsProvider provider)
        {
            var slot = new ProviderSlotVO(provider);
            _slots.Add(slot);
            return slot;
        }

        public bool Unplug(IAnalyticsProvider provider)
        {
            if (!TryGetSlot(provider, out ProviderSlotVO slot))
                return false;

            _slots.Remove(slot);
            return true;
        }

        public void MarkLaunched() => IsLaunched = true;

        public void SetUserId(string userId) => UserId = userId;

        public void SetConsent(AnalyticsConsentVO consent) => Consent = consent;

        public void SetUserProperty(string name, string value) => _userProperties[name] = value;
    }
}
