using System;
using FlowIoC.BaseModule.Signals;
using Modules.AudioModule.Shared;
using UnityEngine;

namespace Modules.AudioModule.Signals
{
    /// <summary>
    /// What the service says to its own commands. None of these leave the module, so they sit apart
    /// from the public holder rather than widening what the module offers.
    ///
    /// There is no Incoming and no Outgoing here. Those two halves say what a module accepts and
    /// what it announces across a boundary, and an internal signal never crosses one.
    /// </summary>
    internal class AudioInternalSignals : ISignalHolder
    {
        public Signal Initialize = new();
        public Signal<AudioKey> Play = new();
        public Signal<AudioKey, Vector3> PlayAt = new();
        public Signal<AudioKey> PlayMusic = new();
        public Signal StopMusic = new();
        public Signal<string, Action<bool>> LoadBank = new();
        public Signal<string> UnloadBank = new();
        public Signal<AudioSnapshot> ApplySnapshot = new();
    }
}
