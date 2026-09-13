namespace Modules.HapticModule.Data.ValueObjects
{
    /// <summary>
    /// An amplitude envelope: breakpoints in seconds from the start, each with an amplitude from
    /// 0 to 1. iOS ignores it - a preset there is a system haptic - and Android plays it.
    /// </summary>
    public class HapticEnvelopeVO
    {
        public float[] Time;
        public float[] Amplitude;
    }
}
