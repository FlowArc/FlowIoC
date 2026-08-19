#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FlowIoC.Editor.CodeGenerator.Cleaner
{
    internal static class ModuleCleaner
    {
        private const string CleanerInitializedKey = "ModuleCleaner_Initialized";
        private static readonly List<string> RemovedObsoleteModules = new List<string>();

        [InitializeOnLoadMethod]
        private static void OnProjectLoad()
        {
            if (!SessionState.GetBool(CleanerInitializedKey, false))
            {
                SessionState.SetBool(CleanerInitializedKey, true);
                EditorApplication.delayCall += CleanObsoleteEntries;
            }
        }

        public static void CleanObsoleteEntries()
        {
            string modulesPath = Path.Combine(Application.dataPath, "Modules");
            if (!Directory.Exists(modulesPath))
            {
                Debug.LogWarning("Modules directory not found. Skipping cleaning process.");
                return;
            }

            Dictionary<string, string> infoFilesWithDirectories = FindInfoFilesWithDirectories(modulesPath);

            foreach (KeyValuePair<string, string> entry in infoFilesWithDirectories)
            {
                string infoFilePath = entry.Key;
                string relatedDirectory = entry.Value;

                if (File.Exists(infoFilePath))
                {
                    CleanInfoFile(infoFilePath, relatedDirectory);
                }
            }

            if (RemovedObsoleteModules.Count > 0)
            {
                ShowCleaningSummaryPopup();
            }
        }

        private static Dictionary<string, string> FindInfoFilesWithDirectories(string rootPath)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            
            string[] rootInfoFiles = Directory.GetFiles(rootPath, "*_modules_info.txt", SearchOption.TopDirectoryOnly);
            foreach (string infoFile in rootInfoFiles)
            {
                result[infoFile] = rootPath;
            }
            
            string[] directories = Directory.GetDirectories(rootPath, "*", SearchOption.AllDirectories);
            foreach (string directory in directories)
            {
                string[] infoFiles = Directory.GetFiles(directory, "*_modules_info.txt", SearchOption.TopDirectoryOnly);
                foreach (string infoFile in infoFiles)
                {
                    result[infoFile] = directory;
                }
            }

            return result;
        }

        private static void CleanInfoFile(string infoFilePath, string relatedDirectory)
        {
            HashSet<string> existingModules = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (Directory.Exists(relatedDirectory))
            {
                string[] directories = Directory.GetDirectories(relatedDirectory, "*", SearchOption.TopDirectoryOnly);
                foreach (string dir in directories)
                {
                    string moduleName = Path.GetFileName(dir);
                    if (moduleName.EndsWith("Module", StringComparison.OrdinalIgnoreCase))
                    {
                        existingModules.Add(moduleName);
                    }
                }
            }

            string[] lines = File.ReadAllLines(infoFilePath);
            List<string> updatedLines = new List<string>();
            bool hasChanges = false;

            foreach (string line in lines)
            {
                string trimmedLine = line.Trim();
                
                if (trimmedLine.Equals("Modules:", StringComparison.OrdinalIgnoreCase))
                {
                    updatedLines.Add(line);
                    continue;
                }
                
                if (existingModules.Contains(trimmedLine))
                {
                    updatedLines.Add(line);
                }
                else
                {
                    hasChanges = true;
                    RemovedObsoleteModules.Add($"{trimmedLine} from {Path.GetFileName(infoFilePath)}");
                    Debug.LogWarning($"Removed obsolete module '{trimmedLine}' from '{Path.GetFileName(infoFilePath)}'.");
                }
            }

            if (hasChanges)
            {
                File.WriteAllLines(infoFilePath, updatedLines);
                Debug.Log($"Updated {Path.GetFileName(infoFilePath)}: Removed obsolete entries.");
                AssetDatabase.Refresh();
            }
        }

        private static void ShowCleaningSummaryPopup()
        {
            string removedModulesList = string.Join("\n", RemovedObsoleteModules);
            bool showPopup = EditorUtility.DisplayDialog(
                "Module Cleaning Summary",
                "The following obsolete module entries were removed:\n\n" +
                removedModulesList,
                "OK"
            );

            if (showPopup)
            {
                RemovedObsoleteModules.Clear();
                AssetDatabase.Refresh();
            }
        }
    }
}
#endif