using System.Collections.Generic;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Function.Provider;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using Modules.AudioModule.Constants;
using Modules.AudioModule.Models;
using Modules.AudioModule.Shared;
using Modules.AudioModule.Shared.Data.UnityObjects;
using Modules.AudioModule.Shared.Data.ValueObjects;
using UnityEngine;

namespace Modules.AudioModule.Controllers
{
    /// <summary>
    /// Finds every bank in the project - each module's Resources/Audio/CD_AudioBank - and files
    /// it. Outside a release build it then holds the banks against the keys the code declares, so
    /// a key with no row, a row with no key and a row with no clip are reported at start rather
    /// than as a silence somebody has to notice.
    /// </summary>
    internal class FindBanksCommand : Command
    {
        [Inject] private IAudioBankModel _banks { get; set; }
        [Inject] private IFunctionProvider _functions { get; set; }

        public override void Execute()
        {
            foreach (CD_AudioBank bank in Resources.LoadAll<CD_AudioBank>(AudioConstants.BANK_RESOURCES_FOLDER))
            {
                if (string.IsNullOrEmpty(bank.Module))
                    FlowLogger.LogWarning($"FindBanks - the bank {bank.name} names no module, so none of its sounds can play. Set Module to the module it sits in.");
                else if (!_banks.Register(bank))
                    FlowLogger.LogWarning($"FindBanks - a second bank names the module {bank.Module}; only the first one found is used.");
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Check();
#endif
        }

        private void Check()
        {
            var rows = new HashSet<AudioKey>();

            foreach (CD_AudioBank bank in _banks.Banks)
            {
                foreach (AudioClipCVO sound in bank.Sounds)
                {
                    if (sound == null || string.IsNullOrEmpty(sound.Key))
                    {
                        FlowLogger.LogWarning($"FindBanks - the {bank.Module} bank has a row with no key.");
                        continue;
                    }

                    var key = new AudioKey(sound.Key);

                    if (!rows.Add(key))
                        FlowLogger.LogWarning($"FindBanks - {key} has two rows; the first one plays.");

                    if (key.Bank != bank.Module)
                        FlowLogger.LogWarning($"FindBanks - {key} sits in the {bank.Module} bank but its key names {key.Bank}. It loads with the wrong bank; move the row.");

                    if (sound.Clips.Count == 0)
                        FlowLogger.LogWarning($"FindBanks - {key} has no clip in the {bank.Module} bank.");
                }
            }

            IReadOnlyList<AudioKey> declared = _functions.Call<ListDeclaredKeysFunction>().ExecuteAndGetResult<IReadOnlyList<AudioKey>>();
            var declaredSet = new HashSet<AudioKey>(declared);

            foreach (AudioKey key in declared)
            {
                if (!rows.Contains(key))
                    FlowLogger.LogWarning($"FindBanks - {key} is declared in AudioKey but no bank has a row for it, so it plays nothing.");
            }

            foreach (AudioKey key in rows)
            {
                if (!declaredSet.Contains(key))
                    FlowLogger.LogWarning($"FindBanks - the {key.Bank} bank has a row for {key}, which no AudioKey declares. Nothing can ask for it.");
            }
        }
    }
}
