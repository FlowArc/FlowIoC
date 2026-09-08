#if UNITY_EDITOR
using System.Globalization;

namespace FlowIoC.Editor.Console
{
    /// <summary>
    /// What a row says instead of the clock when Timing is on: which frame the log was written in,
    /// and how long after the row above it arrived. A wall clock answers when something happened;
    /// these two answer whether two things happened together, which is the question a flow raises.
    /// </summary>
    public class FlowConsoleTiming
    {
        public string Prefix(int frame, float realtime, float previousRealtime, bool hasPrevious)
        {
            string frameText = "f" + frame.ToString(CultureInfo.InvariantCulture);

            if (!hasPrevious) return frameText;

            float seconds = realtime - previousRealtime;

            // Time starts again when the domain reloads, so a restored log can sit later in the
            // list and earlier on the clock. A negative gap is not a measurement.
            if (seconds < 0f) return frameText;

            // Rounded rather than truncated: a gap stored as 0.011999846 of a second is twelve
            // milliseconds, and reporting eleven would be wrong every time.
            int milliseconds = (int) System.Math.Round(seconds * 1000f);

            if (milliseconds >= 1000)
                return frameText + "  +" + seconds.ToString("0.00", CultureInfo.InvariantCulture) + "s";

            return frameText + "  +" + milliseconds.ToString(CultureInfo.InvariantCulture) + "ms";
        }
    }
}
#endif
