#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using FlowIoC.Editor.CodeGenerator.Menus.Module.ModuleGeneration;
using UnityEngine;

namespace FlowIoC.Editor.CodeGenerator.Menus.Module
{
    /// <summary>
    /// Every assembly a module folder declares, read from the asmdefs inside it.
    ///
    /// Delete Module used to work this out from the module's name alone - the module's own assembly,
    /// plus Shared and Signals - and a module that holds sub-modules declares more than those three.
    /// A screen module brings a test module with it, so deleting one left
    /// Modules.MatchBoard.Screen.Test referenced in asmdefs that named it and its .csproj and
    /// .csproj.DotSettings at the solution root, pointing at an assembly that had gone. The same
    /// shape reaches further on a main module, which may hold several screen modules and their test
    /// modules under it.
    ///
    /// The asmdef is asked rather than the folder name because the asmdef is where the answer
    /// actually is: a module whose Scripts/Signals folder carries no asmdef has no Signals assembly,
    /// and a name-derived list would claim one.
    ///
    /// The three derived names are kept alongside what was found, so a module whose folder has
    /// already been half removed still has its own assemblies unwired. Naming one that never existed
    /// costs nothing: the reference cleaner only removes references that are there, and the project
    /// files are only deleted where they exist.
    /// </summary>
    internal class ModuleAssemblies
    {
        private const string ASMDEF_PATTERN = "*.asmdef";

        private readonly Func<string, IEnumerable<string>> _asmdefsUnder;
        private readonly Func<string, string> _readText;

        internal ModuleAssemblies() : this(AsmdefsUnder, File.ReadAllText)
        {
        }

        internal ModuleAssemblies(Func<string, IEnumerable<string>> asmdefsUnder, Func<string, string> readText)
        {
            _asmdefsUnder = asmdefsUnder;
            _readText = readText;
        }

        /// <summary>
        /// <paramref name="modulePath"/> is the module folder on disk and <paramref name="moduleName"/>
        /// the folder's own name. The order is the asmdefs first, in the order they were found, and
        /// then whichever of the derived three were not among them.
        /// </summary>
        internal IReadOnlyList<string> Of(string modulePath, string moduleName)
        {
            var assemblies = new List<string>();

            foreach (string asmdef in Asmdefs(modulePath))
            {
                string name = NameIn(asmdef);

                if (!string.IsNullOrEmpty(name) && !assemblies.Contains(name)) assemblies.Add(name);
            }

            foreach (string derived in Derived(moduleName))
            {
                if (!assemblies.Contains(derived)) assemblies.Add(derived);
            }

            return assemblies;
        }

        /// <summary>What the module's name says its own three assemblies are called.</summary>
        private static IEnumerable<string> Derived(string moduleName)
        {
            string assemblyName = new ModuleAssemblyName().From(moduleName);

            if (string.IsNullOrEmpty(assemblyName)) yield break;

            yield return assemblyName;
            yield return assemblyName + SharedAssemblyDefinition.ASSEMBLY_SUFFIX;
            yield return assemblyName + SignalsAssemblyDefinition.ASSEMBLY_SUFFIX;
        }

        private IEnumerable<string> Asmdefs(string modulePath)
        {
            if (string.IsNullOrEmpty(modulePath)) return Array.Empty<string>();

            try
            {
                return _asmdefsUnder(modulePath) ?? Array.Empty<string>();
            }
            catch (IOException)
            {
                return Array.Empty<string>();
            }
            catch (UnauthorizedAccessException)
            {
                return Array.Empty<string>();
            }
        }

        /// <summary>
        /// The assembly name an asmdef declares. A file that cannot be read or does not parse is
        /// worth no name rather than an exception: this runs on the way to a deletion, and one
        /// unreadable asmdef is not a reason to leave the rest of the module wired in.
        /// </summary>
        private string NameIn(string asmdefPath)
        {
            try
            {
                return JsonUtility.FromJson<AssemblyDefinitionName>(_readText(asmdefPath))?.name;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static IEnumerable<string> AsmdefsUnder(string modulePath) =>
            Directory.Exists(modulePath)
                ? Directory.EnumerateFiles(modulePath, ASMDEF_PATTERN, SearchOption.AllDirectories)
                : Array.Empty<string>();

        /// <summary>The one field of an asmdef this cares about. JsonUtility ignores the rest.</summary>
        [Serializable]
        private class AssemblyDefinitionName
        {
            public string name;
        }
    }
}

#endif
