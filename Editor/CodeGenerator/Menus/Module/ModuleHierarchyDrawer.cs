#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using FlowIoC.Editor.Config.ModuleConfig;
using UnityEditor;
using UnityEngine;

namespace FlowIoC.Editor.CodeGenerator.Menus.Module
{
    internal static class ModuleHierarchyDrawer
    {
        private const string MODULE_INFO_FILE = "_module_info.txt";

        public static void DrawModuleHierarchy(
            string path,
            int indentLevel,
            ref Dictionary<string, bool> moduleExpandedState,
            ref string parentModulePath,
            ref string selectedModuleName,
            ModuleType selectedModuleType)
        {
            string fullPath = Path.Combine(Application.dataPath, path);
            if (!Directory.Exists(fullPath)) return;

            string[] directories = Directory.GetDirectories(fullPath);
            for (var index = 0; index < directories.Length; index++)
            {
                var directory = directories[index];
                string moduleInfoPath = Path.Combine(directory, MODULE_INFO_FILE);
                if (File.Exists(moduleInfoPath))
                {
                    if (IsModuleExcluded(moduleInfoPath))
                    {
                        continue;
                    }

                    string directoryName = Path.GetFileName(directory);
                    string moduleTypePostfix = GetModuleTypePostfix(moduleInfoPath);
                    string displayName = directoryName + moduleTypePostfix;

                    moduleExpandedState.TryAdd(directory, false);

                    EditorGUILayout.BeginHorizontal("box");
                    
                    GUILayout.Space(indentLevel * 10);
                    GUIStyle foldoutStyle = new GUIStyle(EditorStyles.foldout) {richText = true};
                    moduleExpandedState[directory] = EditorGUILayout.Foldout(moduleExpandedState[directory], displayName, true, foldoutStyle);

                    bool isSelected = parentModulePath == directory;
                    string buttonText = isSelected ? "Selected" : "Select";
                    GUIStyle buttonStyle = new GUIStyle(GUI.skin.button)
                    {
                        normal = {textColor = isSelected ? Color.cyan : GUI.skin.button.normal.textColor},
                        hover = {textColor = isSelected ? Color.cyan : Color.yellow}
                    };

                    if (CanSelect(moduleInfoPath, selectedModuleType))
                    {
                        if (GUILayout.Button(buttonText, buttonStyle, GUILayout.Width(60)))
                        {
                            parentModulePath = directory;
                            selectedModuleName = GetModuleNameFromInfoFile(moduleInfoPath);
                        }
                    }
                    else
                    {
                        GUILayout.Space(70);
                    }

                    EditorGUILayout.EndHorizontal();

                    if (moduleExpandedState[directory])
                    {
                        EditorGUI.indentLevel++;
                        DrawModuleHierarchy(path + "/" + directoryName, indentLevel + 1, ref moduleExpandedState, ref parentModulePath,
                            ref selectedModuleName, selectedModuleType);
                        DrawSubModulesRecursively(directory, indentLevel + 1, ref moduleExpandedState, ref parentModulePath, ref selectedModuleName,
                            selectedModuleType);
                        EditorGUI.indentLevel--;
                    }
                }
            }
        }

        private static bool IsModuleExcluded(string moduleInfoPath)
        {
            var lines = File.ReadAllLines(moduleInfoPath);
            foreach (var line in lines)
            {
                if (line.StartsWith("Exclude: "))
                {
                    string excludeValue = line.Substring("Exclude: ".Length).Trim();
                    if (excludeValue.Equals("True", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private static bool CanSelect(string moduleInfoPath, ModuleType selectedModuleType)
        {
            string moduleType = GetModuleTypeFromInfoFile(moduleInfoPath);

            switch (selectedModuleType)
            {
                case ModuleType.Test when moduleType == "Test":
                case ModuleType.Screen when moduleType == "Test":
                case ModuleType.Main when moduleType == "Screen" || moduleType == "Test":
                    return false;
                default:
                    return true;
            }
        }

        private static string GetModuleTypeFromInfoFile(string moduleInfoPath)
        {
            foreach (string line in File.ReadAllLines(moduleInfoPath))
            {
                if (line.StartsWith("ModuleType: "))
                {
                    return line.Substring("ModuleType: ".Length).Trim();
                }
            }
            return "Main";
        }

        private static string GetModuleNameFromInfoFile(string moduleInfoPath)
        {
            foreach (string line in File.ReadAllLines(moduleInfoPath))
            {
                if (line.StartsWith("ModuleName: "))
                {
                    string moduleName = line.Substring("ModuleName: ".Length).Trim();
                    if (moduleName.EndsWith("Module"))
                    {
                        return moduleName.Substring(0, moduleName.Length - "Module".Length).Trim();
                    }
                    return moduleName;
                }
            }
            return string.Empty;
        }

        private static string GetModuleTypePostfix(string moduleInfoPath)
        {
            string postfix = string.Empty;
            foreach (string line in File.ReadAllLines(moduleInfoPath))
            {
                if (line.StartsWith("ModuleType: "))
                {
                    string typeString = line.Substring("ModuleType: ".Length).Trim();
                    if (Enum.TryParse(typeString, out ModuleType moduleType))
                    {
                        if (moduleType == ModuleType.Screen || moduleType == ModuleType.Test)
                            postfix = $" <b>({moduleType})</b>";
                    }
                }
            }
            return postfix;
        }

        private static void DrawSubModulesRecursively(
            string modulePath,
            int indentLevel,
            ref Dictionary<string, bool> moduleExpandedState,
            ref string parentModulePath,
            ref string selectedModuleName,
            ModuleType selectedModuleType)
        {
            CodeGeneratorSettings codeGenSettings = AssetDatabase.LoadAssetAtPath<CodeGeneratorSettings>(CodeGeneratorStrings.CONFIG_PATH);
            if (codeGenSettings == null)
            {
                Debug.LogError($"CodeGeneratorSettings asset not found at {CodeGeneratorStrings.CONFIG_PATH}.");
                return;
            }

            string subModulesFolderName = codeGenSettings.DirectoryStructureConfigMap[FolderConfig.FolderType.SubModules];
            string testModulesFolderName = codeGenSettings.DirectoryStructureConfigMap[FolderConfig.FolderType.TestModules];
            string screenModulesFolderName = codeGenSettings.DirectoryStructureConfigMap[FolderConfig.FolderType.ScreenModules];

            string[] subModulePaths = {subModulesFolderName, testModulesFolderName, screenModulesFolderName};

            foreach (string subPath in subModulePaths)
            {
                string subDirectoryPath = Path.Combine(modulePath, subPath);
                if (!Directory.Exists(subDirectoryPath)) continue;

                string[] subDirectories = Directory.GetDirectories(subDirectoryPath);
                foreach (string subDirectory in subDirectories)
                {
                    string moduleInfoPath = Path.Combine(subDirectory, MODULE_INFO_FILE);
                    if (!File.Exists(moduleInfoPath)) continue;

                    if (IsModuleExcluded(moduleInfoPath))
                    {
                        continue;
                    }

                    string directoryName = Path.GetFileName(subDirectory);
                    string moduleTypePostfix = GetModuleTypePostfix(moduleInfoPath);
                    string displayName = directoryName + moduleTypePostfix;

                    moduleExpandedState.TryAdd(subDirectory, false);

                    EditorGUILayout.BeginHorizontal("box");
                    GUILayout.Space(indentLevel * 20 - 10);
                    GUIStyle foldoutStyle = new GUIStyle(EditorStyles.foldout) {richText = true};
                    moduleExpandedState[subDirectory] = EditorGUILayout.Foldout(moduleExpandedState[subDirectory], displayName, true, foldoutStyle);

                    bool isSelected = parentModulePath == subDirectory;
                    string buttonText = isSelected ? "Selected" : "Select";
                    GUIStyle buttonStyle = new GUIStyle(GUI.skin.button)
                    {
                        normal = {textColor = isSelected ? Color.cyan : GUI.skin.button.normal.textColor},
                        hover = {textColor = isSelected ? Color.cyan : Color.yellow}
                    };

                    if (CanSelect(moduleInfoPath, selectedModuleType))
                    {
                        if (GUILayout.Button(buttonText, buttonStyle, GUILayout.Width(60)))
                        {
                            parentModulePath = subDirectory;
                            selectedModuleName = GetModuleNameFromInfoFile(moduleInfoPath);
                        }
                    }
                    else
                    {
                        GUILayout.Space(70);
                    }

                    EditorGUILayout.EndHorizontal();

                    if (moduleExpandedState[subDirectory])
                    {
                        DrawSubModulesRecursively(subDirectory, indentLevel + 1, ref moduleExpandedState, ref parentModulePath, ref selectedModuleName, selectedModuleType);
                    }
                }
            }
        }
    }
}
#endif
