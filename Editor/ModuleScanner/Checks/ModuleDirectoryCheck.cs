#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using FlowIoC.BaseModule.ProjectPaths;
using FlowIoC.Editor.AgentRules;
using FlowIoC.Editor.ModuleCards;
using UnityEngine;

namespace FlowIoC.Editor.ModuleScanner
{
    /// <summary>
    /// Whether MODULES.md still describes the cards, and whether the project is still ignoring
    /// it. Both belong to the project rather than to any one module, which is why this is an
    /// IProjectCheck.
    /// </summary>
    internal class ModuleDirectoryCheck : IProjectCheck
    {
        private const string GITIGNORE = ".gitignore";

        private readonly Func<string, string> _readDirectory;
        private readonly Action<string, string> _writeDirectory;
        private readonly Func<string, string> _readGitignore;
        private readonly Action<string, string> _writeGitignore;
        private readonly Func<string, IReadOnlyList<ModuleCardEntryEVO>> _entriesOf;

        private readonly ModuleDirectoryBuilder _builder = new ModuleDirectoryBuilder();
        private readonly ModuleDirectoryIgnoreRule _ignoreRule = new ModuleDirectoryIgnoreRule();
        private readonly ManagedBlockWriter _writer = new ManagedBlockWriter(ModuleCardWriter.Style);

        internal ModuleDirectoryCheck() : this(null, null, null, null, null)
        {
        }

        internal ModuleDirectoryCheck(
            Func<string, string> readDirectory,
            Action<string, string> writeDirectory,
            Func<string, string> readGitignore,
            Action<string, string> writeGitignore,
            Func<string, IReadOnlyList<ModuleCardEntryEVO>> entriesOf)
        {
            var file = new ModuleDirectoryFile();

            _readDirectory = readDirectory ?? file.Read;
            _writeDirectory = writeDirectory ?? file.Write;
            _readGitignore = readGitignore ?? DefaultReadGitignore;
            _writeGitignore = writeGitignore ?? DefaultWriteGitignore;
            _entriesOf = entriesOf ?? (root => new ModuleCardEntryCollector().Collect(root));
        }

        public string Id => "module-directory";

        public FindingEVO Inspect(ProjectTargetEVO project)
        {
            string body = _builder.Build(_entriesOf(project.ProjectRoot));
            string existing = _readDirectory(project.ProjectRoot);

            if (existing == null)
                return FindingEVO.Fixable(Id, ModuleDirectoryFile.FILE_NAME + " has not been written yet");

            // Asked of the writer rather than of the hash, because the hash only describes what
            // the block was rendered from - a reader who edited inside the block would otherwise
            // go undetected.
            if (_writer.Write(existing, body, ModuleCardWriter.BLOCK_VERSION).Status != BlockWriteStatus.Unchanged)
                return FindingEVO.Fixable(Id, ModuleDirectoryFile.FILE_NAME + " no longer matches the module cards");

            if (!_ignoreRule.IsPresent(_readGitignore(project.ProjectRoot)))
                return FindingEVO.Fixable(Id, ModuleDirectoryFile.FILE_NAME + " is not in the project's .gitignore");

            return FindingEVO.Ok(Id, ModuleDirectoryFile.FILE_NAME);
        }

        public void Fix(ProjectTargetEVO project)
        {
            WriteIgnoreRule(project.ProjectRoot);
            WriteDirectory(project.ProjectRoot);
        }

        private void WriteIgnoreRule(string projectRoot)
        {
            BlockWriteResult rule = _ignoreRule.Apply(_readGitignore(projectRoot) ?? string.Empty);

            // Created is the one moment a project that upgraded into this feature can be told
            // what to do about a file it may already be tracking. .gitignore does not untrack
            // what git already follows, and git rm --cached is the user's call, not ours.
            if (rule.Status == BlockWriteStatus.Created)
            {
                string relative = new FlowIoCProjectPaths().Root + "/" + ModuleDirectoryFile.FILE_NAME;

                Debug.Log(
                    "<color=cyan>[FlowIoC]</color> " + relative
                                                     + " is generated and now gitignored. If it was committed before, run "
                                                     + "git rm --cached " + relative + " once.");
            }

            if (rule.Status == BlockWriteStatus.Refused)
            {
                Debug.LogWarning("<color=cyan>[FlowIoC]</color> " + rule.Message);
                return;
            }

            if (rule.Status == BlockWriteStatus.Unchanged) return;

            _writeGitignore(projectRoot, rule.Text);
        }

        private void WriteDirectory(string projectRoot)
        {
            string body = _builder.Build(_entriesOf(projectRoot));

            BlockWriteResult result = _writer.Write(
                _readDirectory(projectRoot) ?? string.Empty, body, ModuleCardWriter.BLOCK_VERSION);

            if (result.Status == BlockWriteStatus.Refused || result.Status == BlockWriteStatus.Unchanged) return;

            _writeDirectory(projectRoot, result.Text);
        }

        /// <summary>
        /// The .gitignore beside MODULES.md, inside FlowIoC's own folder. Never the project's
        /// root one: what a consuming repository tracks is its own business.
        /// </summary>
        private static string GitignorePathFor(string projectRoot) =>
            Path.Combine(new ModuleDirectoryFile().FolderFor(projectRoot), GITIGNORE);

        private static string DefaultReadGitignore(string projectRoot)
        {
            string path = GitignorePathFor(projectRoot);
            return File.Exists(path) ? File.ReadAllText(path) : null;
        }

        private static void DefaultWriteGitignore(string projectRoot, string text)
        {
            string path = GitignorePathFor(projectRoot);
            string folder = Path.GetDirectoryName(path);

            if (!string.IsNullOrEmpty(folder) && !Directory.Exists(folder)) Directory.CreateDirectory(folder);

            File.WriteAllText(path, text);
        }
    }
}

#endif