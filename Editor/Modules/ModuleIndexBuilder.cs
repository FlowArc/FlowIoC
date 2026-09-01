#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;

namespace FlowIoC.Editor.Modules
{
    /// <summary>
    /// Turns a scan into the descriptor list, carrying the previous pass's folder GUIDs
    /// forward. Previous entries are matched by folder GUID rather than by name, so renaming
    /// a module folder in the Project window does not lose its folder map.
    /// </summary>
    internal class ModuleIndexBuilder
    {
        public List<ModuleDescriptorEVO> Build(
            IReadOnlyList<ScannedModule> scanned,
            Func<string, string> folderGuidOf,
            IReadOnlyList<ModuleDescriptorEVO> previous)
        {
            var built = new List<ModuleDescriptorEVO>();
            if (scanned == null) return built;

            foreach (ScannedModule module in scanned)
            {
                string folderGuid = folderGuidOf(module.AbsolutePath);

                // A folder Unity has no GUID for is not an imported asset yet. Recording it
                // would put an entry in the index that resolves to nothing.
                if (string.IsNullOrEmpty(folderGuid)) continue;

                ModuleDescriptorEVO carried = previous?
                    .FirstOrDefault(p => string.Equals(p.FolderGuid, folderGuid, StringComparison.Ordinal));

                var descriptor = new ModuleDescriptorEVO
                {
                    Name = module.Name,
                    Kind = module.Kind,
                    FolderGuid = folderGuid
                };

                if (carried?.FolderGuids != null)
                {
                    foreach (var pair in carried.FolderGuids)
                        descriptor.RecordFolderGuid(pair.Key, pair.Value);
                }

                built.Add(descriptor);
            }

            return built;
        }
    }
}

#endif
