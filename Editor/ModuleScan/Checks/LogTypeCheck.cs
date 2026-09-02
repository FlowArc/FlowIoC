#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using FlowIoC.ConsoleModule;
using FlowIoC.Editor.Modules;
using UnityEngine;

namespace FlowIoC.Editor.ModuleScan
{
    /// <summary>
    /// The auto-registered Flow Console channels, one per module.
    ///
    /// The plan itself is ModuleLogTypePlan, which already refuses to propose removals from an
    /// empty module list - a scan that found nothing is a failed scan, not an empty project -
    /// and never proposes removing a channel the project added by hand.
    ///
    /// Test modules are excluded: they run only in the editor and get no channel of their own.
    /// </summary>
    internal class LogTypeCheck : IProjectCheck
    {
        private readonly Action<List<string>> _add;
        private readonly Action<List<string>> _remove;
        private readonly ModuleLogTypePlan _plan = new ModuleLogTypePlan();

        internal LogTypeCheck() : this(AddTypes, FlowLogTypeManager.RemoveFlowLogTypesBatch)
        {
        }

        internal LogTypeCheck(Action<List<string>> add, Action<List<string>> remove)
        {
            _add = add;
            _remove = remove;
        }

        public string Id => "log-types";

        public FindingEVO Inspect(ProjectTargetEVO project)
        {
            LogTypeChanges changes = Changes(project);

            if (changes.ToAdd.Count == 0 && changes.ToRemove.Count == 0)
                return FindingEVO.Ok(Id, "Flow log types");

            var parts = new List<string>();
            if (changes.ToAdd.Count > 0) parts.Add($"missing: {string.Join(", ", changes.ToAdd)}");
            if (changes.ToRemove.Count > 0) parts.Add($"dead: {string.Join(", ", changes.ToRemove)}");

            return FindingEVO.Fixable(Id, $"Flow log types - {string.Join("; ", parts)}");
        }

        public void Fix(ProjectTargetEVO project)
        {
            LogTypeChanges changes = Changes(project);

            if (changes.ToAdd.Count > 0) _add(changes.ToAdd);
            if (changes.ToRemove.Count > 0) _remove(changes.ToRemove);
        }

        private LogTypeChanges Changes(ProjectTargetEVO project)
        {
            var moduleNames = new List<string>();

            if (project.ScannedModules != null)
            {
                foreach (ScannedModule module in project.ScannedModules)
                {
                    if (module.Kind != ModuleKind.Test) moduleNames.Add(module.Name);
                }
            }

            return _plan.Plan(project.RegisteredAutoLogTypes, moduleNames);
        }

        /// <summary>
        /// A value of -1 asks the manager to pick the next free one, and white is what a channel
        /// starts as until someone colours it in the Flow Console settings.
        /// </summary>
        private static void AddTypes(List<string> names)
        {
            var toAdd = new List<(string Name, int Value, Color LogColor)>();

            foreach (string name in names)
                toAdd.Add((name, -1, Color.white));

            FlowLogTypeManager.AddFlowLogTypesBatch(toAdd);
        }
    }
}

#endif
