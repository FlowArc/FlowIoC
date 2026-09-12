#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using FlowIoC.BaseModule.Root;
using FlowIoC.Editor.CodeGenerator.Menus.Module.DeleteModule;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace FlowIoC.Editor.CodeGenerator.Menus.Module.RenameModule
{
    /// <summary>
    /// Rewrites the full name every Root has written down for one of the module's contexts, in
    /// every scene and prefab that lists one.
    ///
    /// Runtime resolves a sub-context from ContextFullName and from nothing else, so a namespace or
    /// a class renamed in code leaves every such Root pointing at a name that no longer compiles to
    /// anything - reported at play time as "Context Type couldn't find!". The inspector heals an
    /// entry from its script reference when somebody opens the Root, and this does the same for all
    /// of them at once, from the rename's own rules, before the code has even been recompiled.
    ///
    /// Which assets to open is the dependency index's answer, the way SubContextUnwirer asks it,
    /// and which entries are the module's is ModuleSubContextEntries' - the script the entry points
    /// at sits inside the module folder or it does not. The three kinds of asset are treated as the
    /// unwirer treats them: a prefab is written, a closed scene is opened, written and closed, and
    /// an open scene is changed and left dirty for its owner.
    /// </summary>
    internal class SubContextRenamer
    {
        private readonly ModuleAssetReferences _references;
        private readonly ModuleSubContextEntries _entries;
        private readonly SourceTextRewriter _rewriter = new SourceTextRewriter();

        internal SubContextRenamer() : this(new ModuleAssetReferences(), new ModuleSubContextEntries())
        {
        }

        internal SubContextRenamer(ModuleAssetReferences references, ModuleSubContextEntries entries)
        {
            _references = references;
            _entries = entries;
        }

        /// <summary>The entry with its two names run through the rules, everything else as it was.</summary>
        internal SubContextData Mapped(SubContextData entry, IReadOnlyList<TextRule> rules, out bool changed)
        {
            string fullName = _rewriter.Rewrite(entry.ContextFullName ?? string.Empty, rules, out changed);

            if (!changed) return entry;

            entry.ContextFullName = fullName;
            entry.ContextName = fullName.Substring(fullName.LastIndexOf('.') + 1);

            return entry;
        }

        /// <summary>
        /// Runs before any folder moves, while the module is still at
        /// <paramref name="moduleAssetPath"/>: the dependency index names the assets by the scripts
        /// as they sit now, and so does the entry matching.
        /// </summary>
        internal IReadOnlyList<SubContextRenameEVO> Rename(string moduleAssetPath, IReadOnlyList<TextRule> rules)
        {
            var outcomes = new List<SubContextRenameEVO>();

            if (string.IsNullOrEmpty(moduleAssetPath)) return outcomes;

            foreach (string assetPath in _references.AssetsPointingIntoOrInside(moduleAssetPath))
            {
                if (assetPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                    RenameInPrefab(assetPath, moduleAssetPath, rules, outcomes);
                else if (assetPath.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
                    RenameInScene(assetPath, moduleAssetPath, rules, outcomes);
            }

            return outcomes;
        }

        private void RenameInPrefab(
            string assetPath, string moduleAssetPath, IReadOnlyList<TextRule> rules, List<SubContextRenameEVO> outcomes)
        {
            GameObject contents = PrefabUtility.LoadPrefabContents(assetPath);

            try
            {
                bool changed = false;

                foreach (RootBase root in contents.GetComponentsInChildren<RootBase>(true))
                    changed |= RenameInRoot(root, assetPath, moduleAssetPath, rules, outcomes, false);

                if (changed) PrefabUtility.SaveAsPrefabAsset(contents, assetPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        private void RenameInScene(
            string assetPath, string moduleAssetPath, IReadOnlyList<TextRule> rules, List<SubContextRenameEVO> outcomes)
        {
            Scene open = SceneManager.GetSceneByPath(assetPath);
            bool wasOpen = open.IsValid() && open.isLoaded;

            Scene scene = wasOpen ? open : EditorSceneManager.OpenScene(assetPath, OpenSceneMode.Additive);

            if (!scene.IsValid()) return;

            bool changed = false;

            foreach (RootBase root in Object.FindObjectsByType<RootBase>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (root.gameObject.scene != scene) continue;

                changed |= RenameInRoot(root, assetPath, moduleAssetPath, rules, outcomes, wasOpen);
            }

            if (changed)
            {
                EditorSceneManager.MarkSceneDirty(scene);

                if (!wasOpen) EditorSceneManager.SaveScene(scene);
            }

            if (!wasOpen) EditorSceneManager.CloseScene(scene, true);
        }

        private bool RenameInRoot(
            RootBase root, string assetPath, string moduleAssetPath, IReadOnlyList<TextRule> rules,
            List<SubContextRenameEVO> outcomes, bool leftUnsaved)
        {
            IReadOnlyList<int> owned = _entries.IndexesIn(root.SubContextTypes, moduleAssetPath);

            var changedAny = false;

            foreach (int index in owned)
            {
                SubContextData before = root.SubContextTypes[index];
                SubContextData after = Mapped(before, rules, out bool changed);

                if (!changed) continue;

                // Recorded once, before the first write, so an open scene keeps the one undo it has.
                if (!changedAny) Undo.RecordObject(root, "rename-sub-contexts");

                root.SubContextTypes[index] = after;
                changedAny = true;

                outcomes.Add(new SubContextRenameEVO
                {
                    AssetPath = assetPath,
                    RootName = root.name,
                    OldName = before.ContextName,
                    NewName = after.ContextName,
                    SceneLeftUnsaved = leftUnsaved
                });
            }

            if (changedAny) EditorUtility.SetDirty(root);

            return changedAny;
        }
    }
}

#endif
