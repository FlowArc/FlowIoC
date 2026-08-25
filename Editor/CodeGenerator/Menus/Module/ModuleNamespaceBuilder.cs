#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;

namespace FlowIoC.Editor.CodeGenerator.Menus.Module
{
    /// <summary>
    /// Assembles a module's C# namespace from its chain of ancestor modules. A module nested
    /// inside a module nests its namespace too - "Modules.Outer.Inner", not two flat entries -
    /// which is why this is more than a straight string.Join. It has no Unity API in it, which
    /// is what makes it worth testing on its own rather than only through the tool that calls it.
    /// </summary>
    internal class ModuleNamespaceBuilder
    {
        private const string ROOT_NAMESPACE = "Modules";

        /// <summary>
        /// <paramref name="ancestorNamesNearestFirst"/> must already be nearest-ancestor-first -
        /// the order ModuleRegistry.AncestorsOf returns. This reverses that to root-first and
        /// appends <paramref name="moduleName"/>, the module itself, last.
        /// </summary>
        public string Build(IEnumerable<string> ancestorNamesNearestFirst, string moduleName)
        {
            List<string> names = ancestorNamesNearestFirst?.ToList() ?? new List<string>();
            names.Reverse();

            if (!string.IsNullOrEmpty(moduleName))
                names.Add(moduleName);

            return ROOT_NAMESPACE + "." + string.Join(".", names);
        }
    }
}
#endif
