#if UNITY_EDITOR
using System;
using System.IO;
using UnityEngine;

namespace FlowIoC.Editor.CodeGenerator.Menus.Module
{
    /// <summary>
    /// The inverse of NamespaceUtility.GetUnityAssetPath: turns an asset path ModuleRegistry
    /// handed back into the absolute filesystem path File/Directory need. Two things reach this
    /// that a plain "strip the leading Assets" substring cannot handle safely - an empty path,
    /// and a Packages path - so this is the one place that conversion happens rather than a
    /// private copy per caller.
    /// </summary>
    internal class ModuleAssetPathResolver
    {
        /// <summary>
        /// Empty in, empty out. ModuleRegistry.PathOf returns empty for a descriptor whose
        /// FolderGuid no longer resolves to a real folder - the folder was deleted or moved
        /// outside the tool and nothing has rebuilt the index since - and callers are expected
        /// to treat that as "skip this module", not crash on it.
        ///
        /// A non-empty path is resolved against the project root rather than assumed to start
        /// with "Assets": ModuleIndexRebuilder also scans Packages/*/Modules, so PathOf can
        /// legitimately hand back a path rooted at "Packages/..." for an embedded package
        /// module. Path.Combine + Path.GetFullPath handles both roots uniformly, so a module
        /// that later shows up inside a package resolves correctly instead of silently vanishing
        /// from tools built on this method.
        /// </summary>
        public string ToAbsolutePath(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath)) return string.Empty;

            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            return Path.GetFullPath(Path.Combine(projectRoot, assetPath));
        }

        /// <summary>
        /// The other direction: ModuleRegistry works in asset paths ("Assets/Modules/HeroModule"),
        /// while the drawers and the namespace provider work in the absolute paths
        /// Directory.GetDirectories hands back. A path that is already rooted somewhere else - an
        /// embedded package module - is handed back unchanged rather than forced under "Assets".
        /// </summary>
        public string ToAssetPath(string absolutePath)
        {
            string normalized = absolutePath.Replace('\\', '/');
            string dataPath = Application.dataPath;

            return normalized.StartsWith(dataPath, StringComparison.OrdinalIgnoreCase)
                ? "Assets" + normalized.Substring(dataPath.Length)
                : normalized;
        }
    }
}
#endif