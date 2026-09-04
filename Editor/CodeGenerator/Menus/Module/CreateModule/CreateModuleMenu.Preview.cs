#if UNITY_EDITOR
using FlowIoC.Editor.Inspector;
using System.Collections.Generic;
using FlowIoC.Editor.Config.ModuleConfig;
using FlowIoC.ScreenModule.Data;
using FlowIoC.ScreenModule.Enums;
using UnityEditor;
using UnityEngine;

namespace FlowIoC.Editor.CodeGenerator.Menus.Module.CreateModule
{
    internal partial class CreateModuleMenu
    {
        /// <summary>
        /// The layout the module will be written from, and the folders that are optional in it.
        /// It is drawn whether or not a name has been typed, and turns itself off with the rest of
        /// the window until one is - a window that is half live reads as broken.
        /// </summary>
        private void DisplayFolderStructurePreview()
        {
            EditorGUILayout.Space(10);

            GUI.backgroundColor = new ModulePanelTheme().Header;
            var style = new GUIStyle(EditorStyles.whiteLabel)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 12,
                richText = true
            };

            EditorGUILayout.BeginHorizontal(new GUIStyle(EditorStyles.helpBox), GUILayout.Height(PANEL_HEADER_HEIGHT));
            GUILayout.Label(EditorGUIUtility.IconContent("console.infoicon"), GUILayout.Width(35), GUILayout.Height(PANEL_HEADER_HEIGHT));
            EditorGUILayout.LabelField(FOLDER_STRUCTURE_PREVIEW, style, GUILayout.Height(PANEL_HEADER_HEIGHT));

            GUI.backgroundColor = Color.white;
            DrawConfigButton();

            EditorGUILayout.EndHorizontal();

            float height = PANEL_HEIGHT;

            _folderPreviewScrollPosition = EditorGUILayout.BeginScrollView(_folderPreviewScrollPosition,
                GUILayout.MinHeight(height), GUILayout.MaxHeight(height));
            EditorGUILayout.BeginVertical();
            DrawFolderPreview(_directoryConfigMap[_selectedModuleType].RootFolders, 0);
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// Opens the layout the preview below is read from. Which asset that is follows the module
        /// type, so it is a button on the preview's own bar rather than a field the reader has to
        /// keep an eye on: pressing it selects the asset and pings it in the Project window.
        /// </summary>
        private void DrawConfigButton()
        {
            DirectoryStructureConfig config = _directoryConfigMap[_selectedModuleType];

            if (config == null) return;

            // Square, the height of the bar it sits on, and an icon rather than a word: the bar
            // already says what the panel is, and the button is the one thing on it to press.
            var content = new GUIContent(ConfigIcon(), CONFIG_BUTTON_TOOLTIP);

            if (!GUILayout.Button(content, GUILayout.Width(PANEL_HEADER_HEIGHT), GUILayout.Height(PANEL_HEADER_HEIGHT)))
                return;

            Selection.activeObject = config;
            EditorGUIUtility.PingObject(config);
        }

        /// <summary>
        /// The gear, under whichever name this Editor knows it by. Unity has renamed the built-in
        /// icon more than once, so the fallbacks matter more than which one wins.
        /// </summary>
        private Texture ConfigIcon()
        {
            foreach (string name in new[] {"Settings", "SettingsIcon", "_Popup", "ScriptableObject Icon"})
            {
                Texture icon = EditorGUIUtility.IconContent(name)?.image;

                if (icon != null) return icon;
            }

            return null;
        }

        private void DrawFolderPreview(List<FolderEVO> folders, int indentLevel)
        {
            foreach (FolderEVO folder in folders)
            {
                if (!folder.IsMandatory && !folder.IsOptional)
                    continue;

                DrawTreeLine(new string(' ', indentLevel * 2) + "-" + folder.FolderName, indentLevel, folder);

                if (folder.SubFolders != null && folder.SubFolders.Count > 0)
                {
                    DrawFolderPreview(folder.SubFolders, indentLevel + 1);
                }
            }
        }

        private void DrawTreeLine(string text, int indentLevel, FolderEVO folder)
        {
            GUI.backgroundColor = new ModulePanelTheme().Row;
            EditorGUILayout.BeginHorizontal("box");
            GUI.backgroundColor = Color.white;
            GUILayout.Space(indentLevel * 20);

            if (folder.IsMandatory)
            {
                GUILayout.Space(20);
            }
            else if (folder.IsOptional)
            {
                bool isSelected = _selectedOptionalFolders.Contains(folder);
                bool newSelection = EditorGUILayout.Toggle(isSelected, GUILayout.Width(18));

                if (newSelection && !isSelected)
                {
                    _selectedOptionalFolders.Add(folder);
                }
                else if (!newSelection && isSelected)
                {
                    _selectedOptionalFolders.Remove(folder);
                }
            }

            if (indentLevel > 0)
                GUILayout.Space(-15);

            EditorGUILayout.LabelField(text, GUILayout.Width(200));
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// What the screen this module declares will do by default. The panel wears the Screen
        /// role's gold - the colour its view and its Root carry in the inspector - so the fields
        /// are read as the screen's own rather than as more of the module's.
        /// </summary>
        private void DrawScreenSettings()
        {
            EditorGUILayout.Space(10);

            var painter = new FlowRowPainter();

            // One fill behind the whole block rather than a tint per field: the amber is Screen
            // Scanner's, the colour it gives a row whose layer is shared, so a reader who knows
            // that window reads this panel as the screen's settings on sight.
            // The padding is the style's rather than a Space: it insets the fields on every side,
            // so the block reads as a panel with a margin instead of text pinned to the fill's
            // left edge and the stripe.
            var body = new GUIStyle {padding = new RectOffset(12, 10, 6, 8)};

            Rect area = EditorGUILayout.BeginVertical(body);

            if (Event.current.type == EventType.Repaint)
                painter.Paint(area, painter.Warn);

            EditorGUILayout.LabelField(SCREEN_SETTINGS_LABEL, EditorStyles.boldLabel);

            _screenSettings.ManagerId = EditorGUILayout.IntField("Manager Id", _screenSettings.ManagerId);
            _screenSettings.Layer = EditorGUILayout.IntField("Layer", _screenSettings.Layer);
            _screenSettings.Tag = (ScreenTag) EditorGUILayout.EnumPopup("Tag", _screenSettings.Tag);
            _screenSettings.LoadType = (ScreenLoadType) EditorGUILayout.EnumPopup("Load", _screenSettings.LoadType);

            switch (_screenSettings.LoadType)
            {
                case ScreenLoadType.Resource:
                    _screenSettings.ResourcePath = EditorGUILayout.TextField("Resources Path", _screenSettings.ResourcePath);
                    break;
                default:
                    // The address is the module name, and the generator gives the prefab that same
                    // address, so it is shown rather than asked for.
                    EditorGUILayout.LabelField("Address", _moduleName + _moduleSuffix);
                    break;
            }

            _screenSettings.HasShowAnimation = EditorGUILayout.ToggleLeft("Has Show Animation", _screenSettings.HasShowAnimation);
            _screenSettings.HasHideAnimation = EditorGUILayout.ToggleLeft("Has Hide Animation", _screenSettings.HasHideAnimation);

            EditorGUILayout.EndVertical();
        }
    }
}
#endif