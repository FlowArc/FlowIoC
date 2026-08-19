#if UNITY_EDITOR
using System.Collections.Generic;
using FlowIoC.Editor.CodeGenerator;
using UnityEditor;
using UnityEngine;

namespace FlowIoC.Editor.Config.ModuleConfig
{
    [CustomPropertyDrawer(typeof(FolderConfig))]
    public class FolderConfigDrawer : PropertyDrawer
    {
        private static Dictionary<string, FolderConfig.FolderType> _previousTypeByPath
            = new Dictionary<string, FolderConfig.FolderType>();

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            float lineHeight = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;

            Rect foldoutRect = new Rect(position.x, position.y, position.width, lineHeight);
            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, label, true);

            if (!property.isExpanded)
                return;

            EditorGUI.indentLevel++;
            float yOffset = position.y + lineHeight + spacing;

            SerializedProperty folderNameProp = property.FindPropertyRelative("FolderName");
            SerializedProperty typeProp = property.FindPropertyRelative("Type");
            SerializedProperty isMandatoryProp = property.FindPropertyRelative("IsMandatory");
            SerializedProperty isOptionalProp = property.FindPropertyRelative("IsOptional");
            SerializedProperty isNamespaceProviderProp = property.FindPropertyRelative("IsNamespaceProvider");
            SerializedProperty subFoldersProp = property.FindPropertyRelative("SubFolders");

            FolderConfig.FolderType currentType = (FolderConfig.FolderType)typeProp.enumValueIndex;

            if (!_previousTypeByPath.TryGetValue(property.propertyPath, out var oldType))
            {
                oldType = currentType;
                _previousTypeByPath[property.propertyPath] = oldType;
            }

            Rect typeRect = new Rect(position.x, yOffset, position.width, lineHeight);
            EditorGUI.BeginChangeCheck();
            EditorGUI.PropertyField(typeRect, typeProp, new GUIContent("Folder Type"));
            if (EditorGUI.EndChangeCheck())
            {
                currentType = (FolderConfig.FolderType)typeProp.enumValueIndex;
                if (oldType != currentType)
                {
                    if (currentType != FolderConfig.FolderType.Folder)
                    {
                        CodeGeneratorSettings settings = TryGetCodeGeneratorSettings();
                        if (settings != null
                            && settings.DirectoryStructureConfigMap.TryGetValue(currentType, out string lockedName))
                        {
                            folderNameProp.stringValue = lockedName;
                        }
                        else
                        {
                            folderNameProp.stringValue = currentType.ToString();
                        }
                    }

                    _previousTypeByPath[property.propertyPath] = currentType;
                }
            }

            yOffset += lineHeight + spacing;

            bool isFolder = currentType == FolderConfig.FolderType.Folder;
            Rect folderNameRect = new Rect(position.x, yOffset, position.width, lineHeight);
            using (new EditorGUI.DisabledGroupScope(!isFolder))
            {
                EditorGUI.PropertyField(folderNameRect, folderNameProp, new GUIContent("Folder Name"));
            }

            yOffset += lineHeight + spacing;

            Rect isMandatoryRect = new Rect(position.x, yOffset, position.width, lineHeight);
            EditorGUI.PropertyField(isMandatoryRect, isMandatoryProp, new GUIContent("Is Mandatory"));
            yOffset += lineHeight + spacing;

            Rect isOptionalRect = new Rect(position.x, yOffset, position.width, lineHeight);
            EditorGUI.PropertyField(isOptionalRect, isOptionalProp, new GUIContent("Is Optional"));
            yOffset += lineHeight + spacing;

            Rect isNamespaceProviderRect = new Rect(position.x, yOffset, position.width, lineHeight);
            EditorGUI.PropertyField(isNamespaceProviderRect, isNamespaceProviderProp, new GUIContent("Is Namespace Provider"));
            yOffset += lineHeight + spacing;

            Rect subFoldersRect = new Rect(position.x, yOffset, position.width,
                EditorGUI.GetPropertyHeight(subFoldersProp, true));
            EditorGUI.PropertyField(subFoldersRect, subFoldersProp, new GUIContent("Sub Folders"), true);
            yOffset += subFoldersRect.height + spacing;

            EditorGUI.indentLevel--;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!property.isExpanded)
                return EditorGUIUtility.singleLineHeight;

            float lineHeight = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;
            float total = 0f;

            total += lineHeight + spacing;
            total += lineHeight + spacing;
            total += lineHeight + spacing;
            total += lineHeight + spacing;
            total += lineHeight + spacing;
            total += lineHeight + spacing;

            SerializedProperty subFoldersProp = property.FindPropertyRelative("SubFolders");
            total += EditorGUI.GetPropertyHeight(subFoldersProp, true) + spacing;

            return total;
        }

        private CodeGeneratorSettings TryGetCodeGeneratorSettings()
        {
            return AssetDatabase.LoadAssetAtPath<CodeGeneratorSettings>(CodeGeneratorStrings.CONFIG_PATH);
        }
    }
}
#endif