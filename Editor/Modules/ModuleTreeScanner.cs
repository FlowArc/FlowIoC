#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;

namespace FlowIoC.Editor.Modules
{
    internal class ScannedModule
    {
        public string Name;
        public ModuleKind Kind;
        public string AbsolutePath;
    }

    /// <summary>
    /// Derives the module list from the folder tree. A module is a folder whose name ends in
    /// "Module"; its kind is the name of the folder it sits in. Nothing is read from disk
    /// beyond the folder names themselves, which is the point: the marker files this replaces
    /// only ever held what this method computes.
    /// </summary>
    internal class ModuleTreeScanner
    {
        private const string ModuleSuffix = "Module";

        private readonly ModuleKindResolver _kindResolver;

        public ModuleTreeScanner(ModuleKindResolver kindResolver)
        {
            _kindResolver = kindResolver;
        }

        public List<ScannedModule> Scan(string modulesRootAbsolutePath)
        {
            var found = new List<ScannedModule>();
            if (string.IsNullOrEmpty(modulesRootAbsolutePath) || !Directory.Exists(modulesRootAbsolutePath))
                return found;

            Walk(modulesRootAbsolutePath, found);
            return found;
        }

        private void Walk(string directory, List<ScannedModule> found)
        {
            foreach (string child in Directory.GetDirectories(directory))
            {
                string name = Path.GetFileName(child);

                if (name.EndsWith(ModuleSuffix, StringComparison.OrdinalIgnoreCase))
                {
                    found.Add(new ScannedModule
                    {
                        Name = name,
                        Kind = _kindResolver.Resolve(Path.GetFileName(directory)),
                        AbsolutePath = child
                    });
                }

                Walk(child, found);
            }
        }
    }
}

#endif
