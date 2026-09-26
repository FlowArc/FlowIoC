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
    /// dropdown and opens the picked one, both in edit mode and in play mode. The list is
    /// <see cref="SceneSwitcherPopup"/>: a search, tabs for the scenes opened most, every scene by
    /// module, the screens' test scenes and the modules' test scenes, and the game's own scenes
    /// pinned above them.
    /// </summary>
    public class SceneSwitcherDropdown
    {
        /// <summary>
        /// Toolbar path the element is registered under. It doubles as the identifier
        /// <see cref="MainToolbar.Refresh(string)"/> expects when the scene list changes.
        /// </summary>
        public const string ELEMENT_PATH = "FlowIoC/Scene Switcher";

        private const string MODULES_FOLDER = "Assets/Modules";

        private readonly List<SceneEntry> _scenes = new();
        private readonly SceneSwitcherUsage _usage = new();

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
            string activePath = Application.isPlaying
                ? SceneManager.GetActiveScene().path
                : EditorSceneManager.GetActiveScene().path;

            UnityEditor.PopupWindow.Show(dropDownRect,
                new SceneSwitcherPopup(_scenes, _usage, activePath, SwitchScene));
        }

        private void SwitchScene(string scenePath)
        {
            _usage.Record(scenePath);

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
                _scenes.Add(SceneEntry.From(AssetDatabase.GUIDToAssetPath(guid)));
        }
    }
}

#endif
