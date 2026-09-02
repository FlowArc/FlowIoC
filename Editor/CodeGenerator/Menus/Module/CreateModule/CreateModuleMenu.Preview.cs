#if UNITY_EDITOR
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
        private void DisplayFolderStructurePreview()
        {
            EditorGUILayout.Space(10);

            GUI.backgroundColor = new Color(.6f, .4f, 1f);
            var style = new GUIStyle(EditorStyles.whiteLabel)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 12,
                richText = true
            };

            EditorGUILayout.BeginHorizontal(new GUIStyle(EditorStyles.helpBox), GUILayout.Height(33));
            GUILayout.Label(EditorGUIUtility.IconContent("console.infoicon"), GUILayout.Width(35), GUILayout.Height(33));
            EditorGUILayout.LabelField(FOLDER_STRUCTURE_PREVIEW, style, GUILayout.Height(33));
            EditorGUILayout.EndHorizontal();
            GUI.backgroundColor = Color.white;

            _folderPreviewScrollPosition = EditorGUILayout.BeginScrollView(_folderPreviewScrollPosition,
                GUILayout.MaxHeight(_selectedModuleType == ModuleType.Screen ? 190 : 200));
            EditorGUILayout.BeginVertical();
            DrawFolderPreview(_directoryConfigMap[_selectedModuleType].RootFolders, 0);
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndScrollView();
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
            EditorGUILayout.BeginHorizontal("box");
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

        private void DrawScreenSettings()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Screen", EditorStyles.boldLabel);

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
        }
    }
}
#endif