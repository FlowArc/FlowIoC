using System;
using Modules.DeviceDebuggerModule.Constants;
using Modules.DeviceDebuggerModule.Data.ValueObjects;

namespace Modules.DeviceDebuggerModule.Entities
{
    /// <summary>
    /// Frame times over a window, ticked by the view each frame while the panel is open. The
    /// frame rate is counted over the last second of samples rather than the whole window, so a
    /// hitch two seconds ago does not drag the number; the worst frame is over the whole window,
    /// so it is seen. Memory and the rest are read from probes handed in, which keeps the sampler
    /// out of UnityEngine and inside a plain test.
    /// </summary>
    public class StatsSampler
    {
        private readonly float[] _frameSeconds;
        private int _next;
        private int _count;

        public StatsSampler(int window = DeviceDebuggerConstants.STATS_WINDOW)
        {
            _frameSeconds = new float[window > 0 ? window : 1];
        }

        public float FrameMs { get; private set; }

        public float Fps { get; private set; }

        public float WorstFrameMs { get; private set; }

        public void Tick(float unscaledDeltaTime)
        {
            if (unscaledDeltaTime <= 0f) return;

            _frameSeconds[_next] = unscaledDeltaTime;
            _next = (_next + 1) % _frameSeconds.Length;
            if (_count < _frameSeconds.Length) _count++;

            FrameMs = unscaledDeltaTime * 1000f;
            Fps = AverageFpsOverLastSecond();
            WorstFrameMs = Worst() * 1000f;
        }

        public StatsSampleVO Sample(Func<long> allocated, Func<long> reserved, Func<long> monoHeap, Func<int> gcCount,
            float uptime, float timeScale)
        {
            var history = new float[_count];

            for (int i = 0; i < _count; i++)
                history[i] = At(i) * 1000f;

            return new StatsSampleVO
            {
                Fps = Fps,
                FrameMs = FrameMs,
                WorstFrameMs = WorstFrameMs,
                AllocatedBytes = allocated?.Invoke() ?? 0,
                ReservedBytes = reserved?.Invoke() ?? 0,
                MonoHeapBytes = monoHeap?.Invoke() ?? 0,
                GcCount = gcCount?.Invoke() ?? 0,
                Uptime = uptime,
                TimeScale = timeScale,
                FrameHistory = history
            };
        }

        /// <summary>Oldest first.</summary>
        private float At(int index)
        {
            int oldest = (_next - _count + _frameSeconds.Length) % _frameSeconds.Length;

            return _frameSeconds[(oldest + index) % _frameSeconds.Length];
        }

        private float AverageFpsOverLastSecond()
        {
            float seconds = 0f;
            int frames = 0;

            for (int i = _count - 1; i >= 0; i--)
            {
                float frame = At(i);

                // A frame that would carry the sum past the second stays out, so sixty even
                // frames read as sixty and not as sixty-one over a second and a tenth. The
                // newest frame is always in, or a two-second hitch would read as no rate at all.
                if (frames > 0 && seconds + frame > 1f) break;

                seconds += frame;
                frames++;
            }

            return seconds > 0f ? frames / seconds : 0f;
        }

        private float Worst()
        {
            float worst = 0f;

            for (int i = 0; i < _count; i++)
                if (At(i) > worst) worst = At(i);

            return worst;
        }
    }
}
