#if UNITY_EDITOR
using FlowIoC.Editor.Console;
using FlowIoC.Editor.ModuleScanner;
using FlowIoC.Editor.Modules;
using UnityEditor;

namespace FlowIoC.Editor.CodeGenerator.Detector
{
    /// <summary>
    /// Rebuilds the module index on every Editor session and writes the channel parts from it.
    /// This used to also keep a list of auto-registered log types in step with the index; the
    /// index is the record now, and a module's channel is the part the generator writes into it,
    /// so the only work left here is rebuilding the one and regenerating the other.
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
            // only visible in Module Scanner, and a panel nobody remembers to open is a panel that
            // never helps.
            new ModuleScannerStartupReport().Report();
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
            // A rebuild that could not run has already said so. Generating from an index loaded
            // independently would read whatever the last scan left there, on the strength of a scan
            // that never happened.
            ED_ModuleIndex index = new ModuleIndexRebuilder().Rebuild();
            if (index == null) return;

            FlowLogTypeGenerator.Generate();
        }
    }
}

#endif