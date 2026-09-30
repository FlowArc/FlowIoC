using System;
using System.Collections.Generic;
using Modules.AudioModule.Enums;
using Modules.AudioModule.Shared.Data.UnityObjects;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Modules.AudioModule.Data.ValueObjects
{
    /// <summary>
    /// One bank, held whole by AudioBankModel: the asset itself, the state its load is in, the
    /// handles that load took and whoever waits on it. It holds a config side and a runtime side
    /// together, which is why it is a plain VO.
    /// </summary>
    internal class AudioBankVO
    {
        public CD_AudioBank Config;

        public AudioBankState State = AudioBankState.Unloaded;

        /// <summary>The Addressables handles the load took, released when the bank is unloaded.</summary>
        public List<AsyncOperationHandle<AudioClip>> Handles = new();

        /// <summary>Whoever asked for the bank while it was loading, answered once it is loaded.</summary>
        public List<Action<bool>> Waiters = new();
    }
}
