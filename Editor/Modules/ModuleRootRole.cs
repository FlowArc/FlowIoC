#if UNITY_EDITOR

using System;
using System.IO;
using System.Text.RegularExpressions;
using FlowIoC.BaseModule.Attributes;
using FlowIoC.Editor.Inspector;

namespace FlowIoC.Editor.Modules
{
    /// <summary>
    /// The role a module's Root wears, read off the Root's file rather than its type. A module
    /// shipped under Modules~ is not compiled, so its Root cannot be asked the way the inspector
    /// asks one - but the file says the same thing: the attribute when the Root declares one,
    /// <c>[FlowHeader(FlowRole.Core)]</c>, and otherwise the name, split by the rule the Root's own
    /// header bar splits it by. The Help banner and the Publish badge both read this, so a module
    /// wears one colour wherever it is shown.
    ///
    /// The plain Root is no answer. It is what a Root that has not said what it roots falls back
    /// to, and a banner wearing the fallback would say the module is one of the four when it has
    /// not said so - null leaves the caller to its own default.
    /// </summary>
    internal class ModuleRootRole
    {
        private const string ROOTS_FOLDER = "Scripts/Runtime/RootsContexts";
        private const string ROOT_SUFFIX = "Root";
        private const string MODULE_SUFFIX = "Module";

        private static readonly Regex Header = new Regex(@"\[\s*FlowHeader\s*\(\s*FlowRole\s*\.\s*(\w+)");

        private readonly FlowRoleResolver _resolver = new FlowRoleResolver();

        internal FlowRole? Read(string moduleFolder)
        {
            string root = RootFileOf(moduleFolder);

            if (root == null)
                return null;

            FlowRole role = Declared(root) ?? _resolver.RoleOfRootName(Path.GetFileNameWithoutExtension(root));

            return role == FlowRole.Root ? (FlowRole?) null : role;
        }

        /// <summary>
        /// The module's own Root: the files ending in Root directly under its RootsContexts, and
        /// never a sub module's under zSubModules. When there are several, the one named after
        /// the module - AdsServiceRoot in AdsModule - is the module's; otherwise the first by name.
        /// </summary>
        private static string RootFileOf(string moduleFolder)
        {
            if (string.IsNullOrEmpty(moduleFolder))
                return null;

            string folder = Path.Combine(moduleFolder, ROOTS_FOLDER.Replace('/', Path.DirectorySeparatorChar));

            if (!Directory.Exists(folder))
                return null;

            string[] files = Directory.GetFiles(folder, "*" + ROOT_SUFFIX + ".cs", SearchOption.TopDirectoryOnly);

            if (files.Length == 0)
                return null;

            Array.Sort(files, StringComparer.Ordinal);

            string moduleName = Path.GetFileName(moduleFolder);

            if (moduleName.EndsWith(MODULE_SUFFIX))
                moduleName = moduleName.Substring(0, moduleName.Length - MODULE_SUFFIX.Length);

            foreach (string file in files)
            {
                if (Path.GetFileNameWithoutExtension(file).StartsWith(moduleName))
                    return file;
            }

            return files[0];
        }

        private static FlowRole? Declared(string rootFile)
        {
            Match match = Header.Match(File.ReadAllText(rootFile));

            if (match.Success && Enum.TryParse(match.Groups[1].Value, out FlowRole role))
                return role;

            return null;
        }
    }
}

#endif
