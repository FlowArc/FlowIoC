#if UNITY_EDITOR

using System.Reflection;
using UnityEditor.PackageManager;

namespace FlowIoC.Editor.Help
{
    /// <summary>
    /// Which category of the Help window a module page belongs to: the package its assembly is
    /// compiled from, under the title that package chose or the one its name gives it. Two pages
    /// from one package share a key and land in one category, and a page from a package FlowIoC
    /// has never heard of gets a category of its own without FlowIoC naming it anywhere.
    /// </summary>
    internal class ModuleGroup
    {
        private const string SUFFIX = " Modules";

        internal ModuleGroup(string key, string title, bool isOwn)
        {
            Key = key;
            Title = title;
            IsOwn = isOwn;
        }

        /// <summary>The package's name, or the assembly's when it is compiled from no package.</summary>
        internal string Key { get; }

        internal string Title { get; }

        /// <summary>
        /// Whether this is FlowIoC's own group. It is listed first: what the framework ships is
        /// what every project has, and the packages a project adds follow it.
        /// </summary>
        internal bool IsOwn { get; }

        /// <summary>
        /// The group of the pages an assembly declares. The title is the assembly's
        /// <see cref="ModuleGroupAttribute"/> when it carries one; otherwise the package's display
        /// name with "Modules" after it, or the assembly's own name when there is no package.
        /// </summary>
        internal static ModuleGroup Of(Assembly assembly)
        {
            PackageInfo package = PackageInfo.FindForAssembly(assembly);
            PackageInfo own = PackageInfo.FindForAssembly(typeof(ModuleGroup).Assembly);
            var declared = assembly.GetCustomAttribute<ModuleGroupAttribute>();

            string key = package != null ? package.name : assembly.GetName().Name;

            string title = declared != null
                ? declared.Title
                : (package != null ? package.displayName : assembly.GetName().Name) + SUFFIX;

            bool isOwn = package != null && own != null && package.name == own.name;

            return new ModuleGroup(key, title, isOwn);
        }
    }
}

#endif
