#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using FlowIoC.Editor.Config.ModuleConfig;

namespace FlowIoC.Editor.ModuleScanner
{
    /// <summary>
    /// A Root and the Context it roots carry the same name in front of their suffixes.
    ///
    /// Create Module writes the two together - CounterServiceRoot roots CounterServiceContext,
    /// PlayerSystemRoot roots PlayerSystemContext, MainRoot roots MainContext - and only these two
    /// names spell the role out at all: the module folder, its assembly and its namespaces are
    /// named for what the module does. Nothing at runtime reads the Context's name, so a pair that
    /// disagrees compiles and runs, and the inspector still paints the Root by its own name. What
    /// goes wrong is the reader: a Root renamed for its colour and a Context left behind says the
    /// module is two things at once, and a search for the Context by the Root's name finds nothing.
    /// That is exactly how the shipped Gameplay module sat for a while - GameplaySystemRoot over
    /// GameplayContext - and nothing said so.
    ///
    /// The pair is read off the Root's own declaration, <c>class XRoot : Root&lt;XContext&gt;</c>,
    /// which is where both names stand on one line, so a module that is not compiled - one shipped
    /// under Modules~ - answers the same as one that is. A generic base such as
    /// <c>BaseScreenTestRoot&lt;TContext&gt;</c> declares no pair of its own and is skipped.
    ///
    /// The finding is Manual. Renaming a Context reaches every file that names it - the Root, the
    /// card's Root line, a Model that injects it by name, a Help page - and which of the two names
    /// is the wrong one is a decision about the module rather than about the file.
    /// </summary>
    internal class RootContextRoleCheck : IModuleCheck
    {
        private const string ROOT_SUFFIX = "Root";
        private const string CONTEXT_SUFFIX = "Context";
        private const string ROOTS_FOLDER = "Scripts/Runtime/RootsContexts";

        /// <summary>
        /// <c>class GameplaySystemRoot : Root&lt;GameplayContext&gt;</c>. The class name must run
        /// straight into the colon, so a generic Root definition - a class name followed by a type
        /// parameter list - does not match, and the base may be any Root: <c>Root</c>,
        /// <c>BaseScreenRoot</c>, <c>BaseScreenTestRoot</c>.
        /// </summary>
        private static readonly Regex Declaration = new Regex(
            @"class\s+(?<root>\w+" + ROOT_SUFFIX + @")\s*:\s*\w*" + ROOT_SUFFIX + @"\s*<\s*(?<context>\w+)\s*>",
            RegexOptions.Compiled);

        private readonly Func<ModuleTargetEVO, IReadOnlyList<string>> _rootFilesOf;
        private readonly Func<string, string> _readFile;

        internal RootContextRoleCheck() : this(DefaultRootFilesOf, File.ReadAllText)
        {
        }

        internal RootContextRoleCheck(Func<ModuleTargetEVO, IReadOnlyList<string>> rootFilesOf, Func<string, string> readFile)
        {
            _rootFilesOf = rootFilesOf;
            _readFile = readFile;
        }

        public string Id => "root-context-role";

        public FindingEVO Inspect(ModuleTargetEVO module)
        {
            IReadOnlyList<string> files = _rootFilesOf(module);

            // A screen module has no Root of its own, and a module written without one has nothing
            // to pair. Whether a module ought to have a Root is not this check's question.
            if (files == null || files.Count == 0)
                return FindingEVO.Ok(Id, "Root and Context (no Root)");

            foreach (string file in files)
            {
                Match match = Declaration.Match(_readFile(file) ?? string.Empty);

                if (!match.Success)
                    continue;

                string root = match.Groups["root"].Value;
                string context = match.Groups["context"].Value;
                string expected = Stem(root, ROOT_SUFFIX) + CONTEXT_SUFFIX;

                if (string.Equals(context, expected, StringComparison.Ordinal))
                    continue;

                return FindingEVO.Manual(
                    Id,
                    $"{root} roots {context}. A Root and its Context carry the same name in front of the suffix, "
                    + $"so the Context of {root} is {expected} - or the Root is {Stem(context, CONTEXT_SUFFIX)}{ROOT_SUFFIX}. "
                    + "Rename one of the two, and everything that names it.",
                    AssetPathOf(module, file));
            }

            return FindingEVO.Ok(Id, "Root and Context");
        }

        public void Fix(ModuleTargetEVO module)
        {
            // Never called: the finding is Manual, and which name is the wrong one is not the
            // scanner's to decide.
        }

        private static string Stem(string name, string suffix) =>
            name.EndsWith(suffix, StringComparison.Ordinal) ? name.Substring(0, name.Length - suffix.Length) : name;

        /// <summary>
        /// The Root files directly under the module's RootsContexts folder - never a sub module's
        /// under zSubModules, which is a module of its own with a row of its own. The layout says
        /// where that folder is; a target with no layout is read at the folder the layouts all
        /// name.
        /// </summary>
        private static IReadOnlyList<string> DefaultRootFilesOf(ModuleTargetEVO module)
        {
            if (module == null || string.IsNullOrEmpty(module.AbsolutePath))
                return Array.Empty<string>();

            string folder = module.Layout != null
                ? module.Layout.FindFullFolderPathByID(FolderEVO.FolderType.RootsAndContexts, module.AbsolutePath)
                : null;

            if (string.IsNullOrEmpty(folder))
                folder = Path.Combine(module.AbsolutePath, ROOTS_FOLDER.Replace('/', Path.DirectorySeparatorChar));

            if (!Directory.Exists(folder))
                return Array.Empty<string>();

            string[] files = Directory.GetFiles(folder, "*" + ROOT_SUFFIX + ".cs", SearchOption.TopDirectoryOnly);
            Array.Sort(files, StringComparer.Ordinal);

            return files;
        }

        private static string AssetPathOf(ModuleTargetEVO module, string file)
        {
            if (string.IsNullOrEmpty(module.AssetPath) || string.IsNullOrEmpty(module.AbsolutePath)) return null;

            string relative = file.Replace('\\', '/');
            string root = module.AbsolutePath.Replace('\\', '/').TrimEnd('/');

            if (!relative.StartsWith(root, StringComparison.OrdinalIgnoreCase)) return module.AssetPath;

            return module.AssetPath.Replace('\\', '/').TrimEnd('/') + relative.Substring(root.Length);
        }
    }
}
#endif
