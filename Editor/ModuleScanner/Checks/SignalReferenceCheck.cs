#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using FlowIoC.Editor.CodeGenerator.Menus.Module.ModuleGeneration;
using FlowIoC.Editor.Inspector;
using FlowIoC.Editor.Modules;

namespace FlowIoC.Editor.ModuleScanner
{
    /// <summary>
    /// Only a Connector references another module's Signals assembly.
    ///
    /// This is the check the split made possible. It could never be written against Shared,
    /// because referencing a neighbour's Shared assembly to read a published enum is legitimate
    /// and common - so a scan had no way to tell a module reading data from a module reaching for
    /// signals. Now the two live in different assemblies and the reference itself says which one
    /// a module meant.
    ///
    /// One reference is allowed and everything else is reported: a module's own Signals assembly,
    /// because that is where it keeps its own holder. A screen or sub module gets no allowance for
    /// its parent's - it may read the parent's Shared data, and what the two say to each other
    /// crosses a Connector like any other traffic. Two kinds of module are not inspected at all: a
    /// Connector, which is the one place allowed to know the game's shape, and a test module, which
    /// is test code and may reference anything.
    ///
    /// The finding is Manual rather than Fixable. Removing a reference breaks whatever was using
    /// it, and what to do instead - move the traffic into a Connector, or fold the two modules into
    /// one - is a decision about the game rather than about the file.
    /// </summary>
    internal class SignalReferenceCheck : IModuleCheck
    {
        private static readonly Regex ReferenceEntry = new Regex("\"(?<name>[^\"]+)\"", RegexOptions.Compiled);

        private readonly Func<ModuleTargetEVO, string> _asmdefTextOf;
        private readonly Func<ModuleTargetEVO, string> _signalsAssemblyOf;
        private readonly Func<ModuleTargetEVO, bool> _isConnector;

        internal SignalReferenceCheck() : this(
            DefaultAsmdefTextOf,
            module => new SignalsAssemblyDefinition().FindIn(module.AbsolutePath, module.Layout),
            DefaultIsConnector)
        {
        }

        internal SignalReferenceCheck(
            Func<ModuleTargetEVO, string> asmdefTextOf,
            Func<ModuleTargetEVO, string> signalsAssemblyOf,
            Func<ModuleTargetEVO, bool> isConnector)
        {
            _asmdefTextOf = asmdefTextOf;
            _signalsAssemblyOf = signalsAssemblyOf;
            _isConnector = isConnector;
        }

        public string Id => "signal-references";

        public FindingEVO Inspect(ModuleTargetEVO module)
        {
            // A Connector is the crossing point, so every Signals assembly it names is the job
            // rather than a breach of it.
            if (_isConnector(module))
                return FindingEVO.Ok(Id, "Signal references (a Connector may name any of them)");

            // Test code may reference anything, its parent and its siblings included.
            if (module.Kind == ModuleKind.Test)
                return FindingEVO.Ok(Id, "Signal references (a test module may name any of them)");

            string asmdef = _asmdefTextOf(module);

            // Whether the assembly exists at all is AssemblyDefinitionCheck's finding to make.
            if (string.IsNullOrEmpty(asmdef))
                return FindingEVO.Ok(Id, "Signal references (no assembly yet)");

            List<string> foreign = Foreign(module, asmdef);

            if (foreign.Count == 0)
                return FindingEVO.Ok(Id, "Signal references");

            return FindingEVO.Manual(
                Id,
                $"Reaches another module's signals directly: {string.Join(", ", foreign)}. Only a "
                + "Connector may do that. Wire the traffic in a Connector sub-context and drop the "
                + "reference, or - if the two really are one module - merge them.");
        }

        private List<string> Foreign(ModuleTargetEVO module, string asmdef)
        {
            string own = _signalsAssemblyOf(module) ?? module.ExpectedAssemblyName + SignalsAssemblyDefinition.ASSEMBLY_SUFFIX;

            return References(asmdef)
                .Where(reference => reference.EndsWith(SignalsAssemblyDefinition.ASSEMBLY_SUFFIX, StringComparison.Ordinal))
                .Where(reference => reference != own)
                .Distinct(StringComparer.Ordinal)
                .ToList();
        }

        /// <summary>
        /// The names inside the reference array, read as quoted text rather than parsed. An asmdef
        /// is JSON Unity wrote, and only that array holds quoted strings that could be mistaken
        /// for one - which is why the span is cut out before the names are read.
        /// </summary>
        private static IEnumerable<string> References(string asmdef)
        {
            int keyIndex = asmdef.IndexOf("\"references\"", StringComparison.Ordinal);
            if (keyIndex < 0) yield break;

            int openIndex = asmdef.IndexOf('[', keyIndex);
            int closeIndex = openIndex < 0 ? -1 : asmdef.IndexOf(']', openIndex);

            if (openIndex < 0 || closeIndex < 0) yield break;

            string inner = asmdef.Substring(openIndex + 1, closeIndex - openIndex - 1);

            foreach (Match match in ReferenceEntry.Matches(inner))
                yield return match.Groups["name"].Value;
        }

        private static string DefaultAsmdefTextOf(ModuleTargetEVO module)
        {
            if (module == null || string.IsNullOrEmpty(module.AbsolutePath) || !Directory.Exists(module.AbsolutePath))
                return null;

            string[] found = Directory.GetFiles(module.AbsolutePath, "*.asmdef", SearchOption.TopDirectoryOnly);

            return found.Length == 1 ? File.ReadAllText(found[0]) : null;
        }

        /// <summary>
        /// Whether the module holds a Connector's context, asked the same way Add Sub Context asks
        /// it - so a context that declares itself with [FlowHeader(FlowRole.Connector)] counts
        /// alongside one named for the job. A name check here would answer differently from the
        /// inspector, and two answers to one question is worse than either.
        /// </summary>
        private static bool DefaultIsConnector(ModuleTargetEVO module)
        {
            if (module == null || string.IsNullOrEmpty(module.ExpectedAssemblyName)) return false;

            var roles = new FlowRoleResolver();

            return AppDomain.CurrentDomain.GetAssemblies()
                .Where(assembly => assembly.GetName().Name == module.ExpectedAssemblyName)
                .SelectMany(SafeTypes)
                .Any(roles.IsConnector);
        }

        /// <summary>
        /// A type that cannot be loaded is not a Connector, and is certainly not a reason for the
        /// whole scan to fall over.
        /// </summary>
        private static IEnumerable<Type> SafeTypes(System.Reflection.Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (System.Reflection.ReflectionTypeLoadException exception)
            {
                return exception.Types.Where(type => type != null);
            }
        }

        /// <summary>
        /// Never called: this check reports Manual and nothing else, and Fix runs only for a
        /// finding its own check called Fixable.
        /// </summary>
        public void Fix(ModuleTargetEVO module)
        {
        }
    }
}

#endif