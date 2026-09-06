#if UNITY_EDITOR

using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FlowIoC.Editor.CodeGenerator.Menus.Module.ModuleGeneration
{
    /// <summary>
    /// Which of a module's Root prefabs a generated screen context is attached to.
    ///
    /// This used to be "the first prefab under the parent's Prefabs folder that carries a RootBase",
    /// and MainModule/Prefabs holds MainRoot.prefab beside PoolServiceRoot.prefab. The right one was
    /// chosen only because the filesystem returned it first; a parent whose folder held another Root
    /// earlier in that order would have had its screen attached to the wrong Root, with no warning
    /// and nothing downstream able to tell.
    ///
    /// The match is on the name the module starts with rather than on a whole file name, because a
    /// Root carries the suffix of what it roots: CounterModule holds CounterServiceRoot and
    /// PlayerModule holds PlayerSystemRoot. Where the folder is ambiguous nothing is attached and
    /// the caller says so - being silently wrong is worse than leaving the step to Add Sub Context.
    ///
    /// Only paths are handled here. Whether a prefab carries a RootBase is the caller's question,
    /// because answering it means loading the asset.
    /// </summary>
    internal class ParentRootPrefabPick
    {
        private const string MODULE_SUFFIX = "Module";
        private const string ROOT_SUFFIX = "Root";

        /// <summary>
        /// The Root prefab that belongs to this module, or null with a refusal explaining why not.
        /// <paramref name="rootPrefabPaths"/> is every prefab under the module that carries a Root.
        /// </summary>
        internal string From(IReadOnlyList<string> rootPrefabPaths, string moduleName, out string refusal)
        {
            refusal = null;

            if (rootPrefabPaths == null || rootPrefabPaths.Count == 0)
            {
                refusal = "no Root prefab was found";

                return null;
            }

            if (rootPrefabPaths.Count == 1)
                return rootPrefabPaths[0];

            List<string> named = rootPrefabPaths.Where(path => IsRootOf(path, moduleName)).ToList();

            if (named.Count == 1)
                return named[0];

            refusal = named.Count == 0
                ? "none of them is named after the module: " + Names(rootPrefabPaths)
                : "more than one of them is named after the module: " + Names(named);

            return null;
        }

        /// <summary>
        /// Whether this prefab is the Root of that module: it starts with the module's own name and
        /// ends in Root, so MainRoot and PlayerSystemRoot both answer for their module while
        /// PoolServiceRoot answers for neither.
        /// </summary>
        private bool IsRootOf(string prefabPath, string moduleName)
        {
            string name = Path.GetFileNameWithoutExtension(prefabPath);
            string module = Base(moduleName);

            return !string.IsNullOrEmpty(module)
                   && name.StartsWith(module, System.StringComparison.Ordinal)
                   && name.EndsWith(ROOT_SUFFIX, System.StringComparison.Ordinal);
        }

        /// <summary>The module's name with the Module suffix off it: PlayerModule is Player.</summary>
        private string Base(string moduleName)
        {
            if (string.IsNullOrEmpty(moduleName)) return null;

            return moduleName.EndsWith(MODULE_SUFFIX, System.StringComparison.Ordinal)
                ? moduleName.Substring(0, moduleName.Length - MODULE_SUFFIX.Length)
                : moduleName;
        }

        private string Names(IEnumerable<string> paths) =>
            string.Join(", ", paths.Select(Path.GetFileNameWithoutExtension));
    }
}

#endif
