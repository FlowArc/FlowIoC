#if UNITY_EDITOR

using System.IO;
using FlowIoC.Editor.AgentRules;
using UnityEditor;
using UnityEngine;

namespace FlowIoC.Editor.CodeStyle
{
    /// <summary>
    /// Holds the one instance Unity's load callback needs. Unity forces this entry point to be
    /// static; everything it does lives on <see cref="PackageNamespaceFoldersStartup"/>.
    ///
    /// It runs on the first update tick after the load rather than on a delayCall, for the reason
    /// the path migration bootstrap gives: delayCall is pumped by the Editor's GUI loop and does
    /// not fire while the Editor sits unfocused, and a package updated from Rider's terminal is
    /// exactly a reload nobody is looking at.
    /// </summary>
    internal static class PackageNamespaceFoldersStartupHook
    {
        [InitializeOnLoadMethod]
        private static void OnProjectLoad()
        {
            EditorApplication.update -= Run;
            EditorApplication.update += Run;
        }

        private static void Run()
        {
            if (EditorApplication.isUpdating || EditorApplication.isCompiling) return;

            EditorApplication.update -= Run;

            new PackageNamespaceFoldersStartup().Run();
        }
    }

    /// <summary>
    /// Writes the package's own namespace folders as soon as the Editor opens, from wherever the
    /// Package Manager resolved the package to. The path carries the version's hash, so the
    /// session remembers which path it answered for rather than that it answered: updating the
    /// package is a domain reload inside the same session, and that is the one moment the files
    /// have to be written again.
    /// </summary>
    internal class PackageNamespaceFoldersStartup
    {
        private const string SessionKey = "FlowIoC.PackageNamespaceFolders.WrittenFor";

        internal void Run()
        {
            // A batch run has no one to write for and no business editing the workspace it was
            // handed - the same line the solution code style draws.
            if (Application.isBatchMode)
                return;

            var info = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(PackageNamespaceFoldersStartup).Assembly);

            if (info == null || string.IsNullOrEmpty(info.resolvedPath))
                return;

            if (SessionState.GetString(SessionKey, string.Empty) == info.resolvedPath)
                return;

            SessionState.SetString(SessionKey, info.resolvedPath);

            PackageNamespaceFoldersReport report =
                new PackageNamespaceFoldersWriter(new ProjectRoot().Resolve(), info.resolvedPath).Run();

            if (report.Error != null)
            {
                Debug.LogWarning($"[FlowIoC] The package's namespace folders could not be written: {report.Error}");
                return;
            }

            foreach (string path in report.WrittenPaths)
                Debug.Log($"[FlowIoC] Namespace folders written: {Path.GetFileName(path)}");
        }
    }
}

#endif
