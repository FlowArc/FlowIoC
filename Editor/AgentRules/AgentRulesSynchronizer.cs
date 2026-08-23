#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;

namespace FlowIoC.Editor.AgentRules
{
    internal enum SyncStatus
    {
        Absent,
        Current,
        Stale,
        Malformed,
        Failed,
    }

    internal readonly struct SyncFileState
    {
        internal string Path { get; }
        internal SyncStatus Status { get; }
        internal string Message { get; }

        internal SyncFileState(string path, SyncStatus status, string message = null)
        {
            Path = path;
            Status = status;
            Message = message;
        }
    }

    /// <summary>
    /// Writes the FlowIoC rule block into the consumer project's root AGENTS.md and points
    /// CLAUDE.md at it. The project root is injected rather than read from Application.dataPath
    /// so the whole thing can be exercised against a temporary directory in tests.
    /// </summary>
    internal class AgentRulesSynchronizer
    {
        internal const string AgentsFileName = "AGENTS.md";
        internal const string ClaudeFileName = "CLAUDE.md";
        internal const string ClaudeImport = "@AGENTS.md";

        private const string ClaudeBody =
            "FlowIoC architecture rules for this project live in AGENTS.md.\n\n" + ClaudeImport;

        private readonly string _projectRoot;
        private readonly AgentRulesSource _source;
        private readonly ManagedBlockWriter _writer = new ManagedBlockWriter();

        internal AgentRulesSynchronizer(string projectRoot, AgentRulesSource source)
        {
            _projectRoot = projectRoot;
            _source = source;
        }

        internal SyncFileState[] Inspect() => Run(false);

        internal SyncFileState[] Sync() => Run(true);

        internal SyncFileState[] RemoveBlocks()
        {
            var states = new List<SyncFileState>();

            foreach (string name in new[] { AgentsFileName, ClaudeFileName })
            {
                string path = System.IO.Path.Combine(_projectRoot, name);
                if (!File.Exists(path))
                    continue;

                try
                {
                    string stripped = _writer.Remove(File.ReadAllText(path));
                    WriteAtomic(path, stripped);
                    states.Add(new SyncFileState(path, SyncStatus.Absent));
                }
                catch (Exception exception)
                {
                    states.Add(new SyncFileState(path, SyncStatus.Failed, exception.Message));
                }
            }

            return states.ToArray();
        }

        private SyncFileState[] Run(bool write)
        {
            if (!_source.TryRead(out string rules, out string error))
                return new[] { new SyncFileState(_projectRoot, SyncStatus.Failed, error) };

            return new[]
            {
                Process(System.IO.Path.Combine(_projectRoot, AgentsFileName), rules, write),
                Process(System.IO.Path.Combine(_projectRoot, ClaudeFileName), ClaudeBody, write),
            };
        }

        private SyncFileState Process(string path, string body, bool write)
        {
            try
            {
                bool exists = File.Exists(path);
                string existing = exists ? File.ReadAllText(path) : string.Empty;

                // A CLAUDE.md that already pulls in AGENTS.md by its own means is left alone
                // entirely - the consumer already wired it up and we have nothing to add.
                if (path.EndsWith(ClaudeFileName, StringComparison.Ordinal)
                    && exists
                    && existing.Contains(ClaudeImport)
                    && _writer.ReadHash(existing) == null)
                {
                    return new SyncFileState(path, SyncStatus.Current);
                }

                var result = _writer.Write(existing, body, _source.Version);

                switch (result.Status)
                {
                    case BlockWriteStatus.Refused:
                        return new SyncFileState(path, SyncStatus.Malformed, result.Message);

                    case BlockWriteStatus.Unchanged:
                        return new SyncFileState(path, SyncStatus.Current);
                }

                if (!write)
                {
                    return new SyncFileState(path,
                        result.Status == BlockWriteStatus.Created ? SyncStatus.Absent : SyncStatus.Stale);
                }

                WriteAtomic(path, result.Text);
                return new SyncFileState(path, SyncStatus.Current);
            }
            catch (Exception exception)
            {
                return new SyncFileState(path, SyncStatus.Failed, exception.Message);
            }
        }

        /// <summary>
        /// Writes through a sibling temporary file so an interrupted write cannot leave the
        /// consumer with a half-rewritten AGENTS.md.
        /// </summary>
        private void WriteAtomic(string path, string text)
        {
            if (!File.Exists(path))
            {
                File.WriteAllText(path, text);
                return;
            }

            string temp = path + ".flowioc-tmp";
            File.WriteAllText(temp, text);

            try
            {
                File.Replace(temp, path, null);
            }
            catch (PlatformNotSupportedException)
            {
                File.Delete(path);
                File.Move(temp, path);
            }
            finally
            {
                if (File.Exists(temp))
                    File.Delete(temp);
            }
        }
    }
}

#endif
