#if UNITY_EDITOR
using FlowIoC.BaseModule.Signals;
using Modules.AudioModule.AudioTestModule.Data.ValueObjects;
using UnityEngine;

namespace Modules.AudioModule.AudioTestModule.Signals
{
    /// <summary>
    /// What the test screen asks for, one signal a button, so each flow in AudioTestContext reads
    /// as the steps a game would bind for the same thing.
    /// </summary>
    internal class AudioTestInternalSignals : ISignalHolder
    {
        public Signal Start = new();
        public Signal Beep = new();
        public Signal<Vector3> Boom = new();
        public Signal ThemeA = new();
        public Signal ThemeB = new();
        public Signal StopMusic = new();
        public Signal Duck = new();
        public Signal Normal = new();
        public Signal Unload = new();
        public Signal<bool> MusicEnabled = new();
        public Signal<bool> SfxEnabled = new();
        public Signal<float> MusicVolume = new();
        public Signal<float> SfxVolume = new();
        public Signal<bool> Muted = new();
        public Signal ReportState = new();
        public Signal<AudioTestStateVO> StateChanged = new();
    }
}
#endif
