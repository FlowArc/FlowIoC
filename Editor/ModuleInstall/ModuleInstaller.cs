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
            InstalledAt(moduleFolderName) != null;

        /// <summary>
        /// The folder the module sits in inside this project, or null when it is not here.
        ///
        /// The lookup is by assembly name, not by folder name. Once installed the folder belongs
        /// to the game, which is free to rename it or move it somewhere that suits the project
        /// better - and a check that only looked for the name the module shipped under would call
        /// that "not installed" and offer to install a second copy. Two copies means two asmdefs
        /// claiming one assembly name, which stops the whole project compiling. The assembly name
        /// is what would collide, so it is what is compared.
        /// </summary>
        internal string InstalledAt(string moduleFolderName)
        {
            string assemblyName = ShippedAssemblyName(moduleFolderName);

            if (string.IsNullOrEmpty(assemblyName))
                return null;

            string assets = Path.Combine(_projectRoot, "Assets");

            if (!Directory.Exists(assets))
                return null;

            foreach (string asmdef in Directory.GetFiles(assets, "*.asmdef", SearchOption.AllDirectories))
            {
                if (assemblyName.Equals(AssemblyNameIn(asmdef), StringComparison.Ordinal))
                    return Path.GetDirectoryName(asmdef);
            }

            return null;
        }

        /// <summary>
        /// The assembly the shipped module declares. A module folder holds exactly one asmdef at
        /// its top - the ones for its Shared and test assemblies sit deeper - so anything else is
        /// a payload this installer does not understand and is reported as nothing.
        /// </summary>
        private string ShippedAssemblyName(string moduleFolderName)
        {
            string root = _source.PathOf(moduleFolderName);

            if (!Directory.Exists(root))
                return null;

            string[] asmdefs = Directory.GetFiles(root, "*.asmdef", SearchOption.TopDirectoryOnly);

            return asmdefs.Length == 1 ? AssemblyNameIn(asmdefs[0]) : null;
        }

        /// <summary>
        /// The name out of an asmdef, or null when the file cannot be read or does not declare
        /// one. An unreadable asmdef is somebody else's problem to report: here it only means
        /// this file is not the module being looked for.
        /// </summary>
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

        /// <summary>
        /// Just enough of an asmdef to read its assembly name. The field is lower case because
        /// that is what the file says and JsonUtility matches on the field name - renaming it to
        /// match the project's style would simply stop it reading anything.
        /// </summary>
        [Serializable]
        private class AssemblyDefinitionName
        {
            public string name;
        }

        /// <summary>A path written from the project root, for a message a reader has to act on.</summary>
        private string ProjectRelative(string fullPath)
        {
            string root = _projectRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            return fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase)
                ? fullPath.Substring(root.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                    .Replace(Path.DirectorySeparatorChar, '/')
                : fullPath;
        }

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

            string installedAt = InstalledAt(moduleFolderName);

            if (installedAt != null)
            {
                error = $"'{moduleFolderName}' is already in this project, at "
                        + $"{ProjectRelative(installedAt)}. Delete it first if you want the shipped "
                        + "copy back.";
                return false;
            }

            string target = TargetOf(moduleFolderName);

            // Nothing declares the module's assembly, and yet the folder it installs to is taken.
            // Copying into it would either fail halfway or mix two modules together, so it is left
            // for whoever put it there to sort out.
            if (Directory.Exists(target))
            {
                error = $"{TargetFolder}/{moduleFolderName} already exists but declares no "
                        + $"'{ShippedAssemblyName(moduleFolderName)}' assembly. Move or delete it "
                        + "before installing.";
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