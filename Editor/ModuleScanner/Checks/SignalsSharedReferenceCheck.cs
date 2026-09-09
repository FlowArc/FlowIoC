#if UNITY_EDITOR
using System;
using System.IO;
using FlowIoC.Editor.CodeGenerator.Menus.Module.ModuleGeneration;
using FlowIoC.Editor.Modules;

namespace FlowIoC.Editor.ModuleScanner
{
    /// <summary>
    /// A module's Signals assembly has to see the module's own Shared assembly.
    ///
    /// A public signal is often generic over a type the module publishes - `Signal&lt;MapCVO&gt;` -
    /// and the two live in different assemblies, so without the reference the holder does not
    /// compile and the compiler reports CS0012. Everything that writes the pair together already
    /// wires it: Create Module passes Shared into the Signals assembly it writes, and
    /// SignalsInstaller looks Shared up when Signals arrives afterwards. This is what catches the
    /// module where the two arrived in the other order, or where somebody edited the list by hand.
    ///
    /// It is the reference list of the Signals asmdef, where AssemblyReferencesCheck reads the
    /// module's own. That left the Signals assembly the one assembly in a module whose references
    /// nothing looked at.
    ///
    /// Only the module's own Shared is required. A holder generic over another module's published
    /// type needs that module's Shared as well, and that reference is added by hand and never
    /// reported: which other modules a holder legitimately reads is a question about the game.
    /// Fix only ever adds, so a hand-added reference survives it.
    /// </summary>
    internal class SignalsSharedReferenceCheck : IModuleCheck
    {
        private readonly Func<ModuleTargetEVO, string> _signalsAsmdefPathOf;
        private readonly Func<ModuleTargetEVO, string> _sharedAssemblyOf;
        private readonly Func<string, string> _readAsmdef;
        private readonly Action<string, string> _writeFile;
        private readonly AssemblyDefinitionReferences _references = new AssemblyDefinitionReferences();

        internal SignalsSharedReferenceCheck() : this(
            module => new SignalsAssemblyDefinition().FindPathIn(module.AbsolutePath, module.Layout),
            module => new SharedAssemblyDefinition().FindIn(module.AbsolutePath, module.Layout),
            path => File.Exists(path) ? File.ReadAllText(path) : null,
            File.WriteAllText)
        {
        }

        internal SignalsSharedReferenceCheck(
            Func<ModuleTargetEVO, string> signalsAsmdefPathOf,
            Func<ModuleTargetEVO, string> sharedAssemblyOf,
            Func<string, string> readAsmdef,
            Action<string, string> writeFile)
        {
            _signalsAsmdefPathOf = signalsAsmdefPathOf;
            _sharedAssemblyOf = sharedAssemblyOf;
            _readAsmdef = readAsmdef;
            _writeFile = writeFile;
        }

        public string Id => "signals-shared-reference";

        public FindingEVO Inspect(ModuleTargetEVO module)
        {
            // A test module keeps its holder in Scripts/Runtime/Signals and has no Signals
            // assembly at all, which is SignalsAssemblyCheck's business rather than this one's.
            if (module.Kind == ModuleKind.Test)
                return FindingEVO.Ok(Id, "Signals-to-Shared reference (a test module has no Signals assembly)");

            string shared = _sharedAssemblyOf(module);

            if (string.IsNullOrEmpty(shared))
                return FindingEVO.Ok(Id, "Signals-to-Shared reference (module publishes no data)");

            string content = ContentOf(module);

            // Whether the Signals assembly exists at all is SignalsAssemblyCheck's finding. Saying
            // it again here would report one gap twice on the same module.
            if (string.IsNullOrEmpty(content))
                return FindingEVO.Ok(Id, "Signals-to-Shared reference (no Signals assembly yet)");

            if (Names(content, shared)) return FindingEVO.Ok(Id, "Signals-to-Shared reference");

            return FindingEVO.Fixable(
                Id,
                $"The Signals assembly does not reference {shared}, so a public signal generic over a "
                + "type this module publishes will not compile.");
        }

        public void Fix(ModuleTargetEVO module)
        {
            string path = _signalsAsmdefPathOf(module);
            if (string.IsNullOrEmpty(path)) return;

            string content = _readAsmdef(path);
            if (string.IsNullOrEmpty(content)) return;

            string shared = _sharedAssemblyOf(module);
            if (string.IsNullOrEmpty(shared)) return;

            _writeFile(path, _references.Add(content, shared, out bool _));
        }

        private string ContentOf(ModuleTargetEVO module)
        {
            string path = _signalsAsmdefPathOf(module);

            return string.IsNullOrEmpty(path) ? null : _readAsmdef(path);
        }

        /// <summary>
        /// The reference is matched as quoted text rather than parsed, the way
        /// AssemblyReferencesCheck matches its own. An asmdef is JSON Unity wrote, the names are
        /// unique, and a quoted whole name cannot match half of another.
        /// </summary>
        private static bool Names(string content, string assemblyName) =>
            content.Contains($"\"{assemblyName}\"");
    }
}

#endif
