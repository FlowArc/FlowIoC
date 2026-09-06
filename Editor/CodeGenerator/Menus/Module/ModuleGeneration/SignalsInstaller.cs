#if UNITY_EDITOR

using System.IO;
using FlowIoC.Editor.Config.ModuleConfig;
using UnityEditor;

namespace FlowIoC.Editor.CodeGenerator.Menus.Module.ModuleGeneration
{
    /// <summary>
    /// Gives a module the public signal holder it was created without.
    ///
    /// Signals is a tick in Create Module like Shared is, and a module unticked on the day it was
    /// made has an empty Scripts/Signals folder ever since: the folder is mandatory, the assembly
    /// is not, and Module Scanner is right to leave an empty one alone rather than ship a DLL with
    /// nothing in it. What is missing is the holder itself, and that is what this writes - with
    /// the assembly and the namespace settings that follow from it.
    ///
    /// Every step checks before it writes, so running this on a module that already has a holder
    /// changes nothing and says so.
    /// </summary>
    internal class SignalsInstaller
    {
        private const string ACTION = "Add Signals";
        private const string HOLDER_SUFFIX = "Signals";

        private readonly SignalsAssemblyDefinition _signalsAssembly;
        private readonly SharedAssemblyDefinition _sharedAssembly;

        internal SignalsInstaller() : this(new SignalsAssemblyDefinition(), new SharedAssemblyDefinition())
        {
        }

        internal SignalsInstaller(SignalsAssemblyDefinition signalsAssembly, SharedAssemblyDefinition sharedAssembly)
        {
            _signalsAssembly = signalsAssembly;
            _sharedAssembly = sharedAssembly;
        }

        internal ModuleInstallReport Install(string moduleName, string modulePath, DirectoryStructureConfig config)
        {
            var report = new ModuleInstallReport(ACTION);

            string signalsPath = config?.FindFullFolderPathByID(FolderEVO.FolderType.PublicSignals, modulePath);

            if (string.IsNullOrEmpty(signalsPath))
            {
                report.Fail("This module's folder layout has no Scripts/Signals folder, so there is nothing to add.");
                return report;
            }

            string moduleAsmdefPath = FindAssemblyDefinition(modulePath);

            if (string.IsNullOrEmpty(moduleAsmdefPath))
            {
                report.Fail($"No assembly definition found in '{modulePath}', so the Signals assembly would have "
                            + "nothing to belong to.");
                return report;
            }

            if (!Directory.Exists(signalsPath)) Directory.CreateDirectory(signalsPath);

            string holderName = HolderName(moduleName);

            WriteHolder(signalsPath, holderName, report);

            string moduleAssemblyName = Path.GetFileNameWithoutExtension(moduleAsmdefPath);
            string signalsAssemblyName = WriteSignalsAssembly(modulePath, config, moduleAssemblyName, report);

            if (string.IsNullOrEmpty(signalsAssemblyName))
            {
                report.Fail("The Signals assembly could not be written.");
                return report;
            }

            ModuleGenerator.AddSubAssemblyNamespaceExceptions(config, modulePath, signalsAssemblyName);
            report.WroteNamespaceSettings(signalsAssemblyName + ".csproj.DotSettings");

            BindInContext(modulePath, config, holderName, signalsPath, report);

            AssetDatabase.Refresh();

            report.AssemblyName = signalsAssemblyName;
            return report;
        }

        /// <summary>
        /// PlayerModule's holder is PlayerSignals - the module name without its suffix, the way
        /// Create Module names it.
        /// </summary>
        private string HolderName(string moduleName)
        {
            const string moduleSuffix = "Module";

            string core = moduleName != null && moduleName.EndsWith(moduleSuffix, System.StringComparison.Ordinal)
                ? moduleName.Substring(0, moduleName.Length - moduleSuffix.Length)
                : moduleName;

            return core + HOLDER_SUFFIX;
        }

        private void WriteHolder(string signalsPath, string holderName, ModuleInstallReport report)
        {
            string holderPath = Path.Combine(signalsPath, holderName + ".cs");

            // A holder somebody has already written is left exactly as it is: it carries the
            // module's whole public surface, and rewriting it from the template would throw every
            // signal in it away.
            if (File.Exists(holderPath)) return;

            string holderNamespace = NamespaceUtility.GetFullNamespaceForFile(holderPath);

            CodeGeneratorUtils.CreateSignals(
                holderName,
                "TempSignals",
                signalsPath,
                CodeGeneratorStrings.TempSignalsPath,
                holderNamespace,
                false);

            report.CreatedFile(holderPath);
        }

        /// <summary>
        /// The Signals assembly sees the module's own Shared assembly where there is one, because
        /// a public signal is often generic over a type the module publishes.
        /// </summary>
        private string WriteSignalsAssembly(
            string modulePath, DirectoryStructureConfig config, string moduleAssemblyName, ModuleInstallReport report)
        {
            string existing = _signalsAssembly.FindIn(modulePath, config);
            if (!string.IsNullOrEmpty(existing)) return existing;

            string shared = _sharedAssembly.FindIn(modulePath, config);

            string created = string.IsNullOrEmpty(shared)
                ? _signalsAssembly.CreateFor(modulePath, config, moduleAssemblyName)
                : _signalsAssembly.CreateFor(modulePath, config, moduleAssemblyName, shared);

            if (!string.IsNullOrEmpty(created)) report.CreatedAssembly(created);

            return created;
        }

        /// <summary>
        /// The holder is injected into the module's Context the way Create Module binds it, so the
        /// module can dispatch what it just gained without anybody editing the Context by hand.
        /// </summary>
        private void BindInContext(
            string modulePath, DirectoryStructureConfig config, string holderName, string signalsPath,
            ModuleInstallReport report)
        {
            string rootsPath = config?.FindFullFolderPathByID(FolderEVO.FolderType.RootsAndContexts, modulePath);
            if (string.IsNullOrEmpty(rootsPath) || !Directory.Exists(rootsPath)) return;

            string[] contexts = Directory.GetFiles(rootsPath, "*Context.cs", SearchOption.TopDirectoryOnly);
            if (contexts.Length != 1) return;

            string holderNamespace =
                NamespaceUtility.GetFullNamespaceForFile(Path.Combine(signalsPath, holderName + ".cs"));

            string contextText = File.ReadAllText(contexts[0]);
            if (contextText.Contains(holderName)) return;

            CodeGeneratorUtils.BindSignalsInContext(contexts[0], holderName, holderNamespace);

            report.BoundInContext(Path.GetFileNameWithoutExtension(contexts[0]));
        }

        private string FindAssemblyDefinition(string modulePath)
        {
            string[] found = Directory.GetFiles(modulePath, "*.asmdef", SearchOption.TopDirectoryOnly);

            return found.Length > 0 ? found[0] : null;
        }
    }
}

#endif
