#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using FlowIoC.ConsoleModule;
using FlowIoC.Editor.ModuleScan;
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

        /// <summary>
        /// The startup pass, and the one place the scan report belongs: nothing else is running,
        /// so what the scan sees is the project as it stands.
        /// </summary>
        public static void DetectAndRegisterModulesOnStartup()
        {
            new ModuleAutoDetector().DetectAndRegisterModules();

            // Everything the detector touches repairs itself silently. Everything else a module can
            // be missing - an assembly, a mandatory folder, a stale namespace settings file - is
            // only visible in Module Scan, and a panel nobody remembers to open is a panel that
            // never helps.
            new ModuleScanStartupReport().Report();
        }

        /// <summary>
        /// Detection on its own, for a caller that is in the middle of changing the project. It
        /// deliberately does not report: an install has folders copied and settings files not yet
        /// written when it calls this, so a scan taken here reports the very issues the caller
        /// repairs on its next line. Whoever calls this reports when its own work is done.
        /// </summary>
        public static void RescanModules()
        {
            new ModuleAutoDetector().DetectAndRegisterModules();
        }

        private void DetectAndRegisterModules()
        {
            // A rebuild that could not run has already said so. Carrying on with an index loaded
            // independently would read an empty module list out of it and propose removing every
            // auto-registered log type, on the strength of a scan that never happened.
            ED_ModuleIndex index = new ModuleIndexRebuilder().Rebuild();
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