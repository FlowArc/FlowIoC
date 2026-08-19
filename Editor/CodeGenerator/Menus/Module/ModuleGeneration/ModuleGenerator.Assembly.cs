#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FlowIoC.Editor.CodeGenerator.Menus.Module.ModuleGeneration
{
    internal partial class ModuleGenerator
    {
        private static string ConvertToModuleAssemblyName(string rawName)
        {
            const string prefix = "Modules.";

            if (rawName.EndsWith("ScreenModule", StringComparison.OrdinalIgnoreCase))
            {
                string coreName = rawName.Substring(0, rawName.Length - "ScreenModule".Length);
                return prefix + coreName + ".Screen";
            }
            else if (rawName.EndsWith("TestModule", StringComparison.OrdinalIgnoreCase))
            {
                string coreName = rawName.Substring(0, rawName.Length - "TestModule".Length);
                return prefix + coreName + ".Test";
            }
            else if (rawName.EndsWith("Module", StringComparison.OrdinalIgnoreCase))
            {
                string coreName = rawName.Substring(0, rawName.Length - "Module".Length);
                return prefix + coreName;
            }

            return prefix + rawName;
        }

        private static string GetParsedAssemblyName(string rawAssemblyName)
        {
            const string prefix = "Modules.";
            string moduleName = rawAssemblyName;
            string suffix = string.Empty;

            if (rawAssemblyName.EndsWith("ScreenModule", StringComparison.OrdinalIgnoreCase))
            {
                suffix = ".Screen";
                moduleName = rawAssemblyName.Substring(0, rawAssemblyName.Length - 12);
            }
            else if (rawAssemblyName.EndsWith("TestModule", StringComparison.OrdinalIgnoreCase))
            {
                suffix = ".Test";
                moduleName = rawAssemblyName.Substring(0, rawAssemblyName.Length - 10);
            }
            else if (rawAssemblyName.EndsWith("Module", StringComparison.OrdinalIgnoreCase))
            {
                moduleName = rawAssemblyName.Substring(0, rawAssemblyName.Length - 6);
            }

            moduleName = moduleName.Trim();

            return prefix + moduleName + suffix;
        }

        private static void CreateAssemblyDefinitionFile(string oldFilePath, string rawAssemblyName, string referenceAssembly = null)
        {
            var finalAssemblyName = GetParsedAssemblyName(rawAssemblyName);

            string asmdefContent;
            if (string.IsNullOrEmpty(referenceAssembly))
            {
                asmdefContent = CreateAssemblyWithEmptyReference(finalAssemblyName);
            }
            else
            {
                asmdefContent = CreateAssemblyWithReference(referenceAssembly, finalAssemblyName);
            }

            string directory = Path.GetDirectoryName(oldFilePath) ?? "";
            string newFileName = finalAssemblyName + ".asmdef";
            string newFilePath = Path.Combine(directory, newFileName);

            File.WriteAllText(newFilePath, asmdefContent);

            if (!oldFilePath.Equals(newFilePath, StringComparison.OrdinalIgnoreCase) && File.Exists(oldFilePath))
            {
                File.Delete(oldFilePath);
                string oldAssetPath = oldFilePath;
                AssetDatabase.DeleteAsset(oldAssetPath);
                Debug.Log($"Deleted old Assembly Definition file: {oldFilePath}");
            }

            AssetDatabase.Refresh();
        }

        private static string CreateAssemblyWithReference(string referenceAssembly, string finalAssemblyName)
        {
            string asmdefContent;
            asmdefContent = $@"{{
  ""name"": ""{finalAssemblyName}"",
  ""references"": [
    ""FlowIoC"",
""{referenceAssembly}""
  ],
  ""includePlatforms"": [],
  ""excludePlatforms"": [],
  ""allowUnsafeCode"": false,
  ""overrideReferences"": false,
  ""precompiledReferences"": [],
  ""autoReferenced"": true,
  ""defineConstraints"": [],
  ""versionDefines"": [],
  ""noEngineReferences"": false
}}";
            return asmdefContent;
        }

        private static string CreateAssemblyWithEmptyReference(string finalAssemblyName)
        {
            string asmdefContent;
            asmdefContent = $@"{{
  ""name"": ""{finalAssemblyName}"",
  ""references"": [
    ""FlowIoC""
  ],
  ""includePlatforms"": [],
  ""excludePlatforms"": [],
  ""allowUnsafeCode"": false,
  ""overrideReferences"": false,
  ""precompiledReferences"": [],
  ""autoReferenced"": true,
  ""defineConstraints"": [],
  ""versionDefines"": [],
  ""noEngineReferences"": false
}}";
            return asmdefContent;
        }
    }
}
#endif