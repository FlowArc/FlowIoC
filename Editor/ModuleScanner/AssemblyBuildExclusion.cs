#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using FlowIoC.Editor.CodeGenerator.Menus.Module.ModuleGeneration;
using UnityEditor.Compilation;

namespace FlowIoC.Editor.ModuleScanner
{
    /// <summary>
    /// Why a module's assembly is not built in this project, when there is a reason to give.
    ///
    /// A plug module - the AppLovin MAX plug of the Ads module, the Firebase plug of Analytics -
    /// carries a define constraint on its asmdef and compiles only when its SDK is in the project.
    /// Without the SDK the compilation pipeline leaves the assembly out altogether, so it is never
    /// loaded and nothing can describe it. That is the module working as designed, and a check
    /// that reads "not loaded" as "did not compile" reports the plug on every load for ever - which
    /// is exactly what teaches people to stop reading the scanner's line.
    ///
    /// The pipeline's own list is what decides built or not: an assembly whose constraints fail
    /// is absent from it, an assembly that fails to compile is still in it. The asmdef is then
    /// read only to name the constraints. An assembly that is absent with no constraint to
    /// explain it gets no reason, and the caller reports it as it always did.
    /// </summary>
    internal class AssemblyBuildExclusion
    {
        private readonly Func<IEnumerable<string>> _builtAssemblyNames;
        private readonly Func<ModuleTargetEVO, string> _asmdefTextOf;
        private HashSet<string> _built;

        internal AssemblyBuildExclusion() : this(DefaultBuiltAssemblyNames, DefaultAsmdefTextOf)
        {
        }

        internal AssemblyBuildExclusion(Func<IEnumerable<string>> builtAssemblyNames, Func<ModuleTargetEVO, string> asmdefTextOf)
        {
            _builtAssemblyNames = builtAssemblyNames;
            _asmdefTextOf = asmdefTextOf;
        }

        /// <summary>
        /// "compiles only under FLOWIOC_APPLOVIN_MAX", to be read after the assembly's name - or
        /// null when the assembly is built here, or when nothing this class knows explains its
        /// absence.
        /// </summary>
        internal string ReasonFor(ModuleTargetEVO module)
        {
            if (module == null || string.IsNullOrEmpty(module.ExpectedAssemblyName)) return null;

            if (IsBuilt(module.ExpectedAssemblyName)) return null;

            IReadOnlyList<string> constraints = new AssemblyDefinitionDefineConstraints().Read(_asmdefTextOf(module));

            if (constraints.Count == 0) return null;

            return "compiles only under " + string.Join(" and ", constraints);
        }

        /// <summary>
        /// The pipeline's list is read the first time it is needed and kept for the life of the
        /// instance: reading it costs about a tenth of a second, and a scan asks about every plug
        /// whose SDK is absent. Whoever owns an instance keeps it for one scan and no longer, so
        /// a compile in between is never read through a stale list.
        /// </summary>
        private bool IsBuilt(string assemblyName)
        {
            _built ??= new HashSet<string>(_builtAssemblyNames(), StringComparer.Ordinal);

            return _built.Contains(assemblyName);
        }

        private static IEnumerable<string> DefaultBuiltAssemblyNames()
        {
            foreach (Assembly assembly in CompilationPipeline.GetAssemblies(AssembliesType.Editor))
                yield return assembly.name;
        }

        /// <summary>
        /// The module's own asmdef, the one file of its kind at the top of the module folder.
        /// The Shared and Signals ones sit deeper and carry no constraint of their own.
        /// </summary>
        private static string DefaultAsmdefTextOf(ModuleTargetEVO module)
        {
            if (string.IsNullOrEmpty(module.AbsolutePath) || !Directory.Exists(module.AbsolutePath)) return null;

            string[] found = Directory.GetFiles(module.AbsolutePath, "*.asmdef", SearchOption.TopDirectoryOnly);

            return found.Length == 1 ? File.ReadAllText(found[0]) : null;
        }
    }
}
#endif
