#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using FlowIoC.Editor.CodeGenerator.Menus.Module.ModuleGeneration;
using FlowIoC.Editor.Modules;

namespace FlowIoC.Editor.ModuleScanner
{
    /// <summary>
    /// The module's own assembly definition.
    ///
    /// A missing one can be written safely, because everything the template needs is derivable
    /// from the folder tree - the module's kind, the module it lives in, and that module's Shared
    /// assembly - so the result is what Create Module would have produced. There is no hand-added
    /// reference to lose either: a file that does not exist never carried one.
    ///
    /// A wrongly named or duplicated asmdef is left to a person. Changing an assembly name
    /// cascades into every asmdef that references it by name and into the root
    /// .csproj.DotSettings named after it, which is more than a scan should decide.
    /// </summary>
    internal class AssemblyDefinitionCheck : IModuleCheck
    {
        private const string EXTENSION = ".asmdef";

        private readonly Func<string, string[]> _asmdefsIn;
        private readonly Action<string, string> _writeFile;
        private readonly Func<ModuleTargetEVO, string> _sharedAssemblyOf;
        private readonly Func<ModuleTargetEVO, string> _signalsAssemblyOf;
        private readonly AssemblyDefinitionTemplate _template = new AssemblyDefinitionTemplate();

        internal AssemblyDefinitionCheck() : this(
            folder => Directory.Exists(folder)
                ? Directory.GetFiles(folder, "*" + EXTENSION, SearchOption.TopDirectoryOnly)
                : new string[0],
            File.WriteAllText,
            module => new SharedAssemblyDefinition().FindIn(module.AbsolutePath, module.Layout),
            module => new SignalsAssemblyDefinition().FindIn(module.AbsolutePath, module.Layout))
        {
        }

        internal AssemblyDefinitionCheck(
            Func<string, string[]> asmdefsIn,
            Action<string, string> writeFile,
            Func<ModuleTargetEVO, string> sharedAssemblyOf,
            Func<ModuleTargetEVO, string> signalsAssemblyOf = null)
        {
            _asmdefsIn = asmdefsIn;
            _writeFile = writeFile;
            _sharedAssemblyOf = sharedAssemblyOf;
            _signalsAssemblyOf = signalsAssemblyOf ?? (_ => null);
        }

        public string Id => "assembly";

        public FindingEVO Inspect(ModuleTargetEVO module)
        {
            string[] found = _asmdefsIn(module.AbsolutePath);

            if (found.Length == 0)
                return FindingEVO.Fixable(Id, $"No assembly definition. It should be {module.ExpectedAssemblyName}.");

            if (found.Length > 1)
                return FindingEVO.Manual(
                    Id,
                    $"{found.Length} assembly definitions in one module folder. Leave exactly one.");

            string actual = Path.GetFileNameWithoutExtension(found[0]);

            if (actual == module.ExpectedAssemblyName)
                return FindingEVO.Ok(Id, $"Assembly {actual}");

            return FindingEVO.Manual(
                Id,
                $"Assembly is named {actual} but the module folder says {module.ExpectedAssemblyName}. "
                + "Renaming it also moves every asmdef that references it and its root "
                + ".csproj.DotSettings, so do it by hand.");
        }

        public void Fix(ModuleTargetEVO module)
        {
            _writeFile(
                Path.Combine(module.AbsolutePath, module.ExpectedAssemblyName + EXTENSION),
                _template.Build(module.ExpectedAssemblyName, References(module)));
        }

        /// <summary>
        /// FlowIoC is added by the template itself, and anything null or empty is dropped there
        /// too - which is what a top level module's absent parent comes through as.
        ///
        /// The module's own Shared and Signals assemblies are named only when they exist. Writing
        /// a reference regardless would leave the asmdef pointing at an assembly nothing produces,
        /// which is the same trap ModuleGenerator avoids by passing whatever CreateFor actually
        /// made. Both are repaired earlier in the pipeline than this check, so by now they are
        /// there if the module owns them.
        ///
        /// A test module also names its parent's assembly and its parent's Signals: it drives the
        /// module under test through that holder, and asmdef references are not transitive. Every
        /// other kind gets the parent's Shared and nothing more - a neighbour's signals are for a
        /// Connector to reach, which is the whole reason the holder has an assembly of its own.
        /// </summary>
        private IEnumerable<string> References(ModuleTargetEVO module)
        {
            var references = new List<string>
            {
                _sharedAssemblyOf(module),
                _signalsAssemblyOf(module),
                module.ParentSharedAssemblyName
            };

            if (module.Kind == ModuleKind.Test)
            {
                references.Add(module.ParentSignalsAssemblyName);
                references.Add(module.ParentAssemblyName);
            }

            return references;
        }
    }
}

#endif