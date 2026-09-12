#if UNITY_EDITOR

using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace FlowIoC.Editor.Addressables
{
    /// <summary>
    /// Puts one entry into the project's Addressables groups. Everything decided about the entry
    /// was decided by ScreenAddressableEntries; this only talks to Unity.
    ///
    /// GetSettings(true) creates Assets/AddressableAssetsData when the project has none, which is
    /// the case in a project that has never opened the Addressables window. com.unity.addressables
    /// is a dependency of the package, so it is always there to be asked.
    /// </summary>
    internal class ScreenAddressables
    {
        internal void Register(ScreenAddressableEntry entry)
        {
            if (entry == null || string.IsNullOrEmpty(entry.AssetPath))
                return;

            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.GetSettings(true);

            if (settings == null)
            {
                Debug.LogError("<color=cyan>[FlowIoC]</color> AddressableAssetSettings could not be created, "
                               + $"so '{entry.AssetPath}' was not made addressable.");
                return;
            }

            string guid = AssetDatabase.AssetPathToGUID(entry.AssetPath);

            if (string.IsNullOrEmpty(guid))
            {
                Debug.LogError($"<color=cyan>[FlowIoC]</color> No asset found at '{entry.AssetPath}', "
                               + "so it was not made addressable.");
                return;
            }

            AddressableAssetGroup group = settings.FindGroup(entry.GroupName)
                                          ?? settings.CreateGroup(entry.GroupName, false, false, false,
                                              settings.DefaultGroup.Schemas);

            AddressableAssetEntry created = settings.CreateOrMoveEntry(guid, group, readOnly: false);

            if (created == null)
            {
                Debug.LogError($"<color=cyan>[FlowIoC]</color> '{entry.AssetPath}' could not be added to "
                               + $"'{entry.GroupName}'.");
                return;
            }

            created.SetAddress(entry.Address);

            if (!string.IsNullOrEmpty(entry.Label))
                created.SetLabel(entry.Label, true, true);

            EditorUtility.SetDirty(settings);
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, created, true);
        }

        /// <summary>
        /// Takes a screen back out: the entry first, then the group if that entry was the last
        /// thing in it. Registering creates a group per screen, so deleting the screen and leaving
        /// the group behind is how a project ends up with a list of empty Local_Screen- groups and
        /// their schema assets, none of which anything reads.
        ///
        /// Called before the prefab is deleted, so the entry is found by the asset it still has.
        /// Afterwards there is nothing to look up: Addressables identifies an entry by GUID, and a
        /// deleted asset has none.
        ///
        /// A group that still holds something is left alone. Two screens can share a group when a
        /// project names them so, and the one being deleted does not get to take the other's
        /// registration with it.
        /// </summary>
        internal bool Unregister(ScreenAddressableEntry entry, out string removedGroup)
        {
            removedGroup = null;

            if (entry == null) return false;

            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.GetSettings(false);
            if (settings == null) return false;

            AddressableAssetGroup group = settings.FindGroup(entry.GroupName);
            if (group == null) return false;

            bool removed = RemoveEntry(settings, group, entry);

            if (group.entries.Count == 0)
            {
                removedGroup = group.Name;
                settings.RemoveGroup(group);
                removed = true;
            }

            if (removed) EditorUtility.SetDirty(settings);

            return removed;
        }

        /// <summary>
        /// Re-addresses a screen whose module is being renamed. The entry is found by the prefab's
        /// GUID, so it is found whatever its address is - and left where it is when that address is
        /// not the one the generator gave it, because then somebody chose it and a rename of the
        /// module is not a reason to take that back. Otherwise the address becomes the new name and
        /// the entry moves to the group the new name implies, the old group going when it is empty,
        /// for the same reason Unregister removes it.
        ///
        /// One line comes back for the report, whichever of those happened.
        /// </summary>
        internal string Rename(string prefabAssetPath, ScreenAddressableEntry old, ScreenAddressableEntry updated)
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.GetSettings(false);
            if (settings == null) return "No Addressables settings: the screen's address was not touched.";

            string guid = AssetDatabase.AssetPathToGUID(prefabAssetPath);
            AddressableAssetEntry entry = string.IsNullOrEmpty(guid) ? null : settings.FindAssetEntry(guid);

            if (entry == null) return prefabAssetPath + " is not addressable, so it has no address to rename.";

            if (entry.address != old.Address)
                return "Address '" + entry.address + "' kept: it is not the one the generator gave the screen.";

            entry.SetAddress(updated.Address);

            AddressableAssetGroup from = entry.parentGroup;
            AddressableAssetGroup to = settings.FindGroup(updated.GroupName)
                                       ?? settings.CreateGroup(updated.GroupName, false, false, false, settings.DefaultGroup.Schemas);

            if (from != to)
            {
                settings.MoveEntry(entry, to);

                if (from != null && from.entries.Count == 0 && from.Name == old.GroupName)
                    settings.RemoveGroup(from);
            }

            EditorUtility.SetDirty(settings);
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entry, true);

            return "Addressable " + old.Address + " → " + updated.Address + " in " + to.Name;
        }

        private bool RemoveEntry(
            AddressableAssetSettings settings, AddressableAssetGroup group, ScreenAddressableEntry entry)
        {
            AddressableAssetEntry found = null;

            foreach (AddressableAssetEntry candidate in group.entries)
            {
                if (candidate.address != entry.Address) continue;

                found = candidate;
                break;
            }

            if (found == null) return false;

            settings.RemoveAssetEntry(found.guid, false);

            return true;
        }
    }
}

#endif