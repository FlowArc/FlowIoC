#if UNITY_EDITOR
using System;
using FlowIoC.ConsoleModule;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace FlowIoC.Editor.Console
{
    [InitializeOnLoad]
    internal static class FlowConsoleDefineChecker
    {
        private const string DefineSymbol = "ENABLE_LOG";

        private static readonly NamedBuildTarget[] Targets =
        {
            NamedBuildTarget.Standalone,
            NamedBuildTarget.Android,
            NamedBuildTarget.iOS,
            NamedBuildTarget.WebGL,
            NamedBuildTarget.Server,
        };

        static FlowConsoleDefineChecker()
        {
            EditorApplication.delayCall += EnsureDefineOnAllPlatforms;
        }

        private static void EnsureDefineOnAllPlatforms()
        {
            if (!IsAutoAddEnabled()) return;

            bool anyAdded = false;

            foreach (var target in Targets)
            {
                try
                {
                    PlayerSettings.GetScriptingDefineSymbols(target, out string[] defines);

                    if (Array.IndexOf(defines, DefineSymbol) >= 0)
                        continue;

                    var newDefines = new string[defines.Length + 1];
                    Array.Copy(defines, newDefines, defines.Length);
                    newDefines[defines.Length] = DefineSymbol;
                    PlayerSettings.SetScriptingDefineSymbols(target, newDefines);
                    anyAdded = true;
                }
                catch
                {
                    // Platform module not installed, skip
                }
            }

            if (anyAdded)
            {
                Debug.Log($"<color=cyan>FlowConsole:</color> Added '{DefineSymbol}' scripting define symbol to all platforms.");
            }
        }

        private static bool IsAutoAddEnabled()
        {
            var settings = Resources.Load<CD_FlowConsole>("CD_FlowConsole");
            return settings == null || settings.AutoAddEnableLogDefine;
        }
    }
}
#endif
