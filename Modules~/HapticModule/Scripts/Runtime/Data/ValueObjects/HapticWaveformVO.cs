namespace Modules.HapticModule.Data.ValueObjects
{
    /// <summary>
    /// What Android's VibrationEffect.createWaveform takes: each entry vibrates for its duration
    /// in milliseconds at its amplitude from 0 to 255.
    /// </summary>
    public class HapticWaveformVO
    {
        public long[] Timings;
        public int[] Amplitudes;
    }
}
