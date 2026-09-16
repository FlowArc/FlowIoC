#if UNITY_EDITOR

using System;
using System.Collections.Generic;

namespace FlowIoC.Editor.ModuleInstall
{
    /// <summary>
    /// Gives every file under an installed module one verdict, from three readings: the shipped
    /// record, the installed record, and the installed files. Nothing is written here - the plan
    /// is what the dialog shows before anything is, and what the updater then applies.
    ///
    /// A module installed before it kept a record gets the only reading that is honest without
    /// one: a file that differs from the shipped copy is a conflict, because nobody can say who
    /// changed it.
    /// </summary>
    internal class ModuleUpdatePlan
    {
        private readonly ShippedRecord _record = new ShippedRecord();

        internal ModuleUpdatePlanEVO Build(string installedFolder, string shippedFolder)
        {
            ShippedRecordEVO shipped = _record.Read(shippedFolder) ?? _record.Build(shippedFolder);
            ShippedRecordEVO installed = _record.Read(installedFolder);

            var plan = new ModuleUpdatePlanEVO {HadRecord = installed != null};

            var paths = new SortedSet<string>(StringComparer.Ordinal);
            paths.UnionWith(shipped.Files.Keys);
            paths.UnionWith(_record.FilesUnder(installedFolder));

            if (installed != null)
                paths.UnionWith(installed.Files.Keys);

            foreach (string path in paths)
            {
                shipped.Files.TryGetValue(path, out string theirs);
                string mine = _record.HashOf(installedFolder, path);

                if (installed == null)
                {
                    plan.Verdicts[path] = WithoutRecord(theirs, mine);
                    continue;
                }

                installed.Files.TryGetValue(path, out string was);
                plan.Verdicts[path] = Verdict(theirs, was, mine);
            }

            foreach (string generated in _record.GeneratedFilesUnder(shippedFolder))
                plan.Verdicts[generated] = ModuleUpdateVerdict.Regenerate;

            return plan;
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
                return mine == was ? ModuleUpdateVerdict.Delete : ModuleUpdateVerdict.KeepRemoved;
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
