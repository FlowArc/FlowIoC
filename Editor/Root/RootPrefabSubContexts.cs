#if UNITY_EDITOR

using System.Collections.Generic;
using System.Linq;
using FlowIoC.BaseModule.Root;
using UnityEditor;
using UnityEngine;

namespace FlowIoC.Editor.Root
{
    /// <summary>
    /// Adds a sub-context to a Root prefab, the way Add Sub Context in the Root inspector does by
    /// hand. Create Module uses it to attach a screen's context to the parent module's Root, and
    /// the screen config migrator to do the same for a context it generated.
    ///
    /// AutoSetup is on: a screen context registers itself with the screen service in Setup, and a
    /// Root only runs a sub-context's Setup when the entry says so.
    /// </summary>
    internal class RootPrefabSubContexts
    {
        /// <summary>
        /// <paramref name="contextScript"/> is the context's own script asset, which is what makes
        /// the entry a tracked reference rather than a name that can quietly stop resolving. The
        /// caller passes it because the caller is the one that knows it: a generator has just
        /// written the .cs file and can load it before it has compiled, which is exactly when
        /// nothing could resolve the name to a type yet. Left null the entry still works - the name
        /// is what runtime reads - and the Root inspector reports the gap with a Resolve button.
        /// </summary>
        internal bool Add(
            string prefabAssetPath, string contextFullName, string contextName, Object contextScript = null)
        {
            GameObject contents = PrefabUtility.LoadPrefabContents(prefabAssetPath);

            try
            {
                RootBase root = contents.GetComponent<RootBase>();
                if (root == null)
                    return false;

                root.SubContextTypes ??= new List<SubContextData>();

                if (root.SubContextTypes.Any(data => data.ContextFullName == contextFullName))
                    return true;

                root.SubContextTypes.Add(new SubContextData
                {
                    ContextScript = contextScript,
                    ContextFullName = contextFullName,
                    ContextName = contextName,
                    AutoSetup = true,
                    IsTest = false
                });

                PrefabUtility.SaveAsPrefabAsset(contents, prefabAssetPath);
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }
    }
}

#endif