#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using FlowIoC.Editor.Modules;
using FlowIoC.Editor.ModuleScanner;
using UnityEditor;
using UnityEngine;

namespace FlowIoC.Editor.CodeGenerator.Menus.Module.RenameModule
{
    /// <summary>
    /// The part of a rename that needs the renamed code compiled: the cards. A card's generated
    /// block names the module's assemblies and its sub modules, and ModuleCardCheck reads those off
    /// the loaded assembly, which only exists after the reload the rename ends in. The renamer
    /// leaves the module names in EditorPrefs and this picks them up on the other side.
    ///
    /// The key is taken before the work, so a card that cannot be written is reported by the
    /// scanner rather than retried on every reload. A test module carries no card and is skipped.
    /// </summary>
    internal class ModuleRenameContinuation
    {
        internal void Run()
        {
            string json = EditorPrefs.GetString(ModuleRenamer.PENDING_KEY, string.Empty);
            EditorPrefs.DeleteKey(ModuleRenamer.PENDING_KEY);

            if (string.IsNullOrEmpty(json)) return;

            var pending = JsonUtility.FromJson<PendingRenameEVO>(json);
            (ProjectTargetEVO project, List<ModuleTargetEVO> modules) = new ModuleTargetFactory().Build();

            var cards = new ModuleCardCheck();
            var refreshed = new List<string>();

            foreach (string name in pending.Modules.Concat(new[] {pending.Parent}))
            {
                ModuleTargetEVO target = modules.FirstOrDefault(module => module.Name == name);

                if (target == null || target.Kind == ModuleKind.Test) continue;

                cards.Fix(target);
                refreshed.Add(name);
            }

            AssetDatabase.Refresh();

            Debug.Log("<color=cyan>[ModuleRenamer]</color> Rename finished. Cards refreshed: "
                      + (refreshed.Count == 0 ? "none" : string.Join(", ", refreshed)));
        }
    }
}
#endif
