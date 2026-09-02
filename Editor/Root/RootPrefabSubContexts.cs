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
        internal bool Add(string prefabAssetPath, string contextFullName, string contextName)
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
