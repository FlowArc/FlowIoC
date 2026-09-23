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
    /// when none had. Neither half leaves it open, and <see cref="Create"/> says why.
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
        /// Makes the scene, saves it at this path and closes it again, beside whatever is open -
        /// which stays open, active and untouched. It is closed because the second half of the
        /// run saves it after the reload, and a scene that is open while it is saved there comes
        /// back from Unity as changed on disk, behind a Reload / Ignore modal that stops the
        /// Editor until somebody answers it - which an agent driving the Editor never does.
        /// Unity makes no new scene beside an untitled one; the run asked
        /// <see cref="UntitledScene"/> before it began.
        /// </summary>
        public void Create()
        {
            if (string.IsNullOrEmpty(_path))
                return;

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Additive);
            new GeneratedSceneCamera(scene).ClearToSolidColour();
            EditorSceneManager.SaveScene(scene, _path);
            EditorSceneManager.CloseScene(scene, true);
        }

        /// <summary>
        /// Runs <paramref name="edit"/> with this scene active, then saves this scene and only
        /// this scene. Nothing has it open - <see cref="Create"/> closed it - so it is opened
        /// beside whatever is open, edited, saved and closed again, and the open scenes are
        /// neither closed nor saved. A scene somebody opened during the compile is edited where
        /// it is.
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