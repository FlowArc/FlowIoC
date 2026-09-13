#if UNITY_ANDROID && !UNITY_EDITOR
using System;
using FlowIoC.ConsoleModule;
using Modules.HapticModule.Data.ValueObjects;
using Modules.HapticModule.Enums;
using UnityEngine;

namespace Modules.HapticModule.Services
{
    /// <summary>
    /// Android's side of a preset: the Vibrator through JNI, no native library. A device with
    /// amplitude control plays the envelope as a waveform; one without plays the breakpoint times
    /// as an off/on pattern at full strength; one with no vibrator plays nothing. Android stops
    /// whatever is playing when a new effect starts, so a play never queues behind another.
    /// </summary>
    public class AndroidHapticPlayer : IHapticPlayer
    {
        private const int API_VIBRATION_EFFECT = 26;
        private const int API_VIBRATOR_MANAGER = 31;

        private readonly HapticPresetEnvelopes _envelopes = new();
        private readonly HapticWaveformBuilder _builder = new();

        private AndroidJavaObject _vibrator;
        private AndroidJavaClass _vibrationEffect;
        private int _sdk;
        private bool _hasVibrator;
        private bool _hasAmplitudeControl;

        public void Initialize()
        {
            try
            {
                using var version = new AndroidJavaClass("android.os.Build$VERSION");
                _sdk = version.GetStatic<int>("SDK_INT");

                using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

                if (_sdk >= API_VIBRATOR_MANAGER)
                {
                    using var manager = activity.Call<AndroidJavaObject>("getSystemService", "vibrator_manager");
                    _vibrator = manager?.Call<AndroidJavaObject>("getDefaultVibrator");
                }
                else
                {
                    _vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                }

                _hasVibrator = _vibrator != null && _vibrator.Call<bool>("hasVibrator");
                _hasAmplitudeControl = _hasVibrator && _sdk >= API_VIBRATION_EFFECT && _vibrator.Call<bool>("hasAmplitudeControl");

                if (_sdk >= API_VIBRATION_EFFECT)
                    _vibrationEffect = new AndroidJavaClass("android.os.VibrationEffect");
            }
            catch (Exception exception)
            {
                _hasVibrator = false;
                FlowLogger.LogError(FlowModule.HapticModule,
                    $"Initialize - AndroidHapticPlayer | the Vibrator service could not be reached, so nothing will vibrate: {exception.Message}");
            }

            FlowLogger.Log(FlowModule.HapticModule,
                $"Initialize - AndroidHapticPlayer | sdk={_sdk} vibrator={_hasVibrator} amplitudeControl={_hasAmplitudeControl}");
        }

        public void Play(HapticPreset preset)
        {
            if (!_hasVibrator)
                return;

            HapticEnvelopeVO envelope = _envelopes.Get(preset);

            if (envelope == null)
                return;

            if (_hasAmplitudeControl)
            {
                HapticWaveformVO waveform = _builder.Build(envelope);

                using var effect = _vibrationEffect.CallStatic<AndroidJavaObject>("createWaveform", waveform.Timings, waveform.Amplitudes, -1);
                _vibrator.Call("vibrate", effect);
                return;
            }

            long[] pattern = _builder.BuildOnOff(envelope);

            if (_sdk >= API_VIBRATION_EFFECT)
            {
                using var effect = _vibrationEffect.CallStatic<AndroidJavaObject>("createWaveform", pattern, -1);
                _vibrator.Call("vibrate", effect);
            }
            else
            {
                _vibrator.Call("vibrate", pattern, -1);
            }
        }

        public void Stop()
        {
            if (_hasVibrator)
                _vibrator.Call("cancel");
        }
    }
}
#endif
