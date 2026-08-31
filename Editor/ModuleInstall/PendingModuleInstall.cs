#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using FlowIoC.Editor.AgentRules;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace FlowIoC.Editor.ModuleInstall
{
    /// <summary>
    /// Installs a module once the packages it needs have been added. A module whose asmdef
    /// references Cinemachine cannot be copied into a project that has no Cinemachine - the
    /// project would stop compiling on the way in - so the packages go first and the copy waits.
    ///
    /// Waiting is the whole difficulty. Adding a package resolves asynchronously and then very
    /// often reloads the domain, which throws away whatever was waiting on the request. So the
    /// intent is written to SessionState before the request is made, and two paths pick it up
    /// again: a poller for the resolve that does not reload, and a load hook for the one that
    /// does. Whichever runs first clears the intent, so the module is installed once.
    ///
    /// SessionState and not EditorPrefs, because an intent that outlived the Editor would be an
    /// install nobody asked for.
    /// </summary>
    internal class PendingModuleInstall
    {
        private const string MODULE_KEY = "FlowIoC.PendingModuleInstall.Module";
        private const string PACKAGES_KEY = "FlowIoC.PendingModuleInstall.Packages";
        private const string PAYLOAD_ROOT_KEY = "FlowIoC.PendingModuleInstall.PayloadRoot";
        private const string PAYLOAD_FOLDER_KEY = "FlowIoC.PendingModuleInstall.PayloadFolder";
        private const char SEPARATOR = ';';

        [InitializeOnLoadMethod]
        private static void OnProjectLoad()
        {
            // Delayed, because a load hook runs before the Package Manager has published what the
            // reload resolved, and the check would read the project as it was.
            EditorApplication.delayCall += () => new PendingModuleInstall().Resume();
        }

        /// <summary>
        /// Adds <paramref name="packageIds"/> and remembers that
        /// <paramref name="moduleFolderName"/> is to be installed from the modules FlowIoC ships
        /// when they arrive.
        /// </summary>
        internal void Begin(string moduleFolderName, IReadOnlyList<string> packageIds) =>
            Begin(moduleFolderName, packageIds, new PendingInstallPayload(null, null));

        /// <summary>
        /// The same, from a named payload. A private module ships in a package that is not
        /// FlowIoC, so where to copy it from has to survive the domain reload alongside what to
        /// copy.
        /// </summary>
        internal void Begin(
            string moduleFolderName, IReadOnlyList<string> packageIds, PendingInstallPayload payload)
        {
            SessionState.SetString(MODULE_KEY, moduleFolderName);
            SessionState.SetString(PACKAGES_KEY, string.Join(SEPARATOR.ToString(), packageIds));
            SessionState.SetString(PAYLOAD_ROOT_KEY, payload.PackageRoot);
            SessionState.SetString(PAYLOAD_FOLDER_KEY, payload.Folder);

            var toAdd = new string[packageIds.Count];

            for (int index = 0; index < packageIds.Count; index++)
                toAdd[index] = packageIds[index];

            AddAndRemoveRequest request = Client.AddAndRemove(toAdd);

            void Poll()
            {
                if (!request.IsCompleted)
                    return;

                EditorApplication.update -= Poll;

                if (request.Status == StatusCode.Failure)
                {
                    Forget();
                    Debug.LogError($"<color=cyan>[FlowIoC]</color> Could not add "
                                   + $"{string.Join(", ", packageIds)}: {request.Error?.message}");

                    return;
                }

                Resume();
            }

            EditorApplication.update += Poll;
        }

        /// <summary>
        /// Installs the remembered module if its packages are all here now. A requirement still
        /// missing means the add did not do what it was asked to, and the intent is dropped rather
        /// than left to fire at some unrelated moment later.
        /// </summary>
        internal void Resume()
        {
            string moduleFolderName = SessionState.GetString(MODULE_KEY, string.Empty);

            if (string.IsNullOrEmpty(moduleFolderName))
                return;

            string[] required = SessionState.GetString(PACKAGES_KEY, string.Empty)
                .Split(new[] {SEPARATOR}, StringSplitOptions.RemoveEmptyEntries);

            // Read before Forget erases it: where the module is copied from is as much a part of
            // the intent as which module it is.
            var payload = new PendingInstallPayload(
                SessionState.GetString(PAYLOAD_ROOT_KEY, string.Empty),
                SessionState.GetString(PAYLOAD_FOLDER_KEY, string.Empty));

            IReadOnlyList<string> stillMissing =
                new MissingPackages().In(new InstalledPackages().Ids(), required);

            Forget();

            if (stillMissing.Count > 0)
            {
                Debug.LogError($"<color=cyan>[FlowIoC]</color> {moduleFolderName} was not installed: "
                               + $"{string.Join(", ", stillMissing)} is still missing.");

                return;
            }

            var installer = new ModuleInstaller(new ProjectRoot().Resolve(), payload.Source());

            if (!installer.TryInstall(moduleFolderName, out string error))
                Debug.LogError($"<color=cyan>[FlowIoC]</color> {error}");
        }

        private static void Forget()
        {
            SessionState.EraseString(PAYLOAD_ROOT_KEY);
            SessionState.EraseString(PAYLOAD_FOLDER_KEY);
            SessionState.EraseString(MODULE_KEY);
            SessionState.EraseString(PACKAGES_KEY);
        }
    }
}

#endif