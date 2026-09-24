using System;
using System.Collections.Generic;
using Modules.AudioModule.Enums;
using Modules.AudioModule.Shared;
using Modules.AudioModule.Shared.Data.UnityObjects;
using Modules.AudioModule.Shared.Data.ValueObjects;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Modules.AudioModule.Models
{
    internal interface IAudioBankModel
    {
        IReadOnlyList<CD_AudioBank> Banks { get; }

        /// <summary>Files a bank under its module and indexes its sounds by key. False for a second bank of the same module.</summary>
        bool Register(CD_AudioBank bank);

        bool TryGetBank(string module, out CD_AudioBank bank);

        bool TryGetSound(AudioKey key, out AudioClipCVO sound);

        AudioBankState StateOf(string module);

        void SetState(string module, AudioBankState state);

        /// <summary>The clips a loaded sound plays; empty until its bank is loaded.</summary>
        IReadOnlyList<AudioClip> ClipsOf(AudioKey key);

        void SetClips(AudioKey key, IReadOnlyList<AudioClip> clips);

        void ClearClips(string module);

        void AddHandle(string module, AsyncOperationHandle<AudioClip> handle);

        /// <summary>Hands back the module's load handles and forgets them, for the caller to release.</summary>
        List<AsyncOperationHandle<AudioClip>> TakeHandles(string module);

        void AddWaiter(string module, Action<bool> done);

        /// <summary>Hands back whoever waits on the module's load and forgets them.</summary>
        List<Action<bool>> TakeWaiters(string module);

        /// <summary>True while the sound's minimum interval since its last play has not passed.</summary>
        bool IsCoolingDown(AudioKey key, float now, float interval);

        void MarkPlayed(AudioKey key, float now);

        /// <summary>A variant index out of <paramref name="count"/>, never the one the key played last.</summary>
        int PickVariant(AudioKey key, int count);
    }
}
