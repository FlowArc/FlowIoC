using System.Globalization;

namespace Modules.AdsModule.Data.ValueObjects
{
    /// <summary>
    /// What the network says the reward is. A game normally grants its own reward by placement
    /// and ignores this; it is carried for the ones that read it.
    /// </summary>
    public readonly struct AdRewardVO
    {
        public static readonly AdRewardVO None = new(string.Empty, 0);

        public readonly string Label;
        public readonly double Amount;

        public AdRewardVO(string label, double amount)
        {
            Label = label ?? string.Empty;
            Amount = amount;
        }

        public bool IsNone => string.IsNullOrEmpty(Label) && Amount == 0;

        public override string ToString() => IsNone ? "none" : $"{Label} x{Amount.ToString(CultureInfo.InvariantCulture)}";
    }
}
