using System;

namespace Modules.DeviceDebuggerModule.Data.ValueObjects
{
    /// <summary>One reading of the Stats tab, taken by the sampler when the tab asks.</summary>
    public class StatsSampleVO
    {
        public float Fps;
        public float FrameMs;
        public float WorstFrameMs;
        public long AllocatedBytes;
        public long ReservedBytes;
        public long MonoHeapBytes;
        public int GcCount;
        public float Uptime;
        public float TimeScale;

        /// <summary>Frame times in milliseconds, oldest first.</summary>
        public float[] FrameHistory = Array.Empty<float>();
    }
}
