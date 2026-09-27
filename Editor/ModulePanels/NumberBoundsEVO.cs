#if UNITY_EDITOR

namespace FlowIoC.Editor.ModulePanels
{
    /// <summary>
    /// The range a dragged number is kept inside, read off the field's own [Min] or [Range]. An
    /// end the field does not declare is open.
    /// </summary>
    internal class NumberBoundsEVO
    {
        internal static readonly NumberBoundsEVO Open = new NumberBoundsEVO(null, null);

        internal NumberBoundsEVO(double? min, double? max)
        {
            Min = min;
            Max = max;
        }

        internal double? Min { get; }

        internal double? Max { get; }

        internal double Clamp(double value)
        {
            if (Min.HasValue && value < Min.Value)
                return Min.Value;

            if (Max.HasValue && value > Max.Value)
                return Max.Value;

            return value;
        }
    }
}

#endif
