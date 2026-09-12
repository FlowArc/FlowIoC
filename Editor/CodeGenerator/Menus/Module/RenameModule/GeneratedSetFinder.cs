#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace FlowIoC.Editor.CodeGenerator.Menus.Module.RenameModule
{
    /// <summary>
    /// The classes Create Module wrote from the module's name, found where it wrote them.
    ///
    /// They are found rather than derived because a module's classes drift from its name: the
    /// Counter module was created as CounterService, so its Root is CounterServiceRoot in a folder
    /// called CounterModule, and a derived name would miss it. What is asked instead is which files
    /// in the folders the generator writes to start with the module's stem, and which identifiers
    /// declared inside them do - which takes a signal holder's nested Incoming and Outgoing with it.
    ///
    /// A file in one of those folders that does not carry the stem is somebody's own and is reported
    /// as kept, so the preview says what will not change as well as what will.
    /// </summary>
    internal class GeneratedSetFinder
    {
        private const string EXTENSION = ".cs";

        private static readonly Regex Declaration =
            new Regex(@"\b(?:class|struct|interface|enum)\s+([A-Za-z_][A-Za-z0-9_]*)");

        private readonly Func<string, IEnumerable<string>> _csFilesIn;
        private readonly Func<string, string> _readText;
        private readonly ModuleStems _stems = new ModuleStems();

        internal GeneratedSetFinder() : this(
            folder => Directory.Exists(folder)
                ? Directory.EnumerateFiles(folder, "*" + EXTENSION, SearchOption.TopDirectoryOnly)
                : Array.Empty<string>(),
            File.ReadAllText)
        {
        }

        internal GeneratedSetFinder(Func<string, IEnumerable<string>> csFilesIn, Func<string, string> readText)
        {
            _csFilesIn = csFilesIn;
            _readText = readText;
        }

        /// <summary>
        /// <paramref name="folders"/> are the folders to look in; a null one is a folder the module
        /// does not have and is skipped. <paramref name="kept"/> collects one line per file left
        /// alone.
        /// </summary>
        internal List<ClassRenameEVO> In(IEnumerable<string> folders, string oldStem, string newStem, List<string> kept)
        {
            var found = new List<ClassRenameEVO>();

            foreach (string folder in folders)
            {
                if (string.IsNullOrEmpty(folder)) continue;

                foreach (string path in _csFilesIn(folder))
                {
                    string file = Path.GetFileNameWithoutExtension(path);

                    if (!_stems.Carries(file, oldStem))
                    {
                        kept.Add(file + EXTENSION + " keeps its name: it does not start with " + oldStem + ".");

                        continue;
                    }

                    var rename = new ClassRenameEVO
                    {
                        Path = path,
                        NewPath = Path.Combine(Path.GetDirectoryName(path) ?? string.Empty,
                            _stems.Carried(file, oldStem, newStem) + EXTENSION)
                    };

                    foreach (Match match in Declaration.Matches(_readText(path)))
                    {
                        string identifier = match.Groups[1].Value;

                        if (!_stems.Carries(identifier, oldStem)) continue;
                        if (rename.Identifiers.Exists(i => i.Old == identifier)) continue;

                        rename.Identifiers.Add(new IdentifierRenameEVO
                        {
                            Old = identifier,
                            New = _stems.Carried(identifier, oldStem, newStem)
                        });
                    }

                    found.Add(rename);
                }
            }

            return found;
        }
    }
}
#endif
