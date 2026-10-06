#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FlowIoC.Editor.ModuleCards;

namespace FlowIoC.Editor.ModuleInstall
{
    /// <summary>
    /// Applies a plan to an installed module: deletes, copies, overwrites, resolves each conflict
    /// the way the reader chose for that file, then rewrites the record and the card's version
    /// line.
    ///
    /// Nothing is read here that the plan did not already read, so the dialog the plan was
    /// shown in described exactly this pass. A file that cannot be written stops the pass and is
    /// named; what was already written stays written, which is what the dialog's "commit first"
    /// is for.
    /// </summary>
    internal class ModuleUpdater
    {
        private const string META = ".meta";

        private readonly ShippedRecord _record = new ShippedRecord();
        private readonly ModuleCardVersionLine _version = new ModuleCardVersionLine();
        private readonly ModuleCardFile _card = new ModuleCardFile();
        private readonly ProjectLineEndings _lineEndings = new ProjectLineEndings();

        /// <summary>
        /// Applies the plan. takeTheirs names, by path, the conflicts the reader gave to the
        /// package; every other conflict stays the game's.
        /// </summary>
        internal bool TryApply(ModuleUpdatePlanEVO plan, string installedFolder, string shippedFolder,
            ICollection<string> takeTheirs, out string error)
        {
            error = null;

            var deleted = new List<string>();

            foreach (KeyValuePair<string, ModuleUpdateVerdict> pair in plan.Verdicts)
            {
                try
                {
                    Apply(pair.Key, pair.Value, installedFolder, shippedFolder, takeTheirs, deleted);
                }
                catch (Exception exception)
                {
                    error = $"FlowIoC could not update '{pair.Key}': {exception.Message}";
                    return false;
                }
            }

            foreach (string path in deleted)
            {
                try
                {
                    RemoveEmptiedFolders(installedFolder, shippedFolder, path);
                }
                catch (Exception exception)
                {
                    error = $"FlowIoC could not remove the folder that held '{path}': {exception.Message}";
                    return false;
                }
            }

            try
            {
                _record.Write(installedFolder, _record.Read(shippedFolder) ?? _record.Build(shippedFolder));

                // The version goes onto whichever card the pass left behind. A shipped copy with
                // no version has nothing to write, and 0.0.0 on the card would say otherwise.
                string card = _card.Read(installedFolder);
                string version = _version.Read(_card.Read(shippedFolder));

                if (card != null && version != ModuleCardVersionLine.NONE)
                    _card.Write(installedFolder, _version.Write(card, version));

                _lineEndings.Apply(Path.Combine(installedFolder, ShippedRecord.FILE_NAME));
                _lineEndings.Apply(_card.PathFor(installedFolder));
            }
            catch (Exception exception)
            {
                error = $"FlowIoC could not write the module's record: {exception.Message}";
                return false;
            }

            return true;
        }

        private void Apply(string path, ModuleUpdateVerdict verdict, string installed, string shipped,
            ICollection<string> takeTheirs, List<string> deleted)
        {
            switch (verdict)
            {
                case ModuleUpdateVerdict.Overwrite:
                case ModuleUpdateVerdict.Copy:
                case ModuleUpdateVerdict.Replace:
                case ModuleUpdateVerdict.Regenerate:
                    CopyOver(shipped, installed, path);
                    return;

                case ModuleUpdateVerdict.Delete:
                case ModuleUpdateVerdict.RemoveEdited:
                    DeleteWithMeta(installed, path, deleted);
                    return;

                case ModuleUpdateVerdict.Conflict:
                    if (takeTheirs != null && takeTheirs.Contains(path))
                        TakeTheirs(installed, shipped, path, deleted);

                    return;

                // The meta goes the way its file was sent. Its file sorts first, so a file the
                // package's side removed has already taken its meta along.
                case ModuleUpdateVerdict.WithAsset:
                    if (takeTheirs != null && takeTheirs.Contains(path.Substring(0, path.Length - META.Length)))
                        TakeTheirs(installed, shipped, path, deleted);

                    return;

                default:
                    return;
            }
        }

        /// <summary>The package's side of a conflict is the file it ships now, or no file at all.</summary>
        private void TakeTheirs(string installed, string shipped, string path, List<string> deleted)
        {
            if (File.Exists(Full(shipped, path)))
                CopyOver(shipped, installed, path);
            else
                DeleteWithMeta(installed, path, deleted);
        }

        private void CopyOver(string shipped, string installed, string path)
        {
            string target = Full(installed, path);

            Directory.CreateDirectory(Path.GetDirectoryName(target));
            File.Copy(Full(shipped, path), target, true);
            _lineEndings.Apply(target);
        }

        /// <summary>
        /// A meta is a tracked file with a verdict of its own, so it normally goes on its own
        /// line of the plan. It is deleted here as well for the one case it does not: a meta the
        /// game touched beside a file the package removed, which would otherwise stay behind and
        /// have Unity report it.
        ///
        /// The other way round, a meta whose asset stays stays with it: Unity would otherwise give
        /// the kept file, or the folder still holding the game's files, a new GUID. A folder that
        /// does end up empty takes its meta along when it is removed afterwards.
        /// </summary>
        private static void DeleteWithMeta(string installed, string path, List<string> deleted)
        {
            string target = Full(installed, path);
            bool isMeta = path.EndsWith(META, StringComparison.OrdinalIgnoreCase);

            if (isMeta)
            {
                string asset = target.Substring(0, target.Length - META.Length);

                if (File.Exists(asset))
                    return;

                // A folder holding no file at all - one a scan made and nothing filled - goes with
                // its meta; one still holding anything keeps it.
                if (Directory.Exists(asset))
                {
                    if (Directory.EnumerateFiles(asset, "*", SearchOption.AllDirectories).Any())
                        return;

                    Directory.Delete(asset, true);
                }
            }

            if (File.Exists(target))
                File.Delete(target);

            if (!isMeta && File.Exists(target + META))
                File.Delete(target + META);

            deleted.Add(path);
        }

        /// <summary>
        /// Walks up from a deleted file and removes every folder the pass left empty, with its
        /// meta, stopping at the module's root, at the first folder still holding anything, and at
        /// a folder the package still ships. Left in place, an empty folder is given a meta of a
        /// new GUID by the next import.
        /// </summary>
        private static void RemoveEmptiedFolders(string installed, string shipped, string deletedPath)
        {
            int slash = deletedPath.LastIndexOf('/');

            while (slash > 0)
            {
                string folder = deletedPath.Substring(0, slash);
                string target = Full(installed, folder);

                if (Directory.Exists(target))
                {
                    if (Directory.EnumerateFileSystemEntries(target).Any()
                        || Directory.Exists(Full(shipped, folder)))
                        return;

                    Directory.Delete(target);
                }

                if (File.Exists(target + META))
                    File.Delete(target + META);

                slash = folder.LastIndexOf('/');
            }
        }

        /// <summary>Through LongPath: the shipped side is read out of the package cache, often past 260 characters.</summary>
        private static string Full(string root, string relative) =>
            new LongPath().Of(Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar)));
    }
}

#endif