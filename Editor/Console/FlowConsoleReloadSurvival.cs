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
    /// Clear on Recompile, when it is on, is the one case where the list is meant to go. The park
    /// is erased rather than written then, so the reload finds nothing to bring back.
    /// </summary>
    [InitializeOnLoad]
    internal static class FlowConsoleReloadSurvival
    {
        private const string ParkKey = "FlowIoC.Console.ParkedLogs";

        private static readonly FlowConsoleLogPark Park = new();
        private static readonly FlowConsoleState State = new();

        static FlowConsoleReloadSurvival()
        {
            Restore();

            AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeAssemblyReload;
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
        }

        private static void OnBeforeAssemblyReload()
        {
            if (State.ClearOnRecompile)
            {
                SessionState.EraseString(ParkKey);
                return;
            }

            SessionState.SetString(ParkKey, Park.Write(FlowLogger.Logs));
        }

        private static void Restore()
        {
            string parked = SessionState.GetString(ParkKey, string.Empty);
            if (string.IsNullOrEmpty(parked)) return;

            SessionState.EraseString(ParkKey);

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
