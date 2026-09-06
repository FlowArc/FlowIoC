#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using FlowIoC.Editor.CodeGenerator.Menus.Module.ModuleGeneration;
using FlowIoC.Editor.Modules;

namespace FlowIoC.Editor.ModuleScanner
{
    /// <summary>
    /// The references a module's assembly must carry: its own Shared and Signals assemblies, the
    /// Shared assembly of the module it lives in, and - for a test module only - that module's
    /// Signals and its own assembly, because a test module is allowed to reach anything and
    /// drives the module under test through its holder.
    ///
    /// A neighbour's Signals assembly is deliberately not on the list for any other kind. That is
    /// what the split is for: referencing a module's Shared to read a published enum used to put
    /// its signal holder in scope with it, and now it does not.
    ///
    /// Fix only ever adds, through AssemblyDefinitionReferences, which is the whole reason that
    /// class exists: an asmdef may carry references someone added by hand - a Unity package, a
    /// Service module - and rewriting the list would drop them.
    /// </summary>
    internal class AssemblyReferencesCheck : IModuleCheck
    {
        private readonly Func<string, string> _readAsmdef;
        private readonly Action<string, string> _writeFile;
        private readonly Func<ModuleTargetEVO, string> _asmdefPathOf;
        private readonly Func<ModuleTargetEVO, string> _sharedAssemblyOf;
        private readonly Func<ModuleTargetEVO, string> _signalsAssemblyOf;
        private readonly AssemblyDefinitionReferences _references = new AssemblyDefinitionReferences();

        internal AssemblyReferencesCheck() : this(
            path => File.Exists(path) ? File.ReadAllText(path) : null,
            File.WriteAllText,
            module => SingleAsmdefIn(module.AbsolutePath),
            module => new SharedAssemblyDefinition().FindIn(module.AbsolutePath, module.Layout),
            module => new SignalsAssemblyDefinition().FindIn(module.AbsolutePath, module.Layout))
        {
        }

        internal AssemblyReferencesCheck(
            Func<string, string> readAsmdef,
            Action<string, string> writeFile,
            Func<ModuleTargetEVO, string> asmdefPathOf,
            Func<ModuleTargetEVO, string> sharedAssemblyOf,
            Func<ModuleTargetEVO, string> signalsAssemblyOf = null)
        {
            _readAsmdef = readAsmdef;
            _writeFile = writeFile;
            _asmdefPathOf = asmdefPathOf;
            _sharedAssemblyOf = sharedAssemblyOf;
            _signalsAssemblyOf = signalsAssemblyOf ?? (_ => null);
        }

        public string Id => "references";

        public FindingEVO Inspect(ModuleTargetEVO module)
        {
            string content = ContentOf(module);

            // Whether the assembly exists at all is AssemblyDefinitionCheck's finding to make.
            // Reporting it here as well would show the same gap twice on one module.
            if (string.IsNullOrEmpty(content))
                return FindingEVO.Ok(Id, "References (no assembly yet)");

            List<string> missing = Missing(module, content);

            if (missing.Count == 0)
                return FindingEVO.Ok(Id, "References");

            return FindingEVO.Fixable(Id, $"Missing references: {string.Join(", ", missing)}");
        }

        public void Fix(ModuleTargetEVO module)
        {
            string path = _asmdefPathOf(module);
            if (string.IsNullOrEmpty(path)) return;

            string content = _readAsmdef(path);
            if (string.IsNullOrEmpty(content)) return;

            foreach (string reference in Required(module))
                content = _references.Add(content, reference, out bool _);

            _writeFile(path, content);
        }

        private string ContentOf(ModuleTargetEVO module)
        {
            string path = _asmdefPathOf(module);

            return string.IsNullOrEmpty(path) ? null : _readAsmdef(path);
        }

        private List<string> Required(ModuleTargetEVO module)
        {
            var required = new List<string>();

            string shared = _sharedAssemblyOf(module);
            if (!string.IsNullOrEmpty(shared)) required.Add(shared);

            string signals = _signalsAssemblyOf(module);
            if (!string.IsNullOrEmpty(signals)) required.Add(signals);

            if (!string.IsNullOrEmpty(module.ParentSharedAssemblyName))
                required.Add(module.ParentSharedAssemblyName);

            if (module.Kind != ModuleKind.Test) return required;

            if (!string.IsNullOrEmpty(module.ParentSignalsAssemblyName))
                required.Add(module.ParentSignalsAssemblyName);

            if (!string.IsNullOrEmpty(module.ParentAssemblyName))
                required.Add(module.ParentAssemblyName);

            return required;
        }

        /// <summary>
        /// The reference list is matched as quoted text rather than parsed. An asmdef is JSON
        /// Unity wrote, the names are unique, and a quoted whole name cannot match half of
        /// another - "Modules.Player.Shared" does not appear inside "Modules.Player".
        /// </summary>
        private List<string> Missing(ModuleTargetEVO module, string content)
        {
            var missing = new List<string>();

            foreach (string reference in Required(module))
            {
                if (!content.Contains($"\"{reference}\"")) missing.Add(reference);
            }

            return missing;
        }

        private static string SingleAsmdefIn(string folder)
        {
            if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder)) return null;

            string[] found = Directory.GetFiles(folder, "*.asmdef", SearchOption.TopDirectoryOnly);

            return found.Length == 1 ? found[0] : null;
        }
    }
}

#endif