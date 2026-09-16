#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using FlowIoC.Editor.ModuleScanner;
using FlowIoC.Editor.Modules;
using UnityEditor;
using UnityEngine;

namespace FlowIoC.Editor.CodeGenerator.Menus.Module
{
    internal static class NamespaceUtility
    {
        internal const string XAML_NAMESPACE = "http://schemas.microsoft.com/winfx/2006/xaml";
        internal const string XAML_ASSEMBLY = "clr-namespace:System;assembly=mscorlib";
        internal const string XAML_PRESENTATION = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
        internal const string JETBRAINS_SETTINGS_STORAGE = "urn:shemas-jetbrains-com:settings-storage-xaml";
        private const string LEGACY_CODE_GENERATED_SECTION = "CodeGeneratedEntries";
        private static readonly CultureInfo TurkishCulture = new CultureInfo("tr-TR");
        internal static readonly string[] SkipFolderNames = {"zScreenModules", "zSubModules", "zTestModules"};

        public static void SetNamespaceProvider(
            string assetFolderPath,
            bool isNamespaceProvider,
            string dotSettingsFilePath
        )
        {
            Debug.Log(
                $"SetNamespaceProvider => folderPath = {assetFolderPath}, isNamespaceProvider = {isNamespaceProvider}, dotSettingsFilePath = {dotSettingsFilePath}");
            string fileName = string.IsNullOrEmpty(dotSettingsFilePath)
                ? "Project.DotSettings"
                : Path.GetFileName(dotSettingsFilePath);

            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string finalDotSettingsPath = Path.Combine(projectRoot, fileName);
            if (!File.Exists(finalDotSettingsPath))
            {
                CreateDotSettingsFile(finalDotSettingsPath);
                Debug.Log($"DotSettings file created at path: {finalDotSettingsPath}");
            }

            var doc = new XmlDocument();
            try
            {
                doc.Load(finalDotSettingsPath);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to load DotSettings file: {ex.Message}");
                return;
            }

            var nsManager = new XmlNamespaceManager(doc.NameTable);
            nsManager.AddNamespace("x", XAML_NAMESPACE);
            nsManager.AddNamespace("s", XAML_ASSEMBLY);

            IReadOnlyList<string> spellings = EncodeSpellings(assetFolderPath);

            if (string.IsNullOrWhiteSpace(spellings[0]))
            {
                Debug.LogError($"Relative path for '{assetFolderPath}' is empty. Skipping.");
                return;
            }

            XmlElement codeGeneratedSection = GetOrCreateCodeGeneratedSection(doc);

            foreach (string spelling in spellings)
            {
                if (isNamespaceProvider)
                    RemoveSkipEntry(codeGeneratedSection, nsManager, spelling);
                else
                    AddOrUpdateSkipEntry(doc, codeGeneratedSection, nsManager, spelling);
            }

            try
            {
                SaveDotSettings(doc, finalDotSettingsPath);
                Debug.Log(
                    $"NamespaceProvider for '{assetFolderPath}' set to {(isNamespaceProvider ? "ENABLED" : "SKIPPED")} in '{finalDotSettingsPath}'.");

                AssetDatabase.Refresh();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to save DotSettings file: {ex.Message}");
            }
        }


        public static void AddNamespaceFolderToSkip(XmlDocument doc, string assetFolderPath)
        {
            IReadOnlyList<string> spellings = EncodeSpellings(assetFolderPath);

            if (string.IsNullOrWhiteSpace(spellings[0]))
            {
                Debug.LogError($"Relative path for '{assetFolderPath}' is empty. Skipping.");
                return;
            }

            XmlNamespaceManager nsManager = new XmlNamespaceManager(doc.NameTable);
            nsManager.AddNamespace("x", XAML_NAMESPACE);
            nsManager.AddNamespace("s", XAML_ASSEMBLY);

            XmlElement codeGeneratedSection = GetOrCreateCodeGeneratedSection(doc);

            foreach (string spelling in spellings)
                AddOrUpdateSkipEntry(doc, codeGeneratedSection, nsManager, spelling);
        }

        private static string NormalizePath(string path)
        {
            return path.Replace('\\', '/');
        }

        /// <summary>
        /// Every spelling of the folder a Rider might look it up under. Rider lowercases the path
        /// before it compares, and which lowercase depends on the machine: the invariant one
        /// nearly everywhere, and on a Turkish machine a capital I has come down as a dotless ı in
        /// files written before. The path as written is kept first, because the files already on
        /// disk carry it and the scanner's check reads it. A folder with no capital I spells the
        /// same three ways and gets two entries; one with it gets three.
        /// </summary>
        internal static IReadOnlyList<string> EncodeSpellings(string assetFolderPath)
        {
            var spellings = new List<string> {EncodeAssetPath(assetFolderPath)};

            foreach (CultureInfo culture in new[] {CultureInfo.InvariantCulture, CultureInfo.CurrentCulture, TurkishCulture})
            {
                string spelling = EncodeLowercaseAssetPath(assetFolderPath, culture);

                if (!spellings.Contains(spelling))
                    spellings.Add(spelling);
            }

            return spellings;
        }

        internal static string EncodeAssetPath(string assetFolderPath)
        {
            return NormalizePath(assetFolderPath)
                .Replace(NormalizePath(Application.dataPath), "Assets")
                .Replace("/", "_005C");
        }

        internal static string EncodeLowercaseAssetPath(string assetFolderPath) =>
            EncodeLowercaseAssetPath(assetFolderPath, CultureInfo.InvariantCulture);

        internal static string EncodeLowercaseAssetPath(string assetFolderPath, CultureInfo culture)
        {
            string relativePath = NormalizePath(assetFolderPath)
                .Replace(NormalizePath(Application.dataPath), "Assets");

            string lowered = relativePath.ToLower(culture);

            var sb = new StringBuilder();
            foreach (char c in lowered)
            {
                if (c == '/')
                {
                    sb.Append("_005C");
                }
                else if (c > 127)
                {
                    sb.AppendFormat("_{0:X4}", (int) c);
                }
                else if (c == '-')
                {
                    sb.Append("_002D");
                }
                else
                {
                    sb.Append(c);
                }
            }

            return sb.ToString();
        }

        private static void AddOrUpdateSkipEntry(XmlDocument doc, XmlElement root,
            XmlNamespaceManager nsManager, string encodedPath)
        {
            string key =
                $"/Default/CodeInspection/NamespaceProvider/NamespaceFoldersToSkip/={encodedPath}/@EntryIndexedValue";

            XmlNode existingNode = root.SelectSingleNode($"s:Boolean[@x:Key='{key}']", nsManager);
            if (existingNode != null)
            {
                existingNode.InnerText = "True";
            }
            else
            {
                XmlElement newBoolean = doc.CreateElement("s", "Boolean", XAML_ASSEMBLY);
                XmlAttribute keyAttribute = doc.CreateAttribute("x", "Key", XAML_NAMESPACE);
                keyAttribute.Value = key;
                newBoolean.Attributes.Append(keyAttribute);
                newBoolean.InnerText = "True";
                root.AppendChild(newBoolean);
            }
        }

        private static void RemoveSkipEntry(XmlElement root, XmlNamespaceManager nsManager,
            string encodedPath)
        {
            string key =
                $"/Default/CodeInspection/NamespaceProvider/NamespaceFoldersToSkip/={encodedPath}/@EntryIndexedValue";

            XmlNode nodeToRemove = root.SelectSingleNode($"s:Boolean[@x:Key='{key}']", nsManager);
            if (nodeToRemove != null)
            {
                root.RemoveChild(nodeToRemove);
            }
        }

        public static XmlElement GetOrCreateCodeGeneratedSection(XmlDocument doc)
        {
            // Remove XML declaration if present (Rider-native format doesn't use it)
            if (doc.FirstChild is XmlDeclaration)
            {
                doc.RemoveChild(doc.FirstChild);
            }

            // Migrate legacy CodeGeneratedEntries section: move children to root, remove section
            XmlNode legacySection = doc.SelectSingleNode($"//{LEGACY_CODE_GENERATED_SECTION}");
            if (legacySection != null)
            {
                while (legacySection.HasChildNodes)
                {
                    doc.DocumentElement.AppendChild(legacySection.FirstChild);
                }

                legacySection.ParentNode.RemoveChild(legacySection);
            }

            EnsureRiderFormatAttributes(doc);

            return doc.DocumentElement;
        }

        public static void CreateDotSettingsFile(string filePath)
        {
            try
            {
                string fileName = Path.GetFileName(filePath);
                string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                string finalDotSettingsPath = Path.Combine(projectRoot, fileName);

                const string content =
                    "<wpf:ResourceDictionary xml:space=\"preserve\"" +
                    " xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"" +
                    " xmlns:s=\"clr-namespace:System;assembly=mscorlib\"" +
                    " xmlns:ss=\"urn:shemas-jetbrains-com:settings-storage-xaml\"" +
                    " xmlns:wpf=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\">" +
                    "\n</wpf:ResourceDictionary>";

                File.WriteAllText(finalDotSettingsPath, content);

                Debug.Log($"DotSettings file created at: {finalDotSettingsPath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to create DotSettings file: {ex.Message}");
            }
        }

        internal static void SaveDotSettings(XmlDocument doc, string path)
        {
            var sb = new StringBuilder();

            sb.Append("<wpf:ResourceDictionary xml:space=\"preserve\"");
            sb.Append(" xmlns:x=\"").Append(XAML_NAMESPACE).Append("\"");
            sb.Append(" xmlns:s=\"").Append(XAML_ASSEMBLY).Append("\"");
            sb.Append(" xmlns:ss=\"").Append(JETBRAINS_SETTINGS_STORAGE).Append("\"");
            sb.Append(" xmlns:wpf=\"").Append(XAML_PRESENTATION).Append("\"");
            sb.AppendLine(">");

            foreach (XmlNode child in doc.DocumentElement.ChildNodes)
            {
                if (child is XmlElement element)
                {
                    sb.Append("\t<s:Boolean x:Key=\"");
                    sb.Append(element.GetAttribute("Key", XAML_NAMESPACE));
                    sb.Append("\">");
                    sb.Append(element.InnerText);
                    sb.AppendLine("</s:Boolean>");
                }
            }

            sb.Append("</wpf:ResourceDictionary>");

            File.WriteAllText(path, sb.ToString());
        }

        private static void EnsureRiderFormatAttributes(XmlDocument doc)
        {
            XmlElement root = doc.DocumentElement;
            if (root == null) return;

            if (!root.HasAttribute("space", "http://www.w3.org/XML/1998/namespace"))
            {
                root.SetAttribute("space", "http://www.w3.org/XML/1998/namespace", "preserve");
            }

            if (string.IsNullOrEmpty(root.GetAttribute("xmlns:ss")))
            {
                root.SetAttribute("xmlns:ss", JETBRAINS_SETTINGS_STORAGE);
            }
        }

        public static string GetModuleNamespace(string modulePath)
        {
            return GetModuleNamespace(new ModuleRegistryFactory().FromProject(), modulePath);
        }

        /// <summary>
        /// Builds "Modules.Outer.Inner" from the module chain modulePath sits in.
        /// AncestorsOf comes back nearest first, so ModuleNamespaceBuilder is the piece that
        /// puts them root-first with the module itself last.
        /// </summary>
        private static string GetModuleNamespace(ModuleRegistry registry, string modulePath)
        {
            registry.TryGetNearestModule(GetUnityAssetPath(modulePath), out ModuleDescriptorEVO module);

            IEnumerable<string> ancestorNames = module == null
                ? Enumerable.Empty<string>()
                : registry.AncestorsOf(module).Select(ancestor => ancestor.Name);

            return new ModuleNamespaceBuilder().Build(ancestorNames, module?.Name);
        }

        public static string GetUnityAssetPath(string fullPath)
        {
            string normalizedFull = NormalizePath(fullPath);
            string normalizedData = NormalizePath(Application.dataPath);

            if (!normalizedFull.StartsWith(normalizedData))
            {
                Debug.LogError($"Path '{fullPath}' is not within the Assets folder.");
                return "Assets";
            }

            return "Assets" + normalizedFull.Substring(normalizedData.Length);
        }


        public static string GetFullNamespaceForFile(string filePath)
        {
            string fileDirectory = Path.GetDirectoryName(filePath);
            if (string.IsNullOrEmpty(fileDirectory))
            {
                return "Modules";
            }

            ModuleRegistry registry = new ModuleRegistryFactory().FromProject();

            string moduleFolder = FindNearestModuleFolder(registry, fileDirectory, out ModuleDescriptorEVO module);
            if (string.IsNullOrEmpty(moduleFolder))
            {
                return "Modules";
            }

            string baseNamespace = GetModuleNamespace(registry, moduleFolder);

            // The folders that name nothing come from the same plan that writes the module's
            // .csproj.DotSettings, so the namespace written into a file and the one Rider reads
            // off its location are answered by one list rather than two.
            IReadOnlyList<string> skipFolders = new DotSettingsPlan().SkipFoldersInside(
                moduleFolder,
                new DirectoryStructureConfigProvider().ConfigFor(module.Kind));

            IReadOnlyList<string> segments = new FolderNamespaceSegments()
                .Between(moduleFolder, fileDirectory, skipFolders);

            return segments.Count > 0
                ? baseNamespace + "." + string.Join(".", segments)
                : baseNamespace;
        }

        private static string FindNearestModuleFolder(
            ModuleRegistry registry,
            string startDirectory,
            out ModuleDescriptorEVO module)
        {
            if (!registry.TryGetNearestModule(GetUnityAssetPath(startDirectory), out module))
                return string.Empty;

            return new ModuleAssetPathResolver().ToAbsolutePath(registry.PathOf(module));
        }
    }
}
#endif