#if UNITY_EDITOR

using FlowIoC.Editor.CodeGenerator.Detector;
using FlowIoC.Editor.ModuleScanner;
using UnityEditor;
using UnityEngine;

namespace FlowIoC.Editor.ModuleInstall
{
    /// <summary>
    /// What turns a folder that was just copied into a module the rest of the Editor knows about,
    /// after an install and after an update alike. The order matters: the index has to know the
    /// module before the namespace settings can be written from it.
    /// </summary>
    internal class InstalledModuleRegistrar
    {
        /// <param name="projectRelativeFolder">Where the module sits, from the project root.</param>
        /// <param name="verb">"installed" or "updated" - what the console line says was done.</param>
        internal void Register(string projectRelativeFolder, string verb)
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            // The index, and with it the module's own FlowModule part - written now rather than on
            // the next load, so the module's own code compiles against the channel it just gained.
            ModuleAutoDetector.RescanModules();

            // <Assembly>.csproj.DotSettings at the project root, for this module and every other.
            new ModuleRepair().FixAll();

            Debug.Log($"<color=cyan>[FlowIoC]</color> Module {verb}: {projectRelativeFolder}");

            // After the repair above, for the reason the startup pass has: a scan taken before it
            // reports the settings files that FixAll has just written.
            new ModuleScannerStartupReport().Report();
        }
    }
}

#endif
