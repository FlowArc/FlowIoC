using System.Collections.Generic;
using Modules.HapticModule.Data.ValueObjects;
using Modules.HapticModule.Enums;

namespace Modules.HapticModule.Services
{
    /// <summary>
    /// Lofelt's nine preset envelopes (Nice Vibrations 4.1.2, HapticPatterns.cs, MIT), with one
    /// cell changed: Medium runs 120 ms, not Lofelt's 80. They are what Android plays; iOS plays
    /// the system haptic of the same name and never reads them. In code rather than an asset
    /// because only one platform could be tuned by an asset.
    /// </summary>
    public class HapticPresetEnvelopes
    {
        private readonly Dictionary<HapticPreset, HapticEnvelopeVO> _envelopes = new();

        public HapticPresetEnvelopes()
        {
            Add(HapticPreset.Selection, new[] {0f, 0.04f}, new[] {0.471f, 0.471f});
            Add(HapticPreset.LightImpact, new[] {0f, 0.04f}, new[] {0.156f, 0.156f});
            // Lofelt's 80 ms. An OEM firmware (OnePlus CPH2747, Android 16) replaces every waveform
            // step of about 100 ms or under with its own 45 ms click, scaling only the strength, so
            // at 80 ms Medium was the same click as Selection. 120 ms plays as a real waveform there,
            // and 120 rather than 110 keeps a margin from that firmware's threshold. Felt on the
            // device: distinct from Light and from the 160 ms Heavy.
            Add(HapticPreset.MediumImpact, new[] {0f, 0.12f}, new[] {0.471f, 0.471f});
            Add(HapticPreset.HeavyImpact, new[] {0f, 0.16f}, new[] {1f, 1f});
            Add(HapticPreset.RigidImpact, new[] {0f, 0.04f}, new[] {1f, 1f});
            Add(HapticPreset.SoftImpact, new[] {0f, 0.16f}, new[] {0.156f, 0.156f});
            Add(HapticPreset.Success, new[] {0f, 0.04f, 0.08f, 0.24f}, new[] {0f, 0.157f, 0f, 1f});
            Add(HapticPreset.Failure,
                new[] {0f, 0.08f, 0.12f, 0.2f, 0.24f, 0.4f, 0.44f, 0.48f},
                new[] {0f, 0.47f, 0f, 0.47f, 0f, 1f, 0f, 0.157f});
            Add(HapticPreset.Warning, new[] {0f, 0.12f, 0.24f, 0.28f}, new[] {0f, 1f, 0f, 0.47f});
        }

        /// <summary>The envelope of a preset, or null for None.</summary>
        public HapticEnvelopeVO Get(HapticPreset preset) =>
            _envelopes.TryGetValue(preset, out HapticEnvelopeVO envelope) ? envelope : null;

        private void Add(HapticPreset preset, float[] time, float[] amplitude) =>
            _envelopes[preset] = new HapticEnvelopeVO {Time = time, Amplitude = amplitude};
    }
}