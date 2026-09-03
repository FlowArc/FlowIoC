#if UNITY_EDITOR
using System.Collections.Generic;
using FlowIoC.BaseModule.Root;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FlowIoC.Editor.Root
{
    /// <summary>
    /// Which Roots already list a context. The same context on two Roots is a deliberate thing -
    /// a screen listed twice with two ManagerIds is registered, pooled and unregistered twice on
    /// purpose - so Add Sub Context reports it rather than forbidding it, and the reader adds the
    /// second entry knowing there is a first.
    ///
    /// Only what is open is read: the scenes currently loaded, or the prefab stage when one is
    /// open. Reading every Root prefab in the project would mean loading every one of them, which
    /// is a cost a window cannot pay each time it opens.
    /// </summary>
    internal class SceneSubContextUsage
    {
        private readonly Dictionary<string, List<string>> _rootsByContext = new();

        internal SceneSubContextUsage(RootBase askingRoot)
        {
            foreach (RootBase root in RootsInScope())
            {
                if (root == null || root == askingRoot || root.SubContextTypes == null)
                    continue;

                foreach (SubContextData subContext in root.SubContextTypes)
                {
                    if (string.IsNullOrEmpty(subContext.ContextFullName))
                        continue;

                    if (!_rootsByContext.TryGetValue(subContext.ContextFullName, out List<string> names))
                    {
                        names = new List<string>();
                        _rootsByContext[subContext.ContextFullName] = names;
                    }

                    if (!names.Contains(root.name))
                        names.Add(root.name);
                }
            }
        }

        /// <summary>
        /// The Roots that already list this context, as one readable line, or null when none do.
        /// </summary>
        internal string UsedBy(string contextFullName)
        {
            if (string.IsNullOrEmpty(contextFullName))
                return null;

            return _rootsByContext.TryGetValue(contextFullName, out List<string> names)
                ? string.Join(", ", names)
                : null;
        }

        private IEnumerable<RootBase> RootsInScope()
        {
            var stage = PrefabStageUtility.GetCurrentPrefabStage();

            if (stage != null)
                return stage.prefabContentsRoot.GetComponentsInChildren<RootBase>(true);

            return Object.FindObjectsByType<RootBase>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        }
    }
}
#endif