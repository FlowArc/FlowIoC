#if UNITY_EDITOR

using System;
using UnityEditor;
using UnityEditor.Callbacks;

namespace FlowIoC.Editor.ModuleCards
{
    /// <summary>
    /// The two moments a card can go out of date. A compile is when the block's facts change,
    /// because they are read by reflection. An import of a MODULE.md is when the authored half
    /// changes - which happens with no compile at all when a branch that only edits a card's
    /// purpose is pulled, and the directory would otherwise stay stale.
    ///
    /// Both go through ModuleCardsRefresher, which writes only what differs. Writing
    /// unconditionally would re-import the file and bring the postprocessor straight back.
    /// </summary>
    internal class ModuleCardsStartupSync : AssetPostprocessor
    {
        [DidReloadScripts]
        private static void OnScriptsReloaded()
        {
            EditorApplication.delayCall += () => new ModuleCardsRefresher().Run();
        }

        private static void OnPostprocessAllAssets(
            string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            if (!Touches(imported) && !Touches(deleted) && !Touches(moved)) return;

            EditorApplication.delayCall += () => new ModuleCardsRefresher().Run();
        }

        private static bool Touches(string[] paths)
        {
            foreach (string path in paths)
            {
                if (path.EndsWith(ModuleCardFile.FILE_NAME, StringComparison.Ordinal)) return true;
            }

            return false;
        }
    }
}

#endif
