#if UNITY_EDITOR

using System;
using System.IO;
using FlowIoC.Editor.CodeGenerator.Detector;
using FlowIoC.Editor.CodeGenerator.Provider;
using FlowIoC.Editor.Console;
using UnityEditor;
using UnityEngine;

namespace FlowIoC.Editor.ModuleInstall
{
    /// <summary>
    /// Copies one of the modules FlowIoC ships into the project, and then does the three things a
    /// module needs beyond its files: the module index, the log channel, and the settings file that
    /// tells Rider what the module's namespaces are.
    ///
    /// Copying alone would leave a folder that looks like a module and behaves like none - the
    /// generators would not find it, FlowLogType would not name it, and its namespaces would carry
    /// the Scripts folder in the middle of them.
    /// </summary>
    internal class ModuleInstaller
    {
        internal const string TargetFolder = "Assets/Modules";

        private readonly string _projectRoot;
        private readonly ModulesSource _source;
        private readonly Action<string> _register;

        internal ModuleInstaller(string projectRoot, ModulesSource source)
            : this(projectRoot, source, null)
        {
        }

        /// <summary>
        /// The same installer with the Editor half handed in. Copying files is the part worth
        /// testing, and it cannot be reached from a test while importing assets and rebuilding the
        /// module index are welded to it.
        /// </summary>
        internal ModuleInstaller(string projectRoot, ModulesSource source, Action<string> register)
        {
            _projectRoot = projectRoot;
            _source = source;
            _register = register ?? Register;
        }

        internal string TargetOf(string moduleFolderName) =>
            Path.Combine(_projectRoot, TargetFolder, moduleFolderName);

        internal bool IsInstalled(string moduleFolderName) =>
            Directory.Exists(TargetOf(moduleFolderName));

        /// <summary>
        /// Installs the module, or explains why it could not. A module already in the project is
        /// left exactly as it is: the copy in the project is the one the game has been editing,
        /// and overwriting it would throw that away.
        /// </summary>
        internal bool TryInstall(string moduleFolderName, out string error)
        {
            error = null;

            string source = _source.PathOf(moduleFolderName);

            if (!Directory.Exists(source))
            {
                error = $"FlowIoC ships no module called '{moduleFolderName}'. Looked in '{source}'.";
                return false;
            }

            string target = TargetOf(moduleFolderName);

            if (Directory.Exists(target))
            {
                error = $"'{moduleFolderName}' is already in this project, at "
                        + $"{TargetFolder}/{moduleFolderName}. Delete it first if you want the "
                        + "shipped copy back.";
                return false;
            }

            try
            {
                Directory.CreateDirectory(Path.Combine(_projectRoot, TargetFolder));
                CopyTree(source, target);
            }
            catch (Exception exception)
            {
                error = $"FlowIoC could not copy '{moduleFolderName}': {exception.Message}";
                return false;
            }

            _register(moduleFolderName);

            return true;
        }

        /// <summary>
        /// What turns the copied folder into a module the rest of the Editor knows about. The
        /// order matters: the index has to know the module before the namespace settings can be
        /// written from it.
        /// </summary>
        private void Register(string moduleFolderName)
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            // The index, and with it the module's own FlowLogType channel.
            ModuleAutoDetector.RescanModules();

            // FlowLogType is otherwise regenerated on a delayed call, which is too late for the
            // module's own code to compile against the channel it just gained.
            FlowLogTypeGenerator.Generate();

            // <Assembly>.csproj.DotSettings at the project root, for this module and every other.
            NamespaceProvider.UpdateNamespaceSettings();

            Debug.Log($"<color=cyan>[FlowIoC]</color> Module installed: {TargetFolder}/{moduleFolderName}");
        }

        private static void CopyTree(string source, string target)
        {
            Directory.CreateDirectory(target);

            foreach (string file in Directory.GetFiles(source))
                File.Copy(file, Path.Combine(target, Path.GetFileName(file)), false);

            foreach (string directory in Directory.GetDirectories(source))
                CopyTree(directory, Path.Combine(target, Path.GetFileName(directory)));
        }
    }
}

#endif