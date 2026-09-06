#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using FlowIoC.Editor.Inspector;
using FlowIoC.Editor.Config.ModuleConfig;
using FlowIoC.Editor.ModuleScanner;
using FlowIoC.Editor.Modules;
using FlowIoC.Editor.CodeGenerator.Menus.Module.ModuleGeneration;

namespace FlowIoC.Editor.CodeGenerator.Menus.Module
{
    internal enum ModuleOfferKind
    {
        Shared,
        Signals
    }

    /// <summary>
    /// Something a module could be given, with the sentence that says what it is for.
    /// </summary>
    internal class ModuleOfferEVO
    {
        internal ModuleOfferKind Kind { get; set; }
        internal string Label { get; set; }
        internal string Explanation { get; set; }
    }

    /// <summary>
    /// What a module has not got and might want - as against what is wrong with it, which is a
    /// finding.
    ///
    /// The difference is the whole reason this is a type of its own. A module without a Shared
    /// assembly is not broken: publishing data is a choice, and SharedAssemblyCheck deliberately
    /// passes over a module that has no Shared folder. The same holds for a public signal holder,
    /// which is why an empty Scripts/Signals folder is reported Ok rather than repaired.
    ///
    /// That is why offers live in a window of their own rather than in Module Scanner. They were
    /// tried there, under each module's findings, and the panel that reports what is wrong is the
    /// wrong place to read what is merely absent: its "Only issues" filter hides every module a
    /// reader would be shopping for, so the offer was invisible exactly when it applied.
    /// </summary>
    internal class ModuleOffers
    {
        private readonly DirectoryStructureConfigProvider _configProvider = new DirectoryStructureConfigProvider();
        private readonly ModuleAssetPathResolver _pathResolver = new ModuleAssetPathResolver();

        internal IReadOnlyList<ModuleOfferEVO> For(ModuleTargetEVO module)
        {
            var offers = new List<ModuleOfferEVO>();

            if (module == null || WhyNotOffered(module) != null) return offers;

            if (WantsShared(module))
            {
                offers.Add(new ModuleOfferEVO
                {
                    Kind = ModuleOfferKind.Shared,
                    Label = "Add Shared",
                    Explanation = "Scripts/Shared, its assembly, and the references that let this module's "
                                  + "screen, sub and test modules read what it publishes.",
                });
            }

            if (WantsSignals(module))
            {
                offers.Add(new ModuleOfferEVO
                {
                    Kind = ModuleOfferKind.Signals,
                    Label = "Add Signals",
                    Explanation = "The public signal holder in Scripts/Signals, its assembly, and the "
                                  + "binding that puts it in this module's Context.",
                });
            }

            return offers;
        }

        /// <summary>
        /// Why this module is offered neither, or null when it is offered both. A reader is owed
        /// the reason rather than a blank row: the two modules that get nothing here get nothing
        /// on purpose.
        /// </summary>
        internal string WhyNotOffered(ModuleTargetEVO module)
        {
            if (module == null) return string.Empty;

            // A test module publishes nothing and announces nothing: it holds no data another
            // module reads, nothing outside it dispatches into it, and it may reference whatever
            // it likes directly.
            if (module.Kind == ModuleKind.Test) return "a test module publishes and announces nothing";

            // A Connector is the one module that owes no signals of its own - it wires other
            // modules' signals and announces none - and it publishes no data for the same reason.
            // Its empty Scripts/Signals folder is the correct state, not a gap.
            if (IsConnector(module)) return "a Connector wires other modules and owns neither";

            return null;
        }

        /// <summary>
        /// Asked of FlowRoleResolver rather than of the folder name alone, so a module that
        /// declares itself with [FlowHeader(FlowRole.Connector)] counts as one too. The name is
        /// the fallback for a project that will not compile, where there is no type to ask.
        /// </summary>
        private bool IsConnector(ModuleTargetEVO module)
        {
            Assembly assembly = AssemblyNamed(module.ExpectedAssemblyName);

            if (assembly == null)
                return NameSaysConnector(module.Name);

            var resolver = new FlowRoleResolver();

            foreach (Type type in SafeTypes(assembly))
            {
                if (!IsRootOrContext(type)) continue;
                if (resolver.IsConnector(type)) return true;
            }

            return NameSaysConnector(module.Name);
        }

        private bool NameSaysConnector(string name) =>
            !string.IsNullOrEmpty(name) && name.Contains("Connector");

        private bool IsRootOrContext(Type type)
        {
            if (type.IsAbstract) return false;

            for (Type walk = type.BaseType; walk != null; walk = walk.BaseType)
            {
                if (walk.Name == "Root`1" || walk.Name == "Context") return true;
            }

            return false;
        }

        private Assembly AssemblyNamed(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;

            return AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(assembly => assembly.GetName().Name == name);
        }

        private IEnumerable<Type> SafeTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException exception)
            {
                return exception.Types.Where(type => type != null);
            }
        }

        /// <summary>
        /// The assembly the module already has for this kind, or empty when it has the folder but
        /// no assembly in it - which is a Module Scanner finding rather than anything this window
        /// repairs.
        /// </summary>
        internal string AssemblyOf(ModuleTargetEVO module, ModuleOfferKind kind)
        {
            DirectoryStructureConfig config = module.Layout;

            return kind == ModuleOfferKind.Shared
                ? new SharedAssemblyDefinition().FindIn(module.AbsolutePath, config)
                : new SignalsAssemblyDefinition().FindIn(module.AbsolutePath, config);
        }

        /// <summary>
        /// Runs the offer and hands back what it did, or null when the module could not be found
        /// in the index - which Shared needs, because it wires the module's children as well.
        /// </summary>
        internal ModuleInstallReport Apply(ModuleOfferEVO offer, ModuleTargetEVO module)
        {
            DirectoryStructureConfig config = _configProvider.ConfigFor(module.Kind);

            if (offer.Kind == ModuleOfferKind.Signals)
                return new SignalsInstaller().Install(module.Name, module.AbsolutePath, config);

            ModuleRegistry registry = new ModuleRegistryFactory().FromProject();
            string assetPath = _pathResolver.ToAssetPath(module.AbsolutePath);

            if (!registry.TryGetModule(assetPath, out ModuleDescriptorEVO descriptor))
            {
                var report = new ModuleInstallReport("Add Shared");
                report.Fail($"'{assetPath}' is not in the module index. Fix the index first.");

                return report;
            }

            return new SharedDataInstaller().Install(registry, descriptor, module.AbsolutePath, config);
        }

        /// <summary>
        /// A layout that declares no Shared folder is not offered one - the module could not hold
        /// it - and a folder already on disk needs nothing.
        /// </summary>
        private bool WantsShared(ModuleTargetEVO module)
        {
            string path = module.Layout?.FindFullFolderPathByID(FolderEVO.FolderType.Shared, module.AbsolutePath);

            return !string.IsNullOrEmpty(path) && !Directory.Exists(path);
        }

        /// <summary>
        /// Scripts/Signals is mandatory, so the folder is nearly always there and empty is the
        /// case worth offering: a module created with signals unticked has the folder and no
        /// holder in it.
        /// </summary>
        private bool WantsSignals(ModuleTargetEVO module)
        {
            string path = module.Layout?.FindFullFolderPathByID(FolderEVO.FolderType.PublicSignals, module.AbsolutePath);

            if (string.IsNullOrEmpty(path)) return false;
            if (!Directory.Exists(path)) return true;

            return Directory.GetFiles(path, "*.cs", SearchOption.AllDirectories).Length == 0;
        }
    }
}

#endif