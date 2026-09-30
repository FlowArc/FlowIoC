using System;
using System.Collections.Generic;
using Modules.AudioModule.Data.ValueObjects;
using Modules.AudioModule.Enums;
using Modules.AudioModule.Shared;
using Modules.AudioModule.Shared.Data.UnityObjects;
using Modules.AudioModule.Shared.Data.ValueObjects;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Modules.AudioModule.Models
{
    /// <summary>
    /// Every bank the project has and every sound they list, one record each: a bank's record holds
    /// its asset, the state its load is in, the handles the load took and whoever waits on it; a
    /// sound's record holds its entry, the clips a loaded bank gave it, and the two memories a play
    /// needs - when it last played, and which variant it played.
    /// </summary>
    internal class AudioBankModel : IAudioBankModel
    {
        private readonly List<AudioBankVO> _banks = new();
        private readonly Dictionary<string, AudioBankVO> _banksByModule = new();
        private readonly Dictionary<AudioKey, AudioSoundVO> _sounds = new();
        private readonly System.Random _random;

        public AudioBankModel() : this(new System.Random())
        {
        }

        /// <summary>Lets a test fix the dice.</summary>
        internal AudioBankModel(System.Random random)
        {
            _random = random;
        }

        public IReadOnlyList<AudioBankVO> Banks => _banks;

        public bool Register(CD_AudioBank bank)
        {
            if (bank == null || string.IsNullOrEmpty(bank.Module) || _banksByModule.ContainsKey(bank.Module))
                return false;

            var record = new AudioBankVO {Config = bank};
            _banks.Add(record);
            _banksByModule[bank.Module] = record;

            // The first bank to list a key owns it; a second bank listing the same key is reported
            // by FindBanks, not here.
            foreach (AudioClipCVO sound in bank.Sounds)
            {
                if (sound == null || string.IsNullOrEmpty(sound.Key))
                    continue;

                _sounds.TryAdd(new AudioKey(sound.Key), new AudioSoundVO {Config = sound});
            }

            return true;
        }

        public bool TryGetBank(string module, out CD_AudioBank bank)
        {
            bank = TryGetRecord(module, out AudioBankVO record) ? record.Config : null;
            return bank != null;
        }

        public bool TryGetSound(AudioKey key, out AudioClipCVO sound)
        {
            sound = _sounds.TryGetValue(key, out AudioSoundVO record) ? record.Config : null;
            return sound != null;
        }

        public AudioBankState StateOf(string module) =>
            TryGetRecord(module, out AudioBankVO record) ? record.State : AudioBankState.Unloaded;

        public void SetState(string module, AudioBankState state)
        {
            if (TryGetRecord(module, out AudioBankVO record))
                record.State = state;
        }

        public IReadOnlyList<AudioClip> ClipsOf(AudioKey key) =>
            _sounds.TryGetValue(key, out AudioSoundVO record) ? record.Clips : AudioSoundVO.NO_CLIPS;

        public void SetClips(AudioKey key, IReadOnlyList<AudioClip> clips)
        {
            if (_sounds.TryGetValue(key, out AudioSoundVO record))
                record.Clips = clips ?? AudioSoundVO.NO_CLIPS;
        }

        /// <summary>
        /// Empties the clips of every key the bank lists. The play memories stay, so a sound played
        /// just before an unload still waits out its interval after the next load.
        /// </summary>
        public void ClearClips(string module)
        {
            if (!TryGetRecord(module, out AudioBankVO bank))
                return;

            foreach (AudioClipCVO sound in bank.Config.Sounds)
            {
                if (sound != null && !string.IsNullOrEmpty(sound.Key) && _sounds.TryGetValue(new AudioKey(sound.Key), out AudioSoundVO record))
                    record.Clips = AudioSoundVO.NO_CLIPS;
            }
        }

        public void AddHandle(string module, AsyncOperationHandle<AudioClip> handle)
        {
            if (TryGetRecord(module, out AudioBankVO record))
                record.Handles.Add(handle);
        }

        public List<AsyncOperationHandle<AudioClip>> TakeHandles(string module)
        {
            var handles = new List<AsyncOperationHandle<AudioClip>>();

            if (!TryGetRecord(module, out AudioBankVO record))
                return handles;

            handles.AddRange(record.Handles);
            record.Handles.Clear();
            return handles;
        }

        public void AddWaiter(string module, Action<bool> done)
        {
            if (done != null && TryGetRecord(module, out AudioBankVO record))
                record.Waiters.Add(done);
        }

        public List<Action<bool>> TakeWaiters(string module)
        {
            var waiters = new List<Action<bool>>();

            if (!TryGetRecord(module, out AudioBankVO record))
                return waiters;

            waiters.AddRange(record.Waiters);
            record.Waiters.Clear();
            return waiters;
        }

        public bool IsCoolingDown(AudioKey key, float now, float interval) =>
            interval > 0f && _sounds.TryGetValue(key, out AudioSoundVO record) && record.HasPlayed && now - record.LastPlayed < interval;

        public void MarkPlayed(AudioKey key, float now)
        {
            if (!_sounds.TryGetValue(key, out AudioSoundVO record))
                return;

            record.LastPlayed = now;
            record.HasPlayed = true;
        }

        public int PickVariant(AudioKey key, int count)
        {
            if (count <= 1)
                return 0;

            _sounds.TryGetValue(key, out AudioSoundVO record);
            int last = record?.LastVariant ?? -1;

            // One fewer choice than there are clips, then step over the last one: never the same
            // clip twice in a row, and every other clip equally likely.
            int pick = _random.Next(last >= 0 && last < count ? count - 1 : count);

            if (last >= 0 && last < count && pick >= last)
                pick++;

            if (record != null)
                record.LastVariant = pick;

            return pick;
        }

        private bool TryGetRecord(string module, out AudioBankVO record)
        {
            record = null;
            return module != null && _banksByModule.TryGetValue(module, out record);
        }
    }
}