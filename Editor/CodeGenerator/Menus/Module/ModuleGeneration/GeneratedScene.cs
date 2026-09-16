#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FlowIoC.Editor.CodeGenerator.Menus.Module.ModuleGeneration
{
    /// <summary>
    /// The scene a Create Module run made before the reload, known by its path and by nothing
    /// else. The second half of the run edits this scene and saves it under its own path. It
    /// never saves whatever scene happens to be active: that was the owner's open scene once,
    /// written to disk with a stray Root in it, because a stale flag said a scene had been made
    /// when none had.
    /// </summary>
    internal class GeneratedScene
    {
        private readonly string _path;

        /// <param name="path">The asset path the first half saved the scene to; empty when the run made none.</param>
        public GeneratedScene(string path)
        {
            _path = Normalize(path);
        }

        /// <summary>
        /// Whether the scene at <paramref name="scenePath"/> is this one. Never for an unsaved
        /// scene, whose path is empty, and never when the run made no scene at all.
        /// </summary>
        public bool IsAt(string scenePath)
        {
            if (string.IsNullOrEmpty(_path))
                return false;

            string candidate = Normalize(scenePath);

            return !string.IsNullOrEmpty(candidate) && string.Equals(candidate, _path, StringComparison.Ordinal);
        }

        /// <summary>
        /// Runs <paramref name="edit"/> with this scene active, then saves this scene and only
        /// this scene. Normally it is still the active scene - the first half made it with
        /// NewScene and the reload kept it - and it is edited in place. When something else is
        /// active, whatever was opened during the compile, this scene is opened beside it,
        /// edited, saved and closed again, and the other scene is neither closed nor saved.
        /// </summary>
        public void Edit(Action<Scene> edit)
        {
            if (string.IsNullOrEmpty(_path))
                return;

            Scene active = SceneManager.GetActiveScene();
            Scene open = SceneManager.GetSceneByPath(_path);
            bool wasOpen = open.IsValid() && open.isLoaded;

            if (!wasOpen && AssetDatabase.LoadAssetAtPath<SceneAsset>(_path) == null)
            {
                Debug.LogError($"<color=cyan>[FlowIoC]</color> The scene Create Module made at '{_path}' is gone, "
                               + "so its Root was not placed. Create the scene again and add the Root to it.");
                return;
            }

            Scene scene = wasOpen ? open : EditorSceneManager.OpenScene(_path, OpenSceneMode.Additive);

            if (!scene.IsValid())
            {
                Debug.LogError($"<color=cyan>[FlowIoC]</color> The scene at '{_path}' could not be opened, "
                               + "so its Root was not placed. Open it and add the Root to it.");
                return;
            }

            SceneManager.SetActiveScene(scene);

            try
            {
                edit(scene);
                EditorSceneManager.SaveScene(scene);
            }
            finally
            {
                if (active.IsValid() && active.isLoaded)
                    SceneManager.SetActiveScene(active);

                if (!wasOpen)
                    EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static string Normalize(string path)
        {
            return string.IsNullOrEmpty(path) ? string.Empty : path.Replace('\\', '/');
        }
    }
}
#endif