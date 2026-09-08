#if UNITY_EDITOR
using System.Collections.Generic;
using FlowIoC.ConsoleModule;
using UnityEditor;

namespace FlowIoC.Editor.Console
{
    /// <summary>
    /// Carries the log list across a domain reload. Saving a script, entering play mode and
    /// leaving it all reset every static in the project, and FlowLogger.Logs is one of them - so
    /// without this the console empties itself at the exact moment a reader most wants to see what
    /// it was holding, whatever Clear on Recompile says.
    ///
    /// Clear on Recompile takes what it takes before this runs, when compilation starts, so the
    /// list is parked as it stands and whatever that clear left - the pinned rows - comes back.
    /// </summary>
    [InitializeOnLoad]
    internal static class FlowConsoleReloadSurvival
    {
        private const string PARK_KEY = "FlowIoC.Console.ParkedLogs";

        private static readonly FlowConsoleLogPark Park = new();

        static FlowConsoleReloadSurvival()
        {
            Restore();

            AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeAssemblyReload;
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
        }

        private static void OnBeforeAssemblyReload()
        {
            // Whatever Clear on Recompile was going to take has already been taken by the time
            // this runs - the automatic clear happens when compilation starts, and what it leaves
            // behind is the pinned rows. So the list is parked as it stands, always.
            SessionState.SetString(PARK_KEY, Park.Write(FlowLogger.Logs));
        }

        private static void Restore()
        {
            string parked = SessionState.GetString(PARK_KEY, string.Empty);
            if (string.IsNullOrEmpty(parked)) return;

            SessionState.EraseString(PARK_KEY);

            List<ConsoleLog> logs = Park.Read(parked);
            if (logs.Count == 0) return;

            // Anything written between this assembly loading and here belongs after what the last
            // session left, so the restored logs go in front of it rather than on top.
            if (FlowLogger.Logs.Count > 0)
                logs.AddRange(FlowLogger.Logs);

            FlowLogger.Logs.Clear();
            FlowLogger.Logs.AddRange(logs);
        }
    }
}
#endif