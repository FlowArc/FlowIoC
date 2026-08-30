#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FlowIoC.Editor.CodeGenerator.Menus.Module.ModuleGeneration
{
    internal partial class ModuleGenerator
    {
        private static string GetParsedAssemblyName(string rawAssemblyName) =>
            new ModuleAssemblyName().From(rawAssemblyName);

        /// <summary>
        /// The assembly of the module at <paramref name="parentModulePath"/>, or null when nothing
        /// there is a module - which is the case for Assets/Modules itself, the folder every top
        /// level module is parented to.
        ///
        /// The name is read off the asmdef that is actually on disk rather than derived from the
        /// folder name, so a module whose assembly was renamed by hand is still found. Only the
        /// module's own asmdef sits at this level; its Shared one lives under Scripts/Shared.
        /// </summary>
        private static string ParentModuleAssemblyName(string parentModulePath)
        {
            if (string.IsNullOrEmpty(parentModulePath) || !Directory.Exists(parentModulePath)) return null;

            string[] asmdefFiles = Directory.GetFiles(parentModulePath, "*.asmdef", SearchOption.TopDirectoryOnly);

            return asmdefFiles.Length == 1 ? Path.GetFileNameWithoutExtension(asmdefFiles[0]) : null;
        }

        /// <summary>
        /// A module can need more than one reference now - its own Shared assembly and the Shared
        /// assembly of the module it lives in - so the references arrive as a list. Entries that
        /// are null or empty are dropped by the template rather than by the caller, because most
        /// callers are passing the result of a lookup that legitimately finds nothing.
        /// </summary>
        private static void CreateAssemblyDefinitionFile(string oldFilePath, string rawAssemblyName, params string[] referenceAssemblies)
        {
            var finalAssemblyName = GetParsedAssemblyName(rawAssemblyName);

            string asmdefContent = new AssemblyDefinitionTemplate().Build(finalAssemblyName, referenceAssemblies);

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
    }
}
#endif