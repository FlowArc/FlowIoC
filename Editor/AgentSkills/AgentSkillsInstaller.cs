#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using FlowIoC.Editor.AgentRules;

namespace FlowIoC.Editor.AgentSkills
{
    /// <summary>
    /// Copies the skills the package ships into the consumer project's .claude/skills folder,
    /// one folder per skill, and takes them out again when the package leaves. The project root
    /// is injected rather than read from Application.dataPath so the whole thing can be
    /// exercised against a temporary directory in tests.
    ///
    /// One rule runs through every method here: only the files the package owns are compared,
    /// written or deleted. Anything the consumer put in that folder is theirs.
    /// </summary>
    internal class AgentSkillsInstaller
    {
        internal const string TargetFolder = ".claude/skills";

        private readonly string _projectRoot;
        private readonly AgentSkillsSource _source;

        internal AgentSkillsInstaller(string projectRoot, AgentSkillsSource source)
        {
            _projectRoot = projectRoot;
            _source = source;
        }

        internal SyncFileState[] Inspect() => Run(false);

        internal SyncFileState[] Install() => Run(true);

        /// <summary>
        /// Takes the shipped skills back out. Called while FlowIoC is being uninstalled, so it
        /// reads the file list off the package before it goes: what is not on that list was not
        /// ours to delete.
        /// </summary>
        internal SyncFileState[] Uninstall()
        {
            if (!_source.TryList(out string[] skills, out string error))
                return new[] {new SyncFileState(_source.Root, SyncStatus.Failed, error)};

            var states = new List<SyncFileState>();

            foreach (string skill in skills)
            {
                string target = TargetOf(Path.GetFileName(skill));

                if (!Directory.Exists(target))
                    continue;

                try
                {
                    Remove(skill, target);
                    states.Add(new SyncFileState(target, SyncStatus.Absent));
                }
                catch (Exception exception)
                {
                    states.Add(new SyncFileState(target, SyncStatus.Failed, exception.Message));
                }
            }

            TryRemoveIfEmpty(SkillsRoot);

            return states.ToArray();
        }

        private SyncFileState[] Run(bool write)
        {
            if (!_source.TryList(out string[] skills, out string error))
                return new[] {new SyncFileState(_source.Root, SyncStatus.Failed, error)};

            if (skills.Length == 0)
                return Array.Empty<SyncFileState>();

            var states = new List<SyncFileState>();

            foreach (string skill in skills)
                states.Add(Process(skill, write));

            return states.ToArray();
        }

        private SyncFileState Process(string skillFolder, bool write)
        {
            string target = TargetOf(Path.GetFileName(skillFolder));

            try
            {
                if (!Directory.Exists(target))
                {
                    if (!write)
                        return new SyncFileState(target, SyncStatus.Absent);

                    Copy(skillFolder, target);
                    return new SyncFileState(target, SyncStatus.Current);
                }

                if (IsCurrent(skillFolder, target))
                    return new SyncFileState(target, SyncStatus.Current);

                if (!write)
                    return new SyncFileState(target, SyncStatus.Stale);

                Copy(skillFolder, target);
                return new SyncFileState(target, SyncStatus.Current);
            }
            catch (Exception exception)
            {
                return new SyncFileState(target, SyncStatus.Failed, exception.Message);
            }
        }

        /// <summary>
        /// Current means every shipped file is present with the same text. A file the consumer
        /// added beside them is left alone and does not make the skill stale - only what the
        /// package owns is compared.
        /// </summary>
        private bool IsCurrent(string sourceFolder, string targetFolder)
        {
            foreach (string file in Directory.GetFiles(sourceFolder, "*", SearchOption.AllDirectories))
            {
                string copy = Path.Combine(targetFolder, RelativeTo(sourceFolder, file));

                if (!File.Exists(copy))
                    return false;

                if (!string.Equals(Normalize(File.ReadAllText(file)), Normalize(File.ReadAllText(copy)), StringComparison.Ordinal))
                    return false;
            }

            return true;
        }

        private void Copy(string sourceFolder, string targetFolder)
        {
            Directory.CreateDirectory(targetFolder);

            foreach (string file in Directory.GetFiles(sourceFolder, "*", SearchOption.AllDirectories))
            {
                string copy = Path.Combine(targetFolder, RelativeTo(sourceFolder, file));

                Directory.CreateDirectory(Path.GetDirectoryName(copy) ?? targetFolder);
                File.Copy(file, copy, true);
            }
        }

        /// <summary>
        /// Deletes what the package put there and nothing else. A note the consumer left beside
        /// a shipped skill keeps its folder alive, because taking that would be taking their work.
        /// </summary>
        private void Remove(string sourceFolder, string targetFolder)
        {
            foreach (string file in Directory.GetFiles(sourceFolder, "*", SearchOption.AllDirectories))
            {
                string copy = Path.Combine(targetFolder, RelativeTo(sourceFolder, file));

                if (File.Exists(copy))
                    File.Delete(copy);
            }

            RemoveEmptyFolders(targetFolder);
        }

        private void RemoveEmptyFolders(string folder)
        {
            foreach (string child in Directory.GetDirectories(folder))
                RemoveEmptyFolders(child);

            TryRemoveIfEmpty(folder);
        }

        /// <summary>
        /// An uninstall should not leave its own empty shells behind, but a folder that still
        /// holds something - a skill of the consumer's own, in the case of .claude/skills -
        /// stays exactly where it is.
        /// </summary>
        private void TryRemoveIfEmpty(string folder)
        {
            if (!Directory.Exists(folder))
                return;

            if (Directory.GetFileSystemEntries(folder).Length > 0)
                return;

            Directory.Delete(folder);
        }

        private string SkillsRoot =>
            Path.Combine(_projectRoot, TargetFolder.Replace('/', Path.DirectorySeparatorChar));

        private string TargetOf(string skillName) => Path.Combine(SkillsRoot, skillName);

        private string RelativeTo(string folder, string file) =>
            file.Substring(folder.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        /// <summary>A skill written out on Windows must not read as stale on macOS, or the reverse.</summary>
        private string Normalize(string text) => text.Replace("\r\n", "\n").Replace("\r", "\n");
    }
}

#endif