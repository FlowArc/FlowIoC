using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using Modules.AudioModule.Entities;
using Modules.AudioModule.Enums;
using Modules.AudioModule.Models;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Modules.AudioModule.Controllers
{
    /// <summary>
    /// Stops whatever of the bank is still playing, hands its clips back to Addressables and marks
    /// it unloaded. A bank still loading is left to finish; unload it after.
    /// </summary>
    internal class UnloadBankCommand : Command
    {
        [SignalParam] private string _module { get; set; }

        [Inject] private IAudioBankModel _banks { get; set; }
        [Inject] private IAudioVoiceModel _voices { get; set; }

        public override void Execute()
        {
            AudioBankState state = _banks.StateOf(_module);

            if (state == AudioBankState.Loading)
            {
                FlowLogger.LogWarning($"UnloadBank - the {_module} bank is still loading, so it stays. Unload it once LoadBank has answered.");
                return;
            }

            if (state == AudioBankState.Unloaded)
                return;

            if (_voices.CurrentMusic != null && _voices.CurrentMusic.Key.Bank == _module)
                _voices.ClearMusic();

            foreach (AudioVoice voice in _voices.All)
            {
                if (voice.Key.Bank == _module)
                    voice.Stop();
            }

            foreach (AsyncOperationHandle<AudioClip> handle in _banks.TakeHandles(_module))
            {
                if (handle.IsValid())
                    Addressables.Release(handle);
            }

            _banks.ClearClips(_module);
            _banks.SetState(_module, AudioBankState.Unloaded);
        }
    }
}
