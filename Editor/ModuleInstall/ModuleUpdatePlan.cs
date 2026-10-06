#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using FlowIoC.Editor.ModuleCards;

namespace FlowIoC.Editor.ModuleInstall
{
    /// <summary>
    /// Gives every file under an installed module one verdict, from three readings: the shipped
    /// record, the installed record, and the installed files. Nothing is written here - the plan
    /// is what the dialog shows before anything is, and what the updater then applies.
    ///
    /// Where both sides changed a file, its kind decides. Code is the package's: its version
    /// replaces the game's edit, and the dialog lists it first. Data may be the game's own work -
    /// a CD_Ads with its ad unit ids, a screen prefab dressed for the game - so the reader chooses
    /// for each file. A file the shipped card names on an Extend line is read as data, code or
    /// not. A meta goes the way its file goes. A file the game added is the game's, whatever its
    /// kind, and is never written over unasked.
    ///
    /// A module installed before it kept a record gets the only reading that is honest without
    /// one: a file that differs from the shipped copy was changed by somebody nobody can name, and
    /// is treated as both sides having changed it.
    ///
    /// A sub module the package removed - or moved, which reads at the old path as removed - goes
    /// whole. File by file it would not: FlowIoC's own tools rewrite an asmdef and a card in the
    /// project, so they read as the game's edits and would stay, and its generated parts are not
    /// recorded at all. Left behind, they declare an assembly the moved copy declares too, and
    /// hold the GUIDs the moved copy's generated parts ship with.
    /// </summary>
    internal class ModuleUpdatePlan
    {
        private const string CARD = "MODULE.md";
        private const string META = ".meta";

        private static readonly string[] _codeExtensions =
            {".cs", ".asmdef", ".asmref", ".shader", ".cginc", ".hlsl", ".compute"};

        private readonly ShippedRecord _record = new ShippedRecord();
        private readonly HashSet<string> _extendable = new HashSet<string>(StringComparer.Ordinal);

        internal ModuleUpdatePlanEVO Build(string installedFolder, string shippedFolder)
        {
            ShippedRecordEVO shipped = _record.Read(shippedFolder) ?? _record.Build(shippedFolder);
            ShippedRecordEVO installed = _record.Read(installedFolder);

            // What the package hands the game to extend is the package's word, so the shipped card says.
            _extendable.Clear();
            _extendable.UnionWith(new ModuleCardExtendLine().Read(new ModuleCardFile().Read(shippedFolder)));

            var plan = new ModuleUpdatePlanEVO {HadRecord = installed != null};

            if (installed != null)
                plan.RemovedModules.AddRange(RemovedModules(installed, shipped));

            var paths = new SortedSet<string>(StringComparer.Ordinal);
            paths.UnionWith(shipped.Files.Keys);
            paths.UnionWith(_record.FilesUnder(installedFolder));

            if (installed != null)
                paths.UnionWith(installed.Files.Keys);

            var raw = new Dictionary<string, ModuleUpdateVerdict>(StringComparer.Ordinal);
            var present = new HashSet<string>(StringComparer.Ordinal);
            var additions = new HashSet<string>(StringComparer.Ordinal);

            foreach (string path in paths)
            {
                shipped.Files.TryGetValue(path, out string theirs);
                string mine = _record.HashOf(installedFolder, path);
                string was = null;

                installed?.Files.TryGetValue(path, out was);

                raw[path] = installed == null ? WithoutRecord(theirs, mine) : Verdict(theirs, was, mine);

                if (mine != null)
                    present.Add(path);

                // The package adds a file where the game already has one of its own: whatever its
                // kind, the file there is not an edit of the package's.
                if (installed != null && was == null && raw[path] == ModuleUpdateVerdict.Conflict)
                    additions.Add(path);
            }

            // Files first, so a meta can read the verdict of the file it belongs to.
            foreach (string path in paths)
            {
                if (!IsMeta(path))
                    plan.Verdicts[path] = ForFile(path, raw[path], present.Contains(path), additions.Contains(path),
                        plan.RemovedModules);
            }

            foreach (string path in paths)
            {
                if (IsMeta(path))
                    plan.Verdicts[path] = ForMeta(path, raw[path], present.Contains(path), plan);
            }

            foreach (string path in plan.Of(ModuleUpdateVerdict.Conflict))
            {
                if (!shipped.Files.ContainsKey(path))
                    plan.Unshipped.Add(path);
            }

            foreach (string generated in _record.GeneratedFilesUnder(shippedFolder))
                plan.Verdicts[generated] = ModuleUpdateVerdict.Regenerate;

            foreach (string generated in _record.GeneratedFilesUnder(installedFolder))
            {
                if (!plan.Verdicts.ContainsKey(generated) && InRemovedModule(generated, plan.RemovedModules))
                    plan.Verdicts[generated] = ModuleUpdateVerdict.Delete;
            }

            return plan;
        }

        /// <summary>
        /// Where both sides changed a file, code takes the package's side and data is asked about.
        /// A Conflict over a file the package only now adds stays a Conflict whatever its kind: the
        /// file there is the game's own, not an edit of the package's. An asmdef or a card is
        /// rewritten in the project by FlowIoC's own tools, so a difference in one is no edit of
        /// the game's to list: it simply takes the package's version.
        /// </summary>
        private ModuleUpdateVerdict ForFile(string path, ModuleUpdateVerdict raw, bool present,
            bool gameAddition, IReadOnlyList<string> removedModules)
        {
            if (InRemovedModule(path, removedModules))
                return InRemovedModule(path, raw, present);

            bool code = IsCode(path);
            bool structure = IsStructure(path);

            switch (raw)
            {
                case ModuleUpdateVerdict.Conflict when code && !gameAddition:
                    return structure ? ModuleUpdateVerdict.Overwrite : ModuleUpdateVerdict.Replace;

                case ModuleUpdateVerdict.RemoveEdited:
                    return !code ? ModuleUpdateVerdict.Conflict : structure ? ModuleUpdateVerdict.Delete : raw;

                default:
                    return raw;
            }
        }

        /// <summary>
        /// A meta follows its file. With the file in conflict it goes whichever way the reader
        /// sends the file; with the file taking the package's version a meta both changed takes
        /// the package's too; with the file staying the game's, so does its meta - import
        /// settings are the game's as much as the file is. A folder's meta, which carries nothing
        /// but its GUID, is the package's.
        /// </summary>
        private ModuleUpdateVerdict ForMeta(string path, ModuleUpdateVerdict raw, bool present,
            ModuleUpdatePlanEVO plan)
        {
            if (InRemovedModule(path, plan.RemovedModules))
                return present ? ModuleUpdateVerdict.Delete : raw;

            string asset = path.Substring(0, path.Length - META.Length);

            if (!plan.Verdicts.TryGetValue(asset, out ModuleUpdateVerdict file))
                return Resolved(raw);

            switch (file)
            {
                case ModuleUpdateVerdict.Conflict:
                    return ModuleUpdateVerdict.WithAsset;

                case ModuleUpdateVerdict.Overwrite:
                case ModuleUpdateVerdict.Copy:
                case ModuleUpdateVerdict.Replace:
                    return Resolved(raw);

                case ModuleUpdateVerdict.Delete:
                case ModuleUpdateVerdict.RemoveEdited:
                    return present ? ModuleUpdateVerdict.Delete : raw;

                default:
                    if (raw == ModuleUpdateVerdict.Conflict || raw == ModuleUpdateVerdict.RemoveEdited)
                        return IsCode(asset) ? Resolved(raw) : ModuleUpdateVerdict.Leave;

                    return raw;
            }
        }

        /// <summary>A meta taking the package's side: its version where it ships one, none where it does not.</summary>
        private static ModuleUpdateVerdict Resolved(ModuleUpdateVerdict raw)
        {
            switch (raw)
            {
                case ModuleUpdateVerdict.Conflict:
                    return ModuleUpdateVerdict.Overwrite;

                case ModuleUpdateVerdict.RemoveEdited:
                    return ModuleUpdateVerdict.Delete;

                default:
                    return raw;
            }
        }

        /// <summary>
        /// Every sub module whose card the installed record lists and under which the package now
        /// ships nothing. Read off the records, so a sub module the game made inside an installed
        /// module is never one of them.
        /// </summary>
        private static IEnumerable<string> RemovedModules(ShippedRecordEVO installed, ShippedRecordEVO shipped)
        {
            foreach (string path in installed.Files.Keys)
            {
                if (!path.EndsWith("/" + CARD, StringComparison.Ordinal))
                    continue;

                string folder = path.Substring(0, path.Length - CARD.Length - 1);

                if (!ShipsUnder(shipped, folder))
                    yield return folder;
            }
        }

        private static bool ShipsUnder(ShippedRecordEVO shipped, string folder)
        {
            foreach (string path in shipped.Files.Keys)
            {
                if (path.StartsWith(folder + "/", StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        private static bool InRemovedModule(string path, IReadOnlyList<string> removedModules)
        {
            foreach (string folder in removedModules)
            {
                if (path.StartsWith(folder + "/", StringComparison.Ordinal) || path == folder + META)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Inside a sub module the package removed, what only makes it a module - its asmdefs, its
        /// card - goes whatever the tools did to it, code the game edited goes as listed, and data
        /// the game edited is asked about. A file the game added there is its own and stays.
        /// </summary>
        private ModuleUpdateVerdict InRemovedModule(string path, ModuleUpdateVerdict raw, bool present)
        {
            if (!present || raw == ModuleUpdateVerdict.Leave || raw == ModuleUpdateVerdict.Delete)
                return raw;

            if (IsStructure(path))
                return ModuleUpdateVerdict.Delete;

            return IsCode(path) ? ModuleUpdateVerdict.RemoveEdited : ModuleUpdateVerdict.Conflict;
        }

        /// <summary>What makes a folder a module, and what FlowIoC's own tools rewrite in place.</summary>
        private static bool IsStructure(string path) =>
            path.EndsWith(".asmdef", StringComparison.Ordinal)
            || path.EndsWith(".asmref", StringComparison.Ordinal)
            || path == CARD
            || path.EndsWith("/" + CARD, StringComparison.Ordinal);

        private static bool IsMeta(string path) => path.EndsWith(META, StringComparison.Ordinal);

        /// <summary>
        /// Scripts, assembly files, shaders and the card: the package's, whoever edited them -
        /// except a file the shipped card hands the game to extend, which is read as data.
        /// </summary>
        private bool IsCode(string path)
        {
            if (_extendable.Contains(path))
                return false;

            if (path == CARD || path.EndsWith("/" + CARD, StringComparison.Ordinal))
                return true;

            string extension = Path.GetExtension(path);

            foreach (string code in _codeExtensions)
            {
                if (string.Equals(extension, code, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Null stands for "not there": theirs null is a file the package no longer ships, was
        /// null one it did not ship before, mine null one the game does not have.
        /// </summary>
        private static ModuleUpdateVerdict Verdict(string theirs, string was, string mine)
        {
            // Neither shipped it: the game's own file.
            if (theirs == null && was == null)
                return ModuleUpdateVerdict.Leave;

            // The package added it.
            if (was == null)
            {
                if (mine == null) return ModuleUpdateVerdict.Copy;
                return mine == theirs ? ModuleUpdateVerdict.Leave : ModuleUpdateVerdict.Conflict;
            }

            // The package removed it.
            if (theirs == null)
            {
                if (mine == null) return ModuleUpdateVerdict.Leave;
                return mine == was ? ModuleUpdateVerdict.Delete : ModuleUpdateVerdict.RemoveEdited;
            }

            bool packageChanged = theirs != was;
            bool gameChanged = mine != was;

            if (!packageChanged)
                return gameChanged ? ModuleUpdateVerdict.KeepEdited : ModuleUpdateVerdict.Leave;

            if (!gameChanged)
                return ModuleUpdateVerdict.Overwrite;

            return mine == theirs ? ModuleUpdateVerdict.Leave : ModuleUpdateVerdict.Conflict;
        }

        private static ModuleUpdateVerdict WithoutRecord(string theirs, string mine)
        {
            if (theirs == null) return ModuleUpdateVerdict.Leave;
            if (mine == null) return ModuleUpdateVerdict.Copy;

            return mine == theirs ? ModuleUpdateVerdict.Leave : ModuleUpdateVerdict.Conflict;
        }
    }
}

#endif