#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using FlowIoC.Editor.ModuleScanner;

namespace FlowIoC.Editor.ModuleCards
{
    /// <summary>
    /// Where cards are looked for. This is deliberately a wider list than ModuleScannerRoots and
    /// a separate type rather than a widening of it: the framework's own modules under
    /// Packages/FlowIoC/Runtime share one assembly and were never meant to satisfy the module
    /// checks, so putting them under the scanner would report findings against a layout that is
    /// correct as it stands. They still deserve a card, because an agent routes into ScreenModule
    /// the same way it routes into a game module.
    ///
    /// A Modules~ or PrivateModules~ folder is not here on purpose. Unity does not import a ~
    /// folder, so those modules are not part of the open project; listing them would route an
    /// agent at code that is not installed.
    /// </summary>
    internal class ModuleCardRoots
    {
        private const string PACKAGES = "Packages";
        private const string RUNTIME = "Runtime";
        private const string MODULE_SUFFIX = "Module";

        internal IEnumerable<string> All(string projectRoot)
        {
            foreach (string root in new ModuleScannerRoots().All(projectRoot))
                yield return root;

            string packages = Path.Combine(projectRoot ?? string.Empty, PACKAGES);
            if (!Directory.Exists(packages)) yield break;

            foreach (string package in Directory.GetDirectories(packages))
            {
                string runtime = Path.Combine(package, RUNTIME);
                if (!Directory.Exists(runtime)) continue;

                if (HoldsAModule(runtime)) yield return runtime;
            }
        }

        /// <summary>
        /// A package's Runtime folder counts as a card root only when a module actually sits
        /// directly in it. Every package has a Runtime; not every one is built out of modules.
        /// </summary>
        private bool HoldsAModule(string runtime)
        {
            foreach (string candidate in Directory.GetDirectories(runtime))
            {
                if (Path.GetFileName(candidate).EndsWith(MODULE_SUFFIX, StringComparison.Ordinal)) return true;
            }

            return false;
        }
    }
}

#endif
