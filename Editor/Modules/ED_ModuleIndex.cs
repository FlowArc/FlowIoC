#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FlowIoC.Editor.Modules
{
    /// <summary>
    /// The whole module graph of the project, in one asset. This is a cache and never a source
    /// of truth: everything in it except the folder GUIDs is derivable from the folder tree, so
    /// a stale or conflicted index is fixed by rebuilding it rather than by hand-editing.
    /// </summary>
    internal class ED_ModuleIndex : ScriptableObject
    {
        [SerializeField] private List<ModuleDescriptorEVO> _modules = new List<ModuleDescriptorEVO>();

        public IReadOnlyList<ModuleDescriptorEVO> Modules => _modules;

        public bool TryGetByFolderGuid(string folderGuid, out ModuleDescriptorEVO module)
        {
            module = string.IsNullOrEmpty(folderGuid)
                ? null
                : _modules.FirstOrDefault(m => string.Equals(m.FolderGuid, folderGuid, StringComparison.Ordinal));

            return module != null;
        }

        public bool TryGetByName(string name, out ModuleDescriptorEVO module)
        {
            module = string.IsNullOrEmpty(name)
                ? null
                : _modules.FirstOrDefault(m => string.Equals(m.Name, name, StringComparison.OrdinalIgnoreCase));

            return module != null;
        }

        public void Replace(IEnumerable<ModuleDescriptorEVO> descriptors)
        {
            _modules = descriptors == null ? new List<ModuleDescriptorEVO>() : descriptors.ToList();
        }

        public void Remove(string folderGuid)
        {
            _modules.RemoveAll(m => string.Equals(m.FolderGuid, folderGuid, StringComparison.Ordinal));
        }
    }
}

#endif
