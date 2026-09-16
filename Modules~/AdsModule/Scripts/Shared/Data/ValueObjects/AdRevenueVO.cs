using System.Globalization;
using Modules.AdsModule.Shared.Enums;

namespace Modules.AdsModule.Shared.Data.ValueObjects
{
    /// <summary>
    /// What one impression paid, as the mediation reported it. Announced on Outgoing.RevenuePaid;
    /// the game's analytics Command builds its ad_impression event from it.
    /// </summary>
    public readonly struct AdRevenueVO
    {
        public readonly string Provider;
        public readonly AdFormat Format;
        public readonly string Placement;
        public readonly string Network;
        public readonly string UnitId;
        public readonly double Revenue;
        public readonly string Currency;
        public readonly string Precision;

        public AdRevenueVO(string provider, AdFormat format, string placement, string network, string unitId,
            double revenue, string currency, string precision)
        {
            Provider = provider ?? string.Empty;
            Format = format;
            Placement = placement ?? string.Empty;
            Network = network ?? string.Empty;
            UnitId = unitId ?? string.Empty;
            Revenue = revenue;
            Currency = currency ?? string.Empty;
            Precision = precision ?? string.Empty;
        }

        public override string ToString() =>
            $"{Network} {Revenue.ToString(CultureInfo.InvariantCulture)} {Currency} ({Format}/{Placement})";
    }
}
