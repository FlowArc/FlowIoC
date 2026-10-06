#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FlowIoC.BaseModule.Root;
using FlowIoC.Editor.Root;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FlowIoC.Editor.CodeGenerator.Menus.Module.ModuleGeneration
{
    /// <summary>
    /// Lists a test screen's context on the test Root it was made for - the Root named after the
    /// test module, ResourceFlyTestRoot for ResourceFlyTestModule - in a scene under that test
    /// module's Scenes folder. The screen has no scene of its own; this is where it is seen.
    ///
    /// A scene somebody has open is changed where it is and left dirty, never saved: whatever else
    /// is unsaved in it is theirs. A closed one is opened beside the open ones, changed, saved and
    /// closed again. When no scene holds the Root, the warning names the step that is left: Add
    /// Sub Context on the test Root, which offers the screen there.
    /// </summary>
    internal class TestRootListing
    {
        private const string MODULE_SUFFIX = "Module";

        internal void Add(string testModulePath, string scenesFolder, string contextFullName)
        {
            if (string.IsNullOrEmpty(contextFullName))
                return;

            string rootName = RootNameFor(testModulePath);
            string contextName = contextFullName.Substring(contextFullName.LastIndexOf('.') + 1);

            foreach (string scenePath in ScenesIn(scenesFolder))
            {
                if (TryList(scenePath, rootName, contextFullName, contextName))
                    return;
            }

            Debug.LogWarning($"<color=cyan>[FlowIoC]</color> No {rootName} was found in the scenes under '{scenesFolder}', "
                             + $"so {contextName} is not listed on a Root yet. Select the test Root, press Add Sub "
                             + $"Context and pick {contextName} - it is offered there.");
        }

        private bool TryList(string scenePath, string rootName, string contextFullName, string contextName)
        {
            Scene open = SceneManager.GetSceneByPath(scenePath);
            bool wasOpen = open.IsValid() && open.isLoaded;
            Scene scene = wasOpen ? open : EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);

            try
            {
                RootBase root = scene.GetRootGameObjects()
                    .SelectMany(gameObject => gameObject.GetComponentsInChildren<RootBase>(true))
                    .FirstOrDefault(candidate => candidate.GetType().Name == rootName);

                if (root == null)
                    return false;

                root.SubContextTypes ??= new List<SubContextData>();

                if (root.SubContextTypes.All(entry => entry.ContextFullName != contextFullName))
                {
                    root.SubContextTypes.Add(new SubContextData
                    {
                        ContextScript = new ContextScriptResolver().ForName(contextFullName),
                        ContextFullName = contextFullName,
                        ContextName = contextName,
                        AutoSetup = true,
                        IsTest = false
                    });

                    // A test Root placed as a prefab instance keeps the entry as an override of
                    // this scene, which is where it belongs, rather than losing it on the next load.
                    if (PrefabUtility.IsPartOfPrefabInstance(root))
                        PrefabUtility.RecordPrefabInstancePropertyModifications(root);

                    EditorUtility.SetDirty(root);
                    EditorSceneManager.MarkSceneDirty(scene);
                }

                if (!wasOpen)
                    EditorSceneManager.SaveScene(scene);

                Debug.Log($"<color=cyan>[FlowIoC]</color> {contextName} listed on {rootName} in '{scenePath}'"
                          + (wasOpen ? " - the scene is open, so it is left unsaved." : "."));

                return true;
            }
            finally
            {
                if (!wasOpen)
                    EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static IEnumerable<string> ScenesIn(string scenesFolder)
        {
            if (string.IsNullOrEmpty(scenesFolder) || !Directory.Exists(scenesFolder))
                return Enumerable.Empty<string>();

            return Directory.GetFiles(scenesFolder, "*.unity", SearchOption.AllDirectories)
                .Select(NamespaceUtility.GetUnityAssetPath)
                .OrderBy(path => path);
        }

        private static string RootNameFor(string testModulePath)
        {
            string folder = Path.GetFileName((testModulePath ?? string.Empty).TrimEnd('/', '\\'));

            return (folder.EndsWith(MODULE_SUFFIX) ? folder.Substring(0, folder.Length - MODULE_SUFFIX.Length) : folder) + "Root";
        }
    }
}
#endif
