#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using FlowIoC.BaseModule.ProjectPaths;
using FlowIoC.ConsoleModule;
using FlowIoC.Editor.Migration;
using FlowIoC.Editor.Modules;
using UnityEditor;
using UnityEngine;

namespace FlowIoC.Editor.Console
{
    internal static class FlowLogTypeGenerator
    {
        private static readonly FlowIoCProjectPaths Paths = new FlowIoCProjectPaths();

        private static readonly string GeneratedFolder = Paths.GeneratedRoot;
        private static readonly string GeneratedFilePath = Paths.FlowLogType;
        private static readonly string AsmRefPath = Paths.GeneratedAsmRef;

        /// <summary>
        /// What puts a generated file into FlowIoC's own assembly rather than into whatever asmdef
        /// sits above it. The parts of FlowLogType have to share an assembly to be one class, and
        /// they are written into modules that each have an assembly of their own.
        /// </summary>
        private const string ASM_REF_CONTENT = "{\n    \"reference\": \"FlowIoC\"\n}";

        private static bool _generatePending;

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            CD_FlowConsole.OnProjectLogTypesChanged -= OnProjectLogTypesChanged;
            CD_FlowConsole.OnProjectLogTypesChanged += OnProjectLogTypesChanged;

            EditorApplication.delayCall += Generate;
        }

        private static void OnProjectLogTypesChanged()
        {
            if (_generatePending) return;
            _generatePending = true;
            EditorApplication.delayCall += () =>
            {
                _generatePending = false;
                Generate();
            };
        }

        public static void Generate()
        {
            // Before anything is written at the new path. A copy of FlowLogType at the old path and
            // one at the new path at the same time is a duplicate type definition, not clutter.
            new FlowIoCPathMigrator().MigrateIfNeeded();

            var settings = FlowLogger.Settings;
            if (settings == null) return;

            CD_FlowConsole.FlowConsoleLogTypeCVO defaultType = null;
            var moduleTypes = new List<CD_FlowConsole.FlowConsoleLogTypeCVO>();
            var customTypes = new List<CD_FlowConsole.FlowConsoleLogTypeCVO>();

            foreach (var logType in settings.LogTypes)
            {
                if (logType.IsMandatory) continue;

                if (string.Equals(logType.Name, "Default", StringComparison.OrdinalIgnoreCase))
                {
                    defaultType = logType;
                    continue;
                }

                if (logType.IsAutoRegistered)
                    moduleTypes.Add(logType);
                else
                    customTypes.Add(logType);
            }

            if (defaultType == null && moduleTypes.Count == 0 && customTypes.Count == 0)
            {
                // Nothing to generate is a legitimate state, but so is "the settings asset did not
                // load and this object is a stand-in". Only the first one may delete source.
                if (!new LogTypeSettingsGuard().IsTrustworthy(settings.IsStandIn, settings.LogTypes))
                {
                    Debug.LogWarning(
                        "<color=cyan>FlowConsole:</color> the log types came back empty on a settings object that " +
                        "did not come from disk, which means CD_FlowConsole.asset could not be loaded rather than " +
                        $"that the project has no log types. {GeneratedFolder} was left in place, and the file is " +
                        "regenerated as soon as the settings load.");
                    return;
                }

                CleanupGeneratedFiles();
                return;
            }

            EnsureDirectoryExists();
            EnsureAsmRefExists();

            bool wroteSomething = WriteModuleParts(moduleTypes);

            string content = GenerateClassContent(defaultType, customTypes);
            string fullPath = GetFullPath(GeneratedFilePath);

            bool centralChanged = !File.Exists(fullPath) || File.ReadAllText(fullPath) != content;

            if (centralChanged)
            {
                File.WriteAllText(fullPath, content);
                AssetDatabase.ImportAsset(GeneratedFilePath, ImportAssetOptions.ForceUpdate);
            }

            if (!centralChanged && !wroteSomething)
            {
                Debug.Log($"<color=cyan>FlowConsole:</color> FlowLogType is already up to date " +
                          $"({moduleTypes.Count} module type(s), {customTypes.Count} custom type(s)).");
                return;
            }

            Debug.Log($"<color=cyan>FlowConsole:</color> FlowLogType generated with " +
                      $"{moduleTypes.Count} module type(s) and {customTypes.Count} custom type(s).");
        }

        /// <summary>
        /// A module's channel is declared in the module, in a part of its own. FlowLogType used to
        /// be one file listing every channel in the project, which made two people adding a module
        /// on two branches conflict over the same lines, and left a deleted module's channel behind
        /// for somebody to notice. A part per module has neither problem: the file is written into
        /// the module, it goes when the module goes, and nobody else's module touches it.
        ///
        /// The part carries an asmref beside it so that it compiles into FlowIoC rather than into
        /// the module's own assembly. Partial parts must share an assembly, and every module has an
        /// assembly of its own - so without the asmref this could not be a partial class at all.
        /// </summary>
        private static bool WriteModuleParts(List<CD_FlowConsole.FlowConsoleLogTypeCVO> moduleTypes)
        {
            ED_ModuleIndex index = new ModuleIndexProvider().LoadOrCreate();
            if (index == null) return false;

            bool wrote = false;
            var written = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var type in moduleTypes.OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase))
            {
                if (!index.TryGetByName(type.Name, out ModuleDescriptorEVO module)) continue;

                string moduleFolder = AssetDatabase.GUIDToAssetPath(module.FolderGuid);
                if (string.IsNullOrEmpty(moduleFolder)) continue;

                string folder = moduleFolder + "/Scripts/Generated";
                string filePath = folder + "/FlowLogType." + type.Name + ".cs";

                written.Add(filePath);

                string content = GeneratePartContent(type);
                string fullPath = GetFullPath(filePath);

                if (File.Exists(fullPath) && File.ReadAllText(fullPath) == content)
                {
                    EnsureModuleAsmRef(folder);
                    continue;
                }

                Directory.CreateDirectory(GetFullPath(folder));
                File.WriteAllText(fullPath, content);
                AssetDatabase.ImportAsset(folder, ImportAssetOptions.ForceUpdate);
                EnsureModuleAsmRef(folder);
                AssetDatabase.ImportAsset(filePath, ImportAssetOptions.ForceUpdate);
                wrote = true;
            }

            wrote |= RemoveOrphanParts(index, written);

            return wrote;
        }

        /// <summary>
        /// A part whose channel is gone. Delete Module takes the whole module folder, so this is
        /// for the other way round: a channel unregistered while the module stayed.
        /// </summary>
        private static bool RemoveOrphanParts(ED_ModuleIndex index, HashSet<string> written)
        {
            bool removed = false;

            foreach (ModuleDescriptorEVO module in index.Modules)
            {
                string moduleFolder = AssetDatabase.GUIDToAssetPath(module.FolderGuid);
                if (string.IsNullOrEmpty(moduleFolder)) continue;

                string folder = moduleFolder + "/Scripts/Generated";
                string folderFullPath = GetFullPath(folder);
                if (!Directory.Exists(folderFullPath)) continue;

                foreach (string file in Directory.GetFiles(folderFullPath, "FlowLogType.*.cs"))
                {
                    string assetPath = folder + "/" + Path.GetFileName(file);
                    if (written.Contains(assetPath)) continue;

                    AssetDatabase.DeleteAsset(assetPath);
                    removed = true;
                }

                if (Directory.GetFiles(folderFullPath, "*.cs").Length == 0)
                {
                    AssetDatabase.DeleteAsset(folder);
                    removed = true;
                }
            }

            return removed;
        }

        private static void EnsureModuleAsmRef(string folder)
        {
            string assetPath = folder + "/FlowIoC.Generated.asmref";
            string fullPath = GetFullPath(assetPath);
            if (File.Exists(fullPath)) return;

            File.WriteAllText(fullPath, ASM_REF_CONTENT);
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
        }

        private static string GeneratePartContent(CD_FlowConsole.FlowConsoleLogTypeCVO type)
        {
            var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string identifier = SanitizeIdentifier(type.Name, used);

            var sb = new StringBuilder();

            AppendHeader(sb);
            sb.AppendLine("    public static partial class FlowLogType");
            sb.AppendLine("    {");
            sb.AppendLine($"        /// <summary>The {EscapeXml(type.Name)} channel.</summary>");
            sb.AppendLine($"        public const string {identifier} = \"{type.Name}\";");
            sb.AppendLine("    }");
            sb.Append("}");

            return sb.ToString();
        }

        /// <summary>
        /// What is left in the shared file once every module's channel is declared in the module:
        /// the project's Default channel, and any channel somebody added by hand. Those belong to
        /// no module, so there is nowhere else to put them.
        /// </summary>
        private static string GenerateClassContent(
            CD_FlowConsole.FlowConsoleLogTypeCVO defaultType,
            List<CD_FlowConsole.FlowConsoleLogTypeCVO> customTypes)
        {
            var sb = new StringBuilder();
            var usedIdentifiers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            AppendHeader(sb);
            sb.AppendLine("    public static partial class FlowLogType");
            sb.AppendLine("    {");

            if (defaultType != null)
            {
                string identifier = SanitizeIdentifier(defaultType.Name, usedIdentifiers);
                sb.AppendLine($"        /// <summary>The {EscapeXml(defaultType.Name)} channel.</summary>");
                sb.AppendLine($"        public const string {identifier} = \"{defaultType.Name}\";");
            }

            if (customTypes.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("        #region Custom Log Types");
                sb.AppendLine();

                foreach (var type in customTypes.OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase))
                {
                    string identifier = SanitizeIdentifier(type.Name, usedIdentifiers);
                    sb.AppendLine($"        /// <summary>The {EscapeXml(type.Name)} channel.</summary>");
                    sb.AppendLine($"        public const string {identifier} = \"{type.Name}\";");
                    sb.AppendLine();
                }

                sb.AppendLine("        #endregion");
            }

            sb.AppendLine("    }");
            sb.Append("}");

            return sb.ToString();
        }

        private static void AppendHeader(StringBuilder sb)
        {
            sb.AppendLine("//------------------------------------------------------------------------------");
            sb.AppendLine("// <auto-generated>");
            sb.AppendLine("//     This code was generated by FlowConsole.");
            sb.AppendLine("//     Do not modify. Changes will be overwritten.");
            sb.AppendLine("// </auto-generated>");
            sb.AppendLine("//------------------------------------------------------------------------------");
            sb.AppendLine();
            sb.AppendLine("namespace FlowIoC.ConsoleModule");
            sb.AppendLine("{");
        }

        internal static string SanitizeIdentifier(string name, HashSet<string> usedIdentifiers)
        {
            if (string.IsNullOrWhiteSpace(name))
                name = "Unknown";

            var sanitized = name.Replace(' ', '_')
                .Replace('-', '_')
                .Replace('.', '_');

            sanitized = Regex.Replace(sanitized, @"[^\w]", "");

            if (sanitized.Length == 0)
                sanitized = "Unknown";
            else if (char.IsDigit(sanitized[0]))
                sanitized = "_" + sanitized;

            string original = sanitized;
            int suffix = 1;
            while (!usedIdentifiers.Add(sanitized))
            {
                sanitized = $"{original}_{suffix}";
                suffix++;
            }

            return sanitized;
        }

        private static string EscapeXml(string text)
        {
            return text.Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;");
        }

        private static void EnsureDirectoryExists()
        {
            string fullPath = GetFullPath(GeneratedFolder);
            if (!Directory.Exists(fullPath))
            {
                Directory.CreateDirectory(fullPath);
                AssetDatabase.ImportAsset(GeneratedFolder, ImportAssetOptions.ForceUpdate);
            }
        }

        private static void EnsureAsmRefExists()
        {
            string fullPath = GetFullPath(AsmRefPath);
            if (File.Exists(fullPath)) return;

            const string asmRefContent = "{\n    \"reference\": \"FlowIoC\"\n}";
            File.WriteAllText(fullPath, asmRefContent);
            AssetDatabase.ImportAsset(AsmRefPath, ImportAssetOptions.ForceUpdate);
        }

        private static void CleanupGeneratedFiles()
        {
            string fullPath = GetFullPath(GeneratedFilePath);
            if (!File.Exists(fullPath)) return;

            AssetDatabase.DeleteAsset(GeneratedFilePath);

            string asmRefFullPath = GetFullPath(AsmRefPath);
            if (File.Exists(asmRefFullPath))
                AssetDatabase.DeleteAsset(AsmRefPath);

            string dirFullPath = GetFullPath(GeneratedFolder);
            if (Directory.Exists(dirFullPath) && Directory.GetFiles(dirFullPath).Length == 0
                                              && Directory.GetDirectories(dirFullPath).Length == 0)
            {
                AssetDatabase.DeleteAsset(GeneratedFolder);
            }

            Debug.Log(
                $"<color=cyan>FlowConsole:</color> {GeneratedFilePath} was removed, because the settings " +
                "carry no log types beyond the mandatory channels and there is nothing left to generate. " +
                "It is written again the moment a module or a custom channel is registered.");
        }

        private static string GetFullPath(string assetPath)
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            return Path.Combine(projectRoot, assetPath);
        }
    }
}
#endif