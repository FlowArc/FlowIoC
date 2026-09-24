#if UNITY_EDITOR

using System.Collections.Generic;
using Modules.AudioModule.Shared.Data.UnityObjects;
using Modules.AudioModule.Shared.Data.ValueObjects;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Modules.AudioModule.Editor
{
    /// <summary>
    /// Makes every clip a bank names addressable, in a group called Audio, whenever a bank is
    /// imported - saved after an edit, or arriving with a module that was installed. A clip picked
    /// in the Inspector is made addressable by Addressables itself; a bank that came in a module
    /// brings clips no Addressables group has heard of, and without an entry they would not load.
    /// A clip already addressable, in any group, is left where it is.
    /// </summary>
    internal class AudioBankAddressables : AssetPostprocessor
    {
        internal const string GROUP_NAME = "Audio";

        private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            var banks = new List<string>();

            foreach (string path in imported)
            {
                if (path.EndsWith(".asset") && AssetDatabase.GetMainAssetTypeAtPath(path) == typeof(CD_AudioBank))
                    banks.Add(path);
            }

            if (banks.Count > 0)
                EditorApplication.delayCall += () => new AudioBankAddressables().Register(banks);
        }

        /// <summary>Gives each named bank's clips an entry. Answers how many entries it made.</summary>
        internal int Register(IEnumerable<string> bankPaths)
        {
            var guids = new List<string>();

            foreach (string path in bankPaths)
            {
                var bank = AssetDatabase.LoadAssetAtPath<CD_AudioBank>(path);

                if (bank == null)
                    continue;

                foreach (AudioClipCVO sound in bank.Sounds)
                {
                    if (sound == null)
                        continue;

                    foreach (AssetReferenceT<AudioClip> clip in sound.Clips)
                    {
                        if (clip != null && !string.IsNullOrEmpty(clip.AssetGUID))
                            guids.Add(clip.AssetGUID);
                    }
                }
            }

            if (guids.Count == 0)
                return 0;

            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.GetSettings(true);

            if (settings == null)
            {
                Debug.LogError("AudioBankAddressables - AddressableAssetSettings could not be created, so the banks' clips were not made addressable and will not load.");
                return 0;
            }

            AddressableAssetGroup group = null;
            int made = 0;

            foreach (string guid in guids)
            {
                if (settings.FindAssetEntry(guid) != null || string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(guid)))
                    continue;

                group ??= settings.FindGroup(GROUP_NAME)
                          ?? settings.CreateGroup(GROUP_NAME, false, false, true, settings.DefaultGroup.Schemas);

                AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, group, readOnly: false);

                if (entry != null)
                    made++;
            }

            if (made > 0)
            {
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssets();
            }

            return made;
        }
    }
}

#endif
