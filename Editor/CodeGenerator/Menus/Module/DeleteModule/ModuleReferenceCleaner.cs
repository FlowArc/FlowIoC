#if UNITY_EDITOR

using System.Collections.Generic;
using System.IO;
using FlowIoC.Editor.CodeGenerator.Menus.Module.ModuleGeneration;
using UnityEditor;

namespace FlowIoC.Editor.CodeGenerator.Menus.Module.DeleteModule
{
    /// <summary>
    /// Unwires a module from every asmdef that named it, before the module goes.
    ///
    /// Left in, the reference points at an assembly that no longer exists, and Unity refuses to
    /// compile whichever module still holds it - with an error about the asmdef rather than about
    /// the code, which takes a reader nowhere. Most of these references were never used anyway:
    /// Create Module wires a parent's Shared assembly into every screen, sub and test module under
    /// it, and most of them never read a line of that data.
    ///
    /// A module that did use the deleted types now fails on the line that used them, which is the
    /// error somebody can act on. Removing the reference is not hiding that - it is what turns an
    /// unreadable failure into a readable one.
    ///
    /// The asmdefs are found through the asset database rather than by walking the disk, so this
    /// costs an indexed query and one read per file that actually names the module.
    /// </summary>
    internal class ModuleReferenceCleaner
    {
        private const string ASMDEF_FILTER = "t:AssemblyDefinitionAsset";

        private readonly AssemblyDefinitionReferences _references = new AssemblyDefinitionReferences();

        /// <summary>
        /// <paramref name="modulePath"/> is the module about to be deleted: its own asmdefs are
        /// skipped, because they are going with it and rewriting them first would only churn files
        /// on their way to the bin.
        /// </summary>
        internal IReadOnlyList<string> Clean(string modulePath, IReadOnlyList<string> assemblyNames)
        {
            var unwired = new List<string>();

            if (assemblyNames == null || assemblyNames.Count == 0) return unwired;

            string module = Normalize(modulePath);

            foreach (string guid in AssetDatabase.FindAssets(ASMDEF_FILTER))
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(assetPath)) continue;

                string full = Normalize(Path.GetFullPath(assetPath));
                if (!string.IsNullOrEmpty(module) && full.StartsWith(module, System.StringComparison.OrdinalIgnoreCase))
                    continue;

                CleanOne(assetPath, assemblyNames, unwired);
            }

            return unwired;
        }

        private void CleanOne(string assetPath, IReadOnlyList<string> assemblyNames, List<string> unwired)
        {
            string content = File.ReadAllText(assetPath);
            string updated = content;
            var removed = new List<string>();

            foreach (string assemblyName in assemblyNames)
            {
                updated = _references.Remove(updated, assemblyName, out bool taken);

                if (taken) removed.Add(assemblyName);
            }

            if (removed.Count == 0) return;

            File.WriteAllText(assetPath, updated);

            string holder = Path.GetFileNameWithoutExtension(assetPath);

            foreach (string assemblyName in removed)
                unwired.Add($"{holder} no longer references {assemblyName}");
        }

        private string Normalize(string path) =>
            string.IsNullOrEmpty(path) ? string.Empty : path.Replace('\\', '/').TrimEnd('/');
    }
}

#endif
