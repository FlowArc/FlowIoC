#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using FlowIoC.BaseModule.Root;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace FlowIoC.Editor.CodeGenerator.Menus.Module.DeleteModule
{
    /// <summary>
    /// Takes a deleted module's sub-contexts out of the Roots that list them.
    ///
    /// This is the rule the rest of Delete Module already follows - unwire a thing from everywhere
    /// it is referenced, then delete - reaching the one place that used to be exempt. It was exempt
    /// because a Root listed its sub-contexts by name, so nothing could be found without reading
    /// every scene as text, and because fixing one means writing a scene or a prefab.
    ///
    /// Both halves changed. The entry holds the context's script, so the assets to open are the
    /// ones the dependency index already names. And of the three kinds of asset only one is
    /// genuinely the reader's: a prefab is a file, a closed scene has nothing unsaved in it, and an
    /// **open** scene may be carrying an experiment nobody wants written. So a prefab is saved, a
    /// closed scene is opened, written and closed again, and an open scene is changed and left
    /// dirty for its owner to save or throw away.
    ///
    /// Nothing here opens a dialog. Whether to remove a given entry is the caller's answer, handed
    /// in as a delegate, which is what lets the window ask once for all of them or once for each
    /// without this class knowing which.
    /// </summary>
    internal class SubContextUnwirer
    {
        private readonly ModuleAssetReferences _references;
        private readonly ModuleSubContextEntries _entries;

        internal SubContextUnwirer() : this(new ModuleAssetReferences(), new ModuleSubContextEntries())
        {
        }

        internal SubContextUnwirer(ModuleAssetReferences references, ModuleSubContextEntries entries)
        {
            _references = references;
            _entries = entries;
        }

        /// <summary>
        /// Every entry of this module found in a Root outside it, reported and not touched. This is
        /// what the cancelling answer shows, and it costs a dependency query rather than opening
        /// anything.
        /// </summary>
        internal IReadOnlyList<string> Report(string moduleAssetPath) => _references.Find(moduleAssetPath);

        /// <summary>
        /// Walks the assets that point into the module and offers each entry to
        /// <paramref name="shouldRemove"/>. Returns what became of every one of them.
        /// </summary>
        internal IReadOnlyList<SubContextUnwireEVO> Remove(
            string moduleAssetPath, Func<SubContextUnwireEVO, bool> shouldRemove)
        {
            var outcomes = new List<SubContextUnwireEVO>();

            if (string.IsNullOrEmpty(moduleAssetPath)) return outcomes;

            foreach (string assetPath in _references.AssetsPointingInto(moduleAssetPath))
            {
                if (assetPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                    RemoveFromPrefab(assetPath, moduleAssetPath, shouldRemove, outcomes);
                else if (assetPath.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
                    RemoveFromScene(assetPath, moduleAssetPath, shouldRemove, outcomes);
            }

            return outcomes;
        }

        /// <summary>
        /// A prefab is a file rather than somebody's workspace, so it is loaded, changed and written
        /// without asking anyone. LoadPrefabContents gives an instance of its own, which is why the
        /// Roots are found and the indexes computed here rather than carried in from a scan.
        /// </summary>
        private void RemoveFromPrefab(
            string assetPath,
            string moduleAssetPath,
            Func<SubContextUnwireEVO, bool> shouldRemove,
            List<SubContextUnwireEVO> outcomes)
        {
            GameObject contents = PrefabUtility.LoadPrefabContents(assetPath);

            try
            {
                bool changed = false;

                foreach (RootBase root in contents.GetComponentsInChildren<RootBase>(true))
                    changed |= RemoveFromRoot(root, assetPath, moduleAssetPath, shouldRemove, outcomes, false);

                if (changed) PrefabUtility.SaveAsPrefabAsset(contents, assetPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        /// <summary>
        /// A scene that is already open is changed and left dirty, because whatever else is in it
        /// unsaved belongs to whoever opened it. One that is not open is opened additively, written
        /// and closed again, which touches nothing the reader can see.
        /// </summary>
        private void RemoveFromScene(
            string assetPath,
            string moduleAssetPath,
            Func<SubContextUnwireEVO, bool> shouldRemove,
            List<SubContextUnwireEVO> outcomes)
        {
            Scene open = SceneManager.GetSceneByPath(assetPath);
            bool wasOpen = open.IsValid() && open.isLoaded;

            Scene scene = wasOpen ? open : EditorSceneManager.OpenScene(assetPath, OpenSceneMode.Additive);

            if (!scene.IsValid()) return;

            bool changed = false;

            foreach (RootBase root in Object.FindObjectsByType<RootBase>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (root.gameObject.scene != scene) continue;

                changed |= RemoveFromRoot(root, assetPath, moduleAssetPath, shouldRemove, outcomes, wasOpen);
            }

            if (changed)
            {
                EditorSceneManager.MarkSceneDirty(scene);

                if (!wasOpen) EditorSceneManager.SaveScene(scene);
            }

            if (!wasOpen) EditorSceneManager.CloseScene(scene, true);
        }

        /// <summary>
        /// One Root. Every entry the module owns is offered, and the ones taken are removed in a
        /// single pass afterwards so that removing the first cannot move the rest.
        /// </summary>
        private bool RemoveFromRoot(
            RootBase root,
            string assetPath,
            string moduleAssetPath,
            Func<SubContextUnwireEVO, bool> shouldRemove,
            List<SubContextUnwireEVO> outcomes,
            bool leftUnsaved)
        {
            IReadOnlyList<int> owned = _entries.IndexesIn(root.SubContextTypes, moduleAssetPath);

            if (owned.Count == 0) return false;

            var taking = new List<int>();

            foreach (int index in owned)
            {
                var found = new SubContextUnwireEVO
                {
                    AssetPath = assetPath,
                    RootName = root.name,
                    ContextName = root.SubContextTypes[index].ContextName,
                    Outcome = SubContextUnwireOutcome.Found
                };

                if (shouldRemove(found))
                {
                    taking.Add(index);
                    found.Outcome = leftUnsaved
                        ? SubContextUnwireOutcome.RemovedNotSaved
                        : SubContextUnwireOutcome.Removed;
                }
                else
                {
                    found.Outcome = SubContextUnwireOutcome.Skipped;
                }

                outcomes.Add(found);
            }

            if (taking.Count == 0) return false;

            // Recorded, so a removal made on an open scene can be taken back the way any other
            // inspector edit can - which is the only undo an unsaved scene has.
            Undo.RecordObject(root, "unwire-sub-contexts");

            _entries.RemoveAt(root.SubContextTypes, taking);

            EditorUtility.SetDirty(root);

            return true;
        }
    }
}

#endif
