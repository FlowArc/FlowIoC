#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FlowIoC.Editor.CodeGenerator.Menus.Module.RenameModule;
using FlowIoC.Editor.ModuleScanner;
using UnityEngine;

namespace FlowIoC.Editor.Migration
{
    /// <summary>
    /// Carries a project from <c>FlowLogType</c> to <c>FlowModule</c>: the class of module
    /// constants was renamed when it stopped being a log type and started naming the module, and
    /// every <c>FlowLogType.PlayerModule</c> in the game's sources has to follow.
    ///
    /// The rename is what makes the migration possible at all. The package declares
    /// <c>FlowModule.Default</c>; a project's old shared file declares <c>FlowLogType.Default</c>,
    /// a different class, so the two compile side by side and this code gets to run - where a
    /// second <c>FlowLogType.Default</c> would have stopped the compile before anything could
    /// delete the older copy. Once the sources say <c>FlowModule</c>, the old parts and the old
    /// shared file are orphans and the sweep takes them.
    ///
    /// The rewrite is a whole-word replacement of the identifier, the way Rename Module rewrites a
    /// channel, so <c>FlowLogTypeGenerator</c> in a comment is left alone and nothing but the
    /// class name is touched. Generated parts are skipped: they are about to be deleted.
    /// </summary>
    internal class FlowModuleReferenceMigrator
    {
        internal const string OLD_NAME = "FlowLogType";
        internal const string NEW_NAME = "FlowModule";

        private const string SOURCE_PATTERN = "*.cs";
        private const string GENERATED_SEGMENT = "/Scripts/Generated/";

        private readonly TextRule _rule = TextRule.Token(OLD_NAME, NEW_NAME);
        private readonly SourceTextRewriter _rewriter = new SourceTextRewriter();
        private readonly TextFile _files = new TextFile();

        /// <summary>The text with every whole-word <c>FlowLogType</c> read as <c>FlowModule</c>.</summary>
        internal string Rewrite(string text, out bool changed)
        {
            return _rewriter.Rewrite(text, new[] {_rule}, out changed);
        }

        /// <summary>
        /// Whether a source file is one the rewrite leaves alone: a generated part, or the shared
        /// file, both about to go. Rewriting either would turn its <c>FlowLogType</c> into a second
        /// declaration of <c>FlowModule</c> - the shared file's <c>Default</c> against the package's
        /// - and stop the compile the deletion needs.
        /// </summary>
        internal bool IsSkipped(string path)
        {
            string normalized = Normalize(path);

            return normalized.IndexOf(GENERATED_SEGMENT, StringComparison.OrdinalIgnoreCase) >= 0
                   || Path.GetFileName(normalized).StartsWith(OLD_NAME + ".", StringComparison.Ordinal);
        }

        /// <summary>
        /// Whether the project still has anything written under the old name: the shared file an
        /// older FlowIoC wrote at either of its roots, or a module part named for the old class.
        /// Cheap once the migration has run - a few File.Exists and a directory listing per module
        /// root - so it is asked on every generator pass rather than remembered per session, which
        /// is what lets a package upgraded under an open Editor migrate at once.
        /// </summary>
        internal bool IsNeeded()
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

            foreach (string shared in new[]
                     {
                         "Assets/FlowIoC/Generated/FlowLogType.cs",
                         "Assets/Plugins/FlowIoC/Generated/FlowLogType.cs"
                     })
            {
                if (File.Exists(Path.Combine(projectRoot, shared))) return true;
            }

            foreach (string root in new ModuleScannerRoots().All(projectRoot))
            {
                if (!Directory.Exists(root)) continue;

                if (Directory.EnumerateFiles(root, OLD_NAME + ".*.cs", SearchOption.AllDirectories).Any())
                    return true;
            }

            return false;
        }

        /// <summary>Rewrites every source in the project's module roots and under Assets. Returns how many changed.</summary>
        internal int RewriteProject()
        {
            int rewritten = 0;

            foreach (string source in Sources())
            {
                if (IsSkipped(source)) continue;

                if (_files.Rewrite(source, text => Rewrite(text, out _)))
                    rewritten++;
            }

            return rewritten;
        }

        /// <summary>
        /// The same scope Rename Module rewrites: everything under Assets, and every module root a
        /// package embeds - never a folder ending in a tilde, which Unity does not compile.
        /// </summary>
        private static IEnumerable<string> Sources()
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string assets = Normalize(Application.dataPath);

            var roots = new List<string> {Application.dataPath};

            foreach (string root in new ModuleScannerRoots().All(projectRoot))
            {
                if (!Normalize(root).StartsWith(assets, StringComparison.OrdinalIgnoreCase)) roots.Add(root);
            }

            foreach (string root in roots)
            {
                if (!Directory.Exists(root)) continue;

                foreach (string path in Directory.EnumerateFiles(root, SOURCE_PATTERN, SearchOption.AllDirectories))
                {
                    if (Normalize(path).Split('/').Any(segment => segment.EndsWith("~", StringComparison.Ordinal))) continue;

                    yield return path;
                }
            }
        }

        private static string Normalize(string path) => path.Replace('\\', '/');
    }
}

#endif