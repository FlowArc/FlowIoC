#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using FlowIoC.ConsoleModule;
using FlowIoC.Editor.Modules;
using UnityEditor;
using UnityEngine;

namespace FlowIoC.Editor.CodeGenerator.Detector
{
    /// <summary>
    /// Rebuilds the module index on every Editor session and keeps the auto-registered log
    /// types in step with it. This used to also write and repair _*_info.txt marker files; the
    /// index replaces that job entirely, so the only work left here is the log type side of
    /// module detection - adding a type for a module that has none, and now also removing one
    /// whose module is gone.
    /// </summary>
    internal class ModuleAutoDetector
    {
        private const string InitializedKey = "ModuleAutoDetector_Initialized";

        [InitializeOnLoadMethod]
        private static void OnProjectLoad()
        {
            if (!SessionState.GetBool(InitializedKey, false))
            {
                SessionState.SetBool(InitializedKey, true);
                EditorApplication.delayCall += DetectAndRegisterModulesOnStartup;
            }
        }

        public static void DetectAndRegisterModulesOnStartup()
        {
            new ModuleAutoDetector().DetectAndRegisterModules();
        }

        public static void RescanModules()
        {
            new ModuleAutoDetector().DetectAndRegisterModules();
        }

        private void DetectAndRegisterModules()
        {
            // A rebuild that could not run has already said so. Carrying on with an index loaded
            // independently would read an empty module list out of it and propose removing every
            // auto-registered log type, on the strength of a scan that never happened.
            FlowIoCModuleIndex index = new ModuleIndexRebuilder().Rebuild();
            if (index == null) return;

            IEnumerable<string> registeredAutoTypes = FlowLogger.Settings.LogTypes
                .Where(logType => logType.IsAutoRegistered && !logType.IsMandatory)
                .Select(logType => logType.Name);

            IEnumerable<string> moduleNames = index.Modules
                .Where(module => module.Kind != ModuleKind.Test)
                .Select(module => module.Name);

            LogTypeChanges changes = new ModuleLogTypePlan().Plan(registeredAutoTypes, moduleNames);

            if (changes.ToAdd.Count > 0)
            {
                List<(string Name, int Value, Color LogColor)> toAdd = changes.ToAdd
                    .Select(name => (Name: name, Value: -1, LogColor: Color.white))
                    .ToList();

                FlowLogTypeManager.AddFlowLogTypesBatch(toAdd);
            }

            if (changes.ToRemove.Count > 0)
                FlowLogTypeManager.RemoveFlowLogTypesBatch(changes.ToRemove);
        }
    }
}

#endif