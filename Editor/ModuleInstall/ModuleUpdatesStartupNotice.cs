#if UNITY_EDITOR

using System.Collections.Generic;
using System.Text;
using FlowIoC.Editor.AgentRules;
using FlowIoC.Editor.Help;
using FlowIoC.Editor.Help.WhatsNew;
using FlowIoC.Editor.ModuleCards;
using UnityEditor;
using UnityEngine;

namespace FlowIoC.Editor.ModuleInstall
{
    /// <summary>
    /// Holds the one instance Unity's load callback needs. Unity forces this entry point to be
    /// static; everything it does lives on <see cref="ModuleUpdatesStartupNotice"/>.
    /// </summary>
    [InitializeOnLoad]
    internal static class ModuleUpdatesStartupHook
    {
        static ModuleUpdatesStartupHook()
        {
            EditorApplication.delayCall += () => new ModuleUpdatesStartupNotice().Run();
        }
    }

    /// <summary>
    /// One console line per installed module the package ships a newer version of, once per
    /// Editor session and again when a package's version changes - which is the moment this
    /// exists for. A project with nothing to update says nothing.
    /// </summary>
    internal class ModuleUpdatesStartupNotice
    {
        private const string SESSION_KEY = "FlowIoC.ModuleUpdates.CheckedStamp";

        internal void Run()
        {
            if (Application.isBatchMode)
                return;

            IReadOnlyList<ModulePage> pages = PackageModuleSections.Found();
            string stamp = Stamp(pages);

            // What the session remembers is which set of package versions it answered for, so a
            // package update inside the session - a domain reload, not a restart - answers again.
            if (SessionState.GetString(SESSION_KEY, string.Empty) == stamp)
                return;

            SessionState.SetString(SESSION_KEY, stamp);

            string projectRoot = new ProjectRoot().Resolve();
            var order = new VersionOrder();

            foreach (ModulePage page in pages)
            {
                var payload = new ModulePayload(page.GetType().Assembly);

                if (!payload.IsResolved)
                    continue;

                var installer = new ModuleInstaller(projectRoot, payload.SourceOf(page));
                string installed = installer.InstalledVersionOf(page.ModuleFolderName);

                if (installed == null)
                    continue;

                string shipped = installer.ShippedVersionOf(page.ModuleFolderName);

                if (shipped == ModuleCardVersionLine.NONE || !order.IsNewer(shipped, installed))
                    continue;

                string shown = installed == ModuleCardVersionLine.NONE ? "an unversioned copy" : installed;

                Debug.Log($"<color=cyan>[FlowIoC]</color> {page.Title} {shown} installed, {shipped} shipped "
                          + "- Tools/FlowIoC/Module Library");
            }
        }

        /// <summary>Every package that ships a page, with its version, so a change in any one of them counts.</summary>
        private static string Stamp(IReadOnlyList<ModulePage> pages)
        {
            var seen = new SortedSet<string>();

            foreach (ModulePage page in pages)
            {
                var payload = new ModulePayload(page.GetType().Assembly);
                seen.Add(payload.PackageName + "@" + payload.PackageVersion);
            }

            var stamp = new StringBuilder();

            foreach (string entry in seen)
                stamp.Append(entry).Append(';');

            return stamp.ToString();
        }
    }
}

#endif
