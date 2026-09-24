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
    /// <summary>
    /// Every bank the project has, what state each is in, the clips a loaded one holds, and the
    /// two memories a play needs - when a key last played, and which variant it played.
    /// </summary>
    internal class AudioBankModel : IAudioBankModel
    {
        private static readonly IReadOnlyList<AudioClip> NO_CLIPS = Array.Empty<AudioClip>();

        private readonly List<CD_AudioBank> _banks = new();
        private readonly Dictionary<string, CD_AudioBank> _bankByModule = new();
        private readonly Dictionary<AudioKey, AudioClipCVO> _soundByKey = new();
        private readonly Dictionary<string, AudioBankState> _state = new();
        private readonly Dictionary<AudioKey, IReadOnlyList<AudioClip>> _clips = new();
        private readonly Dictionary<string, List<AsyncOperationHandle<AudioClip>>> _handles = new();
        private readonly Dictionary<string, List<Action<bool>>> _waiters = new();
        private readonly Dictionary<AudioKey, float> _lastPlayed = new();
        private readonly Dictionary<AudioKey, int> _lastVariant = new();
        private readonly System.Random _random;

        public AudioBankModel() : this(new System.Random())
        {
        }

        /// <summary>Lets a test fix the dice.</summary>
        internal AudioBankModel(System.Random random)
        {
            _random = random;
        }

        public IReadOnlyList<CD_AudioBank> Banks => _banks;

        public bool Register(CD_AudioBank bank)
        {
            if (bank == null || string.IsNullOrEmpty(bank.Module) || _bankByModule.ContainsKey(bank.Module))
                return false;

            _banks.Add(bank);
            _bankByModule[bank.Module] = bank;
            _state[bank.Module] = AudioBankState.Unloaded;

            foreach (AudioClipCVO sound in bank.Sounds)
            {
                if (sound == null || string.IsNullOrEmpty(sound.Key))
                    continue;

                var key = new AudioKey(sound.Key);
                _soundByKey.TryAdd(key, sound);
            }

            return true;
        }

        public bool TryGetBank(string module, out CD_AudioBank bank)
        {
            bank = null;
            return module != null && _bankByModule.TryGetValue(module, out bank);
        }

        public bool TryGetSound(AudioKey key, out AudioClipCVO sound) => _soundByKey.TryGetValue(key, out sound);

        public AudioBankState StateOf(string module) =>
            module != null && _state.TryGetValue(module, out AudioBankState state) ? state : AudioBankState.Unloaded;

        public void SetState(string module, AudioBankState state) => _state[module] = state;

        public IReadOnlyList<AudioClip> ClipsOf(AudioKey key) =>
            _clips.TryGetValue(key, out IReadOnlyList<AudioClip> clips) ? clips : NO_CLIPS;

        public void SetClips(AudioKey key, IReadOnlyList<AudioClip> clips) => _clips[key] = clips ?? NO_CLIPS;

        public void ClearClips(string module)
        {
            if (!TryGetBank(module, out CD_AudioBank bank))
                return;

            foreach (AudioClipCVO sound in bank.Sounds)
            {
                if (sound != null && !string.IsNullOrEmpty(sound.Key))
                    _clips.Remove(new AudioKey(sound.Key));
            }
        }

        public void AddHandle(string module, AsyncOperationHandle<AudioClip> handle)
        {
            if (!_handles.TryGetValue(module, out List<AsyncOperationHandle<AudioClip>> handles))
                _handles[module] = handles = new List<AsyncOperationHandle<AudioClip>>();

            handles.Add(handle);
        }

        public List<AsyncOperationHandle<AudioClip>> TakeHandles(string module)
        {
            if (!_handles.Remove(module, out List<AsyncOperationHandle<AudioClip>> handles))
                return new List<AsyncOperationHandle<AudioClip>>();

            return handles;
        }

        public void AddWaiter(string module, Action<bool> done)
        {
            if (done == null)
                return;

            if (!_waiters.TryGetValue(module, out List<Action<bool>> waiters))
                _waiters[module] = waiters = new List<Action<bool>>();

            waiters.Add(done);
        }

        public List<Action<bool>> TakeWaiters(string module)
        {
            if (!_waiters.Remove(module, out List<Action<bool>> waiters))
                return new List<Action<bool>>();

            return waiters;
        }

        public bool IsCoolingDown(AudioKey key, float now, float interval) =>
            interval > 0f && _lastPlayed.TryGetValue(key, out float last) && now - last < interval;

        public void MarkPlayed(AudioKey key, float now) => _lastPlayed[key] = now;

        public int PickVariant(AudioKey key, int count)
        {
            if (count <= 1)
                return 0;

            int last = _lastVariant.TryGetValue(key, out int played) ? played : -1;

            // One fewer choice than there are clips, then step over the last one: never the same
            // clip twice in a row, and every other clip equally likely.
            int pick = _random.Next(last >= 0 && last < count ? count - 1 : count);

            if (last >= 0 && last < count && pick >= last)
                pick++;

            _lastVariant[key] = pick;
            return pick;
        }
    }
}
