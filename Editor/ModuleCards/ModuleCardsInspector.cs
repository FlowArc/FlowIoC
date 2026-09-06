#if UNITY_EDITOR

using System.Collections.Generic;
using System.IO;
using FlowIoC.Editor.AgentRules;
using FlowIoC.Editor.ModuleScanner;

namespace FlowIoC.Editor.ModuleCards
{
    /// <summary>
    /// What the Agent Scanner shows for the cards: the directory, then one row per module. It
    /// answers in SyncFileState so the panel draws these rows the way it draws the rule files and
    /// the skills, and it reads the same checks the refresher does rather than deciding again.
    ///
    /// A module whose purpose has not been written is reported as Malformed rather than Stale.
    /// The word is the panel's, and what it means there is exactly right: pressing Sync cannot
    /// produce that sentence, so the row must not be counted among the files Sync would write.
    /// </summary>
    internal class ModuleCardsInspector
    {
        private readonly string _projectRoot;

        internal ModuleCardsInspector(string projectRoot)
        {
            _projectRoot = projectRoot;
        }

        internal SyncFileState[] Inspect()
        {
            var states = new List<SyncFileState> {DirectoryState()};

            foreach (ModuleCardEntryEVO entry in new ModuleCardEntryCollector().Collect(_projectRoot))
                states.Add(CardState(entry));

            return states.ToArray();
        }

        private SyncFileState DirectoryState()
        {
            string path = new ModuleDirectoryFile().PathFor(_projectRoot);

            FindingEVO finding = new ModuleDirectoryCheck()
                .Inspect(new ProjectTargetEVO {ProjectRoot = _projectRoot});

            return finding.Status == ModuleCheckStatus.Ok
                ? new SyncFileState(path, SyncStatus.Current)
                : new SyncFileState(path, SyncStatus.Stale, finding.Message);
        }

        /// <summary>
        /// The row is named after the module rather than after the file, because every card is
        /// called MODULE.md and a column of that name says nothing.
        /// </summary>
        private SyncFileState CardState(ModuleCardEntryEVO entry)
        {
            string path = Path.Combine(_projectRoot, entry.RelativePath);

            // Absent is what Sync writes. A framework module is not a scanner target, so its card
            // is a person's job and saying otherwise would leave Sync lit by something pressing
            // it cannot fix.
            if (!entry.HasCard)
            {
                return entry.ScannerOwned
                    ? new SyncFileState(path, SyncStatus.Absent, "no card yet")
                    : new SyncFileState(path, SyncStatus.Malformed, "no card yet - write it by hand");
            }

            if (string.IsNullOrEmpty(entry.Purpose))
                return new SyncFileState(path, SyncStatus.Malformed, "purpose not written yet");

            if (string.IsNullOrEmpty(entry.Concepts))
                return new SyncFileState(path, SyncStatus.Malformed, "concepts not written yet");

            return new SyncFileState(path, SyncStatus.Current);
        }
    }
}

#endif