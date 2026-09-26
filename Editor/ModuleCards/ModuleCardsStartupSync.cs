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
    ///
    /// The refresh runs on the first update tick rather than on a delayCall, which never fires while
    /// the Editor sits unfocused: the cards are read by an assistant and an IDE with Unity in the
    /// background, so they have to follow a compile nobody is watching. Scheduling twice before the
    /// tick runs the refresh once.
    /// </summary>
    internal class ModuleCardsStartupSync : AssetPostprocessor
    {
        [DidReloadScripts]
        private static void OnScriptsReloaded() => Schedule();

        private static void OnPostprocessAllAssets(
            string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            if (!Touches(imported) && !Touches(deleted) && !Touches(moved)) return;

            Schedule();
        }

        private static void Schedule()
        {
            EditorApplication.update -= RunOnce;
            EditorApplication.update += RunOnce;
        }

        private static void RunOnce()
        {
            if (EditorApplication.isUpdating || EditorApplication.isCompiling) return;

            EditorApplication.update -= RunOnce;
            new ModuleCardsRefresher().Run();
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
