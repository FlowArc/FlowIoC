#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using FlowIoC.Editor.Modules;

namespace FlowIoC.Editor.ModuleScanner
{
    /// <summary>
    /// Whether the stored module index still describes what is on disk.
    ///
    /// The scan itself does not read the index - its targets come from the folder tree - which is
    /// exactly what lets the index be one of the things under test rather than a precondition of
    /// testing anything. Everything downstream reads the index: the generators, the namespace
    /// settings, Add Shared Data, the Flow log types.
    /// </summary>
    internal class ModuleIndexCheck : IProjectCheck
    {
        private readonly Action _rebuild;

        internal ModuleIndexCheck() : this(() => new ModuleIndexRebuilder().Rebuild())
        {
        }

        internal ModuleIndexCheck(Action rebuild)
        {
            _rebuild = rebuild;
        }

        public string Id => "index";

        public FindingEVO Inspect(ProjectTargetEVO project)
        {
            var onDisk = new Dictionary<string, ModuleKind>(StringComparer.Ordinal);
            var indexed = new Dictionary<string, ModuleKind>(StringComparer.Ordinal);

            if (project.ScannedModules != null)
            {
                foreach (ScannedModule module in project.ScannedModules)
                    onDisk[module.Name] = module.Kind;
            }

            if (project.Index?.Modules != null)
            {
                foreach (ModuleDescriptorEVO descriptor in project.Index.Modules)
                    indexed[descriptor.Name] = descriptor.Kind;
            }

            // A scan that found nothing is far more likely to be a failed scan than a project
            // with no modules at all, and rebuilding on the strength of it would empty the index.
            // ModuleLogTypePlan takes the same caution about removals.
            if (onDisk.Count == 0)
                return FindingEVO.Ok(Id, $"Module index ({indexed.Count} modules)");

            var unindexed = new List<string>();
            var stale = new List<string>();
            var moved = new List<string>();

            foreach (KeyValuePair<string, ModuleKind> module in onDisk)
            {
                if (!indexed.TryGetValue(module.Key, out ModuleKind kind))
                    unindexed.Add(module.Key);
                else if (kind != module.Value)
                    moved.Add($"{module.Key} ({kind} -> {module.Value})");
            }

            foreach (string name in indexed.Keys)
            {
                if (!onDisk.ContainsKey(name)) stale.Add(name);
            }

            if (unindexed.Count == 0 && stale.Count == 0 && moved.Count == 0)
                return FindingEVO.Ok(Id, $"Module index ({indexed.Count} modules)");

            var parts = new List<string>();
            if (unindexed.Count > 0) parts.Add($"not indexed: {string.Join(", ", unindexed)}");
            if (stale.Count > 0) parts.Add($"gone from disk: {string.Join(", ", stale)}");
            if (moved.Count > 0) parts.Add($"kind changed: {string.Join(", ", moved)}");

            return FindingEVO.Fixable(Id, $"Module index has drifted - {string.Join("; ", parts)}");
        }

        public void Fix(ProjectTargetEVO project) => _rebuild();
    }
}

#endif
