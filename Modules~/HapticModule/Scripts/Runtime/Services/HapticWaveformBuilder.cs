using System;
using System.Collections.Generic;
using Modules.HapticModule.Data.ValueObjects;
using UnityEngine;

namespace Modules.HapticModule.Services
{
    /// <summary>
    /// Turns an envelope into what Android's Vibrator plays. Build() reproduces the Lofelt core's
    /// conversion for a device with amplitude control - interpolate each pair of breakpoints at
    /// 25 ms steps, drop the interior points that round to the same 1/256 amplitude bin, then make
    /// one entry per pair of points: the duration in whole milliseconds with the rounding error
    /// carried forward, and the amplitude of the pair's first point on 0..255. The last breakpoint
    /// starts no entry; it is where the clip ends. BuildOnOff() is the fallback for a device
    /// without amplitude control: the breakpoint distances as an off/on pattern at full strength.
    ///
    /// The Lofelt core does this arithmetic in f32, and the amplitude a point lands on can turn on
    /// the last bit - 0.3333334 × 255 is 85, 0.33333333 × 255 is 84. The runtime is free to carry a
    /// float expression in double precision, so every intermediate here is narrowed with an explicit
    /// (float) cast, which is what keeps the output the core's.
    /// </summary>
    public class HapticWaveformBuilder
    {
        private const float MIN_TIME_STEP = 0.025f;
        private const float SAMPLING_FREQUENCY = 40f;
        private const int QUANTIZATION_DEPTH = 256;
        private const int MAX_AMPLITUDE = 255;

        public HapticWaveformVO Build(HapticEnvelopeVO envelope)
        {
            List<(float time, float amplitude)> points = Interpolate(envelope);
            var timings = new List<long>();
            var amplitudes = new List<int>();
            float accumulatedMs = 0f;

            for (int i = 0; i + 1 < points.Count; i++)
            {
                (float timeA, float amplitudeA) = points[i];
                float duration = (float) (points[i + 1].time - timeA);

                if (duration <= 0f) continue;

                float accumulatedSeconds = (float) (accumulatedMs / 1000f);
                float timingErrorMs = (float) ((float) (timeA - accumulatedSeconds) * 1000f);
                float durationWithError = (float) ((float) (duration * 1000f) + timingErrorMs);
                long durationMs = (long) MathF.Round(durationWithError, MidpointRounding.AwayFromZero);

                if (durationMs <= 0) continue;

                timings.Add(durationMs);
                accumulatedMs = (float) (accumulatedMs + durationMs);
                amplitudes.Add((int) (float) (amplitudeA * MAX_AMPLITUDE));
            }

            return new HapticWaveformVO {Timings = timings.ToArray(), Amplitudes = amplitudes.ToArray()};
        }

        public long[] BuildOnOff(HapticEnvelopeVO envelope)
        {
            var timings = new long[envelope.Time.Length];

            timings[0] = 0;

            for (int i = 1; i < envelope.Time.Length; i++)
            {
                float distance = (float) (envelope.Time[i] - envelope.Time[i - 1]);
                timings[i] = (long) (float) (distance * 1000f);
            }

            return timings;
        }

        private List<(float time, float amplitude)> Interpolate(HapticEnvelopeVO envelope)
        {
            var points = new List<(float, float)>();

            for (int i = 1; i < envelope.Time.Length; i++)
            {
                float timeA = envelope.Time[i - 1];
                float timeB = envelope.Time[i];
                float amplitudeA = envelope.Amplitude[i - 1];
                float amplitudeB = envelope.Amplitude[i];
                float interval = (float) (timeB - timeA);
                int totalPoints = (int) (float) ((float) (SAMPLING_FREQUENCY * interval) + 1f);
                var segmentTimes = new List<float>();
                var segmentAmplitudes = new List<float>();

                if (interval > MIN_TIME_STEP && totalPoints >= 3)
                {
                    float step = (float) (interval / (float) (totalPoints - 1));
                    float amplitudeDiff = (float) (amplitudeB - amplitudeA);

                    for (int p = 0; p < totalPoints; p++)
                    {
                        float time = (float) (timeA + (float) (step * p));
                        float clamped = Mathf.Clamp(time, timeA, timeB);
                        float factor = (float) ((float) (clamped - timeA) / interval);

                        segmentTimes.Add(time);
                        segmentAmplitudes.Add((float) (amplitudeA + (float) (amplitudeDiff * factor)));
                    }
                }
                else
                {
                    segmentTimes.Add(timeA);
                    segmentTimes.Add(timeB);
                    segmentAmplitudes.Add(amplitudeA);
                    segmentAmplitudes.Add(amplitudeB);
                }

                points.AddRange(RemoveRedundant(segmentTimes, segmentAmplitudes));
            }

            return points;
        }

        /// <summary>
        /// Drops the points a player with 256 amplitude steps could not tell from the one before.
        /// A segment's first and last points always stay.
        /// </summary>
        private List<(float, float)> RemoveRedundant(List<float> times, List<float> amplitudes)
        {
            var kept = new List<(float, float)>();
            float first = times[0];
            float last = times[times.Count - 1];
            float currentBin = 0f;

            for (int i = 0; i < times.Count; i++)
            {
                float scaled = (float) (amplitudes[i] * QUANTIZATION_DEPTH);
                float quantized = (float) (MathF.Round(scaled, MidpointRounding.AwayFromZero) / QUANTIZATION_DEPTH);
                bool interior = Math.Abs(times[i] - first) > float.Epsilon && Math.Abs(times[i] - last) > float.Epsilon;

                if (interior && Math.Abs(quantized - currentBin) < float.Epsilon) continue;

                kept.Add((times[i], amplitudes[i]));
                currentBin = quantized;
            }

            return kept;
        }
    }
}