#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using FlowIoC.Editor.AgentRules;

namespace FlowIoC.Editor.AgentSkills
{
    /// <summary>
    /// Copies the skills the package ships into the consumer project's .claude/skills folder,
    /// one folder per skill. The project root is injected rather than read from
    /// Application.dataPath so the whole thing can be exercised against a temporary directory
    /// in tests.
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

        private SyncFileState[] Run(bool write)
        {
            if (!_source.TryList(out string[] skills, out string error))
                return new[] { new SyncFileState(_source.Root, SyncStatus.Failed, error) };

            if (skills.Length == 0)
                return Array.Empty<SyncFileState>();

            var states = new List<SyncFileState>();

            foreach (string skill in skills)
                states.Add(Process(skill, write));

            return states.ToArray();
        }

        private SyncFileState Process(string skillFolder, bool write)
        {
            string name = Path.GetFileName(skillFolder);
            string target = Path.Combine(_projectRoot, TargetFolder.Replace('/', Path.DirectorySeparatorChar), name);

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
                string relative = file.Substring(sourceFolder.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                string copy = Path.Combine(targetFolder, relative);

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
                string relative = file.Substring(sourceFolder.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                string copy = Path.Combine(targetFolder, relative);

                Directory.CreateDirectory(Path.GetDirectoryName(copy) ?? targetFolder);
                File.Copy(file, copy, true);
            }
        }

        /// <summary>A skill written out on Windows must not read as stale on macOS, or the reverse.</summary>
        private string Normalize(string text) => text.Replace("\r\n", "\n").Replace("\r", "\n");
    }
}

#endif
