#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using FlowIoC.Editor.ModuleInstall;
using UnityEngine;

namespace FlowIoC.Editor.SetupModules
{
    /// <summary>What one run of the installer did, or why it did nothing.</summary>
    internal class SetupInstallReport
    {
        internal bool Succeeded;
        internal string[] Installed = Array.Empty<string>();
        internal string Blocked;
    }

    /// <summary>
    /// Copies the setup modules into the project, all of them or none.
    ///
    /// All or none is not tidiness. The payload holds GUID references that cross module
    /// boundaries - the ScreenManager on ScreenRoot lists the two screen modules' config assets,
    /// and MainScene instantiates roots from four different modules - so half a set is a set that
    /// does not work. The pre-flight check is what makes that promise keepable: every target is
    /// tested before anything is written.
    ///
    /// This class copies files and nothing more. Telling Unity about what arrived - the module
    /// index, the log channels, the namespace settings, the Addressables entries, the build list -
    /// belongs to the startup hook, which does it once for the whole set rather than once per
    /// module.
    /// </summary>
    internal class SetupModulesInstaller
    {
        internal const string TargetFolder = "Assets/Modules";

        private readonly string _projectRoot;
        private readonly ModulesSource _source;

        internal SetupModulesInstaller(string projectRoot, ModulesSource source)
        {
            _projectRoot = projectRoot;
            _source = source;
        }

        internal string TargetOf(string moduleFolderName) =>
            Path.Combine(_projectRoot, TargetFolder, moduleFolderName);

        /// <summary>
        /// True when every module of the set is already in the project. Asked of the assemblies
        /// rather than the folders, so a game that renamed or moved one is still credited with
        /// having it.
        /// </summary>
        internal bool IsInstalled()
        {
            if (!_source.TryList(out string[] payload, out _) || payload.Length == 0)
                return false;

            foreach (string folder in payload)
            {
                if (AssemblyAt(Path.GetFileName(folder)) == null)
                    return false;
            }

            return true;
        }

        internal SetupInstallReport Install()
        {
            var report = new SetupInstallReport();

            if (!_source.TryList(out string[] payload, out string listError))
            {
                report.Blocked = listError;
                return report;
            }

            if (payload.Length == 0)
            {
                report.Blocked = $"FlowIoC ships no setup modules. Looked in '{_source.Root}'.";
                return report;
            }

            string blocked = FirstBlocker(payload);

            if (blocked != null)
            {
                report.Blocked = blocked;
                return report;
            }

            var written = new List<string>();

            try
            {
                Directory.CreateDirectory(Path.Combine(_projectRoot, TargetFolder));

                foreach (string folder in payload)
                {
                    string name = Path.GetFileName(folder);
                    CopyTree(folder, TargetOf(name));
                    written.Add(name);
                }
            }
            catch (Exception exception)
            {
                // Half a set is worse than none, so what this run wrote is taken back out and the
                // caller is told nothing happened. A disk that failed once can be tried again.
                foreach (string name in written)
                    TryDelete(TargetOf(name));

                report.Blocked = $"FlowIoC could not copy the setup modules: {exception.Message}";
                return report;
            }

            report.Succeeded = true;
            report.Installed = written.ToArray();

            return report;
        }

        /// <summary>
        /// The first reason the set cannot be written, or null when every target is free. Both
        /// questions matter: a folder in the way would be copied into, and an assembly already in
        /// the project would collide with the one the payload declares and stop everything
        /// compiling.
        /// </summary>
        private string FirstBlocker(IEnumerable<string> payload)
        {
            foreach (string folder in payload)
            {
                string name = Path.GetFileName(folder);

                if (Directory.Exists(TargetOf(name)))
                    return $"{TargetFolder}/{name} already exists.";

                string assembly = ShippedAssemblyName(folder);

                if (assembly != null && AssemblyAt(name) != null)
                    return $"The assembly '{assembly}' is already in this project.";
            }

            return null;
        }

        /// <summary>The folder in this project declaring the assembly the named module ships, or null.</summary>
        private string AssemblyAt(string moduleFolderName)
        {
            string assembly = ShippedAssemblyName(_source.PathOf(moduleFolderName));

            if (string.IsNullOrEmpty(assembly))
                return null;

            string assets = Path.Combine(_projectRoot, "Assets");

            if (!Directory.Exists(assets))
                return null;

            foreach (string asmdef in Directory.GetFiles(assets, "*.asmdef", SearchOption.AllDirectories))
            {
                if (assembly.Equals(AssemblyNameIn(asmdef), StringComparison.Ordinal))
                    return Path.GetDirectoryName(asmdef);
            }

            return null;
        }

        /// <summary>
        /// The assembly a payload folder declares. A module folder holds exactly one asmdef at its
        /// top; the ones for its Shared, screen and test assemblies sit deeper.
        /// </summary>
        private static string ShippedAssemblyName(string moduleRoot)
        {
            if (!Directory.Exists(moduleRoot))
                return null;

            string[] asmdefs = Directory.GetFiles(moduleRoot, "*.asmdef", SearchOption.TopDirectoryOnly);

            return asmdefs.Length == 1 ? AssemblyNameIn(asmdefs[0]) : null;
        }

        private static string AssemblyNameIn(string asmdefPath)
        {
            try
            {
                var declaration = JsonUtility.FromJson<AssemblyDefinitionName>(File.ReadAllText(asmdefPath));

                return declaration == null || string.IsNullOrEmpty(declaration.name) ? null : declaration.name;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (Directory.Exists(path))
                    Directory.Delete(path, true);
            }
            catch (Exception)
            {
                // The rollback is best effort. Whatever survives is reported by the caller's log,
                // and the marker is not written, so the next session tries again.
            }
        }

        private static void CopyTree(string source, string target)
        {
            Directory.CreateDirectory(target);

            foreach (string file in Directory.GetFiles(source))
                File.Copy(file, Path.Combine(target, Path.GetFileName(file)), false);

            foreach (string directory in Directory.GetDirectories(source))
                CopyTree(directory, Path.Combine(target, Path.GetFileName(directory)));
        }

        /// <summary>Just enough of an asmdef to read its assembly name.</summary>
        [Serializable]
        private class AssemblyDefinitionName
        {
            public string name;
        }
    }
}

#endif
