using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using Modules.AudioModule.Enums;
using Modules.AudioModule.Models;
using Modules.AudioModule.Shared;
using Modules.AudioModule.Shared.Data.UnityObjects;
using Modules.AudioModule.Shared.Data.ValueObjects;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Modules.AudioModule.Controllers
{
    /// <summary>
    /// Loads every clip of a module's bank through Addressables and answers whoever asked - true
    /// when the bank is ready, false when there is no such bank. A bank already loaded answers at
    /// once; one on its way adds the caller to those waiting. A clip that fails is reported and
    /// left out, and the rest of the bank still loads.
    /// </summary>
    internal class LoadBankCommand : Command
    {
        [SignalParam] private string _module { get; set; }
        [SignalParam] private Action<bool> _done { get; set; }

        [Inject] private IAudioBankModel _banks { get; set; }

        public override async void Execute()
        {
            if (!_banks.TryGetBank(_module, out CD_AudioBank bank))
            {
                FlowLogger.LogWarning($"LoadBank - no bank names the module {_module}. Give the module a Resources/Audio/CD_AudioBank through Tools/FlowIoC-Modules/Audio.");
                _done(false);
                return;
            }

            switch (_banks.StateOf(_module))
            {
                case AudioBankState.Loaded:
                    _done(true);
                    return;
                case AudioBankState.Loading:
                    _banks.AddWaiter(_module, _done);
                    return;
            }

            string module = _module;
            _banks.SetState(module, AudioBankState.Loading);
            _banks.AddWaiter(module, _done);

            Retain();

            try
            {
                foreach (AudioClipCVO sound in bank.Sounds)
                {
                    if (sound != null && !string.IsNullOrEmpty(sound.Key))
                        _banks.SetClips(new AudioKey(sound.Key), await LoadClips(module, sound));
                }
            }
            catch (Exception exception)
            {
                FlowLogger.LogError($"LoadBank - loading the {module} bank threw, so it plays what loaded before the throw: {exception}");
            }

            _banks.SetState(module, AudioBankState.Loaded);

            foreach (Action<bool> waiter in _banks.TakeWaiters(module))
                waiter(true);

            Release();
        }

        private async Task<IReadOnlyList<AudioClip>> LoadClips(string module, AudioClipCVO sound)
        {
            var clips = new List<AudioClip>(sound.Clips.Count);

            foreach (AssetReferenceT<AudioClip> reference in sound.Clips)
            {
                if (reference == null || !reference.RuntimeKeyIsValid())
                {
                    FlowLogger.LogWarning($"LoadBank - {sound.Key} in the {module} bank has an empty clip slot.");
                    continue;
                }

                AsyncOperationHandle<AudioClip> handle = Addressables.LoadAssetAsync<AudioClip>(reference.RuntimeKey);
                _banks.AddHandle(module, handle);

                AudioClip clip = await handle.Task;

                if (handle.Status == AsyncOperationStatus.Succeeded && clip != null)
                    clips.Add(clip);
                else
                    FlowLogger.LogWarning($"LoadBank - a clip of {sound.Key} in the {module} bank did not load: {handle.OperationException?.Message}");
            }

            return clips;
        }
    }
}
