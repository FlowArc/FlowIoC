#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;

namespace FlowIoC.Editor.Modules
{
    /// <summary>
    /// The one place the tools ask about modules. Every question they used to answer by
    /// probing for a _module_info.txt and parsing it is answered here from the index.
    /// </summary>
    internal class ModuleRegistry
    {
        private readonly ED_ModuleIndex _index;
        private readonly IAssetPaths _assetPaths;

        public ModuleRegistry(ED_ModuleIndex index, IAssetPaths assetPaths)
        {
            _index = index;
            _assetPaths = assetPaths;
        }

        public IReadOnlyList<ModuleDescriptorEVO> Modules => _index.Modules;

        public bool IsModule(string assetPath)
        {
            return TryGetModule(assetPath, out _);
        }

        public bool TryGetModule(string assetPath, out ModuleDescriptorEVO module)
        {
            module = null;
            if (string.IsNullOrEmpty(assetPath)) return false;

            string guid = _assetPaths.GuidOf(Normalize(assetPath));
            return !string.IsNullOrEmpty(guid) && _index.TryGetByFolderGuid(guid, out module);
        }

        public bool TryGetNearestModule(string assetPath, out ModuleDescriptorEVO module)
        {
            module = null;
            string current = Normalize(assetPath);

            while (!string.IsNullOrEmpty(current))
            {
                if (TryGetModule(current, out module)) return true;

                int slash = current.LastIndexOf('/');
                if (slash <= 0) return false;

                current = current.Substring(0, slash);
            }

            return false;
        }

        public string PathOf(ModuleDescriptorEVO module)
        {
            return module == null ? string.Empty : _assetPaths.PathOf(module.FolderGuid);
        }

        public IEnumerable<ModuleDescriptorEVO> ChildrenOf(ModuleDescriptorEVO module, ModuleKind kind)
        {
            string parentPath = PathOf(module);
            if (string.IsNullOrEmpty(parentPath)) return Enumerable.Empty<ModuleDescriptorEVO>();

            string prefix = parentPath + "/";

            return _index.Modules
                .Where(m => m.Kind == kind)
                .Where(m => PathOf(m).StartsWith(prefix, StringComparison.Ordinal))
                .Where(m => IsNearestModuleUnder(m, module))
                .ToList();
        }

        public IEnumerable<ModuleDescriptorEVO> AncestorsOf(ModuleDescriptorEVO module)
        {
            string path = PathOf(module);
            if (string.IsNullOrEmpty(path)) yield break;

            int slash = path.LastIndexOf('/');
            string current = slash <= 0 ? string.Empty : path.Substring(0, slash);

            while (!string.IsNullOrEmpty(current))
            {
                if (TryGetModule(current, out ModuleDescriptorEVO ancestor))
                    yield return ancestor;

                slash = current.LastIndexOf('/');
                if (slash <= 0) yield break;

                current = current.Substring(0, slash);
            }
        }

        /// <summary>
        /// A screen module nested two modules deep is a child of the inner one, not of both.
        /// </summary>
        private bool IsNearestModuleUnder(ModuleDescriptorEVO candidate, ModuleDescriptorEVO parent)
        {
            ModuleDescriptorEVO nearest = AncestorsOf(candidate).FirstOrDefault();
            return nearest != null
                   && string.Equals(nearest.FolderGuid, parent.FolderGuid, StringComparison.Ordinal);
        }

        private string Normalize(string path)
        {
            return path?.Replace('\\', '/').TrimEnd('/');
        }
    }
}

#endif
