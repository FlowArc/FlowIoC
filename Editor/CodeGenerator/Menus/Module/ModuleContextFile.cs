#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace FlowIoC.Editor.CodeGenerator.Menus.Module
{
    /// <summary>
    /// The context file a module's bindings are written into. Create Command, Create Model and
    /// Create View all edit it, and they cannot assume it is called <c>PlayerContext.cs</c> any
    /// more: a module whose Root roots a System or a Service names its context for that role, so
    /// the same module may hold <c>PlayerSystemContext.cs</c> or <c>CounterServiceContext.cs</c>.
    ///
    /// The plain name is tried first, then the roles, and the file that exists wins. A module that
    /// has no context at all is handed the plain name back with a warning, so what the caller
    /// failed to open is the file it says it looked for.
    /// </summary>
    internal class ModuleContextFile
    {
        private readonly ModuleRoleNaming _naming = new ModuleRoleNaming();

        /// <summary>
        /// The context file inside <paramref name="rootsAndContextsPath"/>.
        /// <paramref name="kindSuffix"/> is what the module type adds between the name and the
        /// word Context - "Test" for a test module, "Screen" for a screen module, empty otherwise.
        /// </summary>
        public string Find(string rootsAndContextsPath, string moduleName, string kindSuffix = "")
        {
            List<string> candidates = Candidates(moduleName, kindSuffix);

            foreach (string candidate in candidates)
            {
                string path = Path.Combine(rootsAndContextsPath, candidate);

                if (File.Exists(path))
                    return path;
            }

            Debug.LogWarning($"No context found in '{rootsAndContextsPath}'. Looked for {string.Join(", ", candidates)}.");

            return Path.Combine(rootsAndContextsPath, candidates[0]);
        }

        /// <summary>
        /// The file names a module of this name may have written its context under, plain name
        /// first. Only a main module is ever offered a role, so the roles are tried after the
        /// name the generator has always written.
        /// </summary>
        public List<string> Candidates(string moduleName, string kindSuffix)
        {
            var candidates = new List<string>();

            foreach (ModuleRole role in new[] {ModuleRole.Core, ModuleRole.System, ModuleRole.Service})
            {
                string name = $"{_naming.Apply(moduleName, role)}{kindSuffix}Context.cs";

                if (!candidates.Contains(name))
                    candidates.Add(name);
            }

            return candidates;
        }
    }
}
#endif
