#if UNITY_EDITOR && UNITY_6000_3_OR_NEWER

using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FlowIoC.Editor.SceneSwitcher
{
    /// <summary>
    /// Lists every scene that lives under the FlowIoC modules folder in a main toolbar
    /// dropdown and opens the picked one, both in edit mode and in play mode. Entries are
    /// labelled "ModuleName/SceneName" so scenes belonging to different modules stay
    /// distinguishable when they share a name.
    /// </summary>
    public class SceneSwitcherDropdown
    {
        /// <summary>
        /// Toolbar path the element is registered under. It doubles as the identifier
        /// <see cref="MainToolbar.Refresh(string)"/> expects when the scene list changes.
        /// </summary>
        public const string ELEMENT_PATH = "FlowIoC/Scene Switcher";

        private const string MODULES_FOLDER = "Assets/Modules";
        private const string MODULES_FOLDER_NAME = "Modules";

        private readonly List<SceneEntry> _scenes = new();

        public SceneSwitcherDropdown()
        {
            RefreshSceneList();
            EditorApplication.projectChanged += OnProjectChanged;
        }

        /// <summary>
        /// Builds the toolbar element. Unity calls this again after every domain reload,
        /// so the element itself carries no state beyond the dropdown callback.
        /// </summary>
        public MainToolbarElement CreateElement()
        {
            var icon = EditorGUIUtility.IconContent("d_SceneAsset Icon").image as Texture2D;
            var content = new MainToolbarContent("Scene Switcher", icon, "Switch Scene");
            return new MainToolbarDropdown(content, ShowDropdownMenu);
        }

        private void ShowDropdownMenu(Rect dropDownRect)
        {
            var menu = new GenericMenu();

            if (_scenes.Count == 0)
            {
                menu.AddDisabledItem(new GUIContent("No scenes found"));
                menu.DropDown(dropDownRect);
                return;
            }

            string currentScene = Application.isPlaying
                ? SceneManager.GetActiveScene().name
                : EditorSceneManager.GetActiveScene().name;

            foreach (SceneEntry scene in _scenes)
            {
                string path = scene.Path;
                bool isActive = Path.GetFileNameWithoutExtension(path) == currentScene;

                menu.AddItem(new GUIContent(scene.DisplayName), isActive, () => SwitchScene(path));
            }

            menu.DropDown(dropDownRect);
        }

        private void SwitchScene(string scenePath)
        {
            if (Application.isPlaying)
            {
                string sceneName = Path.GetFileNameWithoutExtension(scenePath);

                // Play mode can only reach scenes that made it into the build, so the
                // dropdown has to report the ones it cannot open instead of failing silently.
                if (Application.CanStreamedLevelBeLoaded(sceneName))
                    SceneManager.LoadScene(sceneName);
                else
                    Debug.LogError($"Scene '{sceneName}' is not in Build Settings.");

                return;
            }

            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(scenePath);
        }

        private void OnProjectChanged()
        {
            RefreshSceneList();
            MainToolbar.Refresh(ELEMENT_PATH);
        }

        private void RefreshSceneList()
        {
            _scenes.Clear();

            if (!AssetDatabase.IsValidFolder(MODULES_FOLDER)) return;

            string[] guids = AssetDatabase.FindAssets("t:Scene", new[] { MODULES_FOLDER });

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                _scenes.Add(new SceneEntry(BuildDisplayName(path), path));
            }

            _scenes.Sort((left, right) => string.CompareOrdinal(left.DisplayName, right.DisplayName));
        }

        private string BuildDisplayName(string scenePath)
        {
            string sceneName = Path.GetFileNameWithoutExtension(scenePath);
            string folderPath = Path.GetDirectoryName(scenePath)?.Replace("\\", "/");

            if (string.IsNullOrEmpty(folderPath)) return sceneName;

            string[] folders = folderPath.Split('/');

            // The module name is whatever folder sits directly under "Modules"; the rest of
            // the path is the module's internal layout and carries no meaning for the menu.
            for (int i = 0; i < folders.Length - 1; i++)
            {
                if (folders[i] == MODULES_FOLDER_NAME)
                    return $"{folders[i + 1]}/{sceneName}";
            }

            return sceneName;
        }

        private readonly struct SceneEntry
        {
            public readonly string DisplayName;
            public readonly string Path;

            public SceneEntry(string displayName, string path)
            {
                DisplayName = displayName;
                Path = path;
            }
        }
    }
}

#endif
