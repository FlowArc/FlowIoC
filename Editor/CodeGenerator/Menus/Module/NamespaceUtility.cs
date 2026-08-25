#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
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

        public static bool IsFolderNamespaceProvider(string folderPath)
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

            string fileName = "Project.DotSettings";
            string finalDotSettingsPath = Path.Combine(projectRoot, fileName);

            if (!File.Exists(finalDotSettingsPath))
                return true;

            var doc = new XmlDocument();
            doc.Load(finalDotSettingsPath);

            var nsManager = new XmlNamespaceManager(doc.NameTable);
            nsManager.AddNamespace("x", XAML_NAMESPACE);
            nsManager.AddNamespace("s", XAML_ASSEMBLY);

            string relativePath = NormalizePath(folderPath)
                .Replace(NormalizePath(Application.dataPath), "Assets")
                .Replace("/", "_005C");

            if (string.IsNullOrEmpty(relativePath))
                return true;

            string key = $"/Default/CodeInspection/NamespaceProvider/NamespaceFoldersToSkip/={relativePath}/@EntryIndexedValue";

            XmlNode skipNode = doc.SelectSingleNode($"//s:Boolean[@x:Key='{key}']", nsManager);
            if (skipNode != null &&
                skipNode.InnerText.Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return true;
        }

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

            string relativePath = EncodeAssetPath(assetFolderPath);
            string lowercasePath = EncodeLowercaseAssetPath(assetFolderPath);

            if (string.IsNullOrWhiteSpace(relativePath))
            {
                Debug.LogError($"Relative path for '{assetFolderPath}' is empty. Skipping.");
                return;
            }

            XmlElement codeGeneratedSection = GetOrCreateCodeGeneratedSection(doc);

            if (isNamespaceProvider)
            {
                RemoveSkipEntry(codeGeneratedSection, nsManager, relativePath);
                if (lowercasePath != relativePath)
                    RemoveSkipEntry(codeGeneratedSection, nsManager, lowercasePath);
            }
            else
            {
                AddOrUpdateSkipEntry(doc, codeGeneratedSection, nsManager, relativePath);
                if (lowercasePath != relativePath)
                    AddOrUpdateSkipEntry(doc, codeGeneratedSection, nsManager, lowercasePath);
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
            string relativePath = EncodeAssetPath(assetFolderPath);
            string lowercasePath = EncodeLowercaseAssetPath(assetFolderPath);

            if (string.IsNullOrWhiteSpace(relativePath))
            {
                Debug.LogError($"Relative path for '{assetFolderPath}' is empty. Skipping.");
                return;
            }

            XmlNamespaceManager nsManager = new XmlNamespaceManager(doc.NameTable);
            nsManager.AddNamespace("x", XAML_NAMESPACE);
            nsManager.AddNamespace("s", XAML_ASSEMBLY);

            XmlElement codeGeneratedSection = GetOrCreateCodeGeneratedSection(doc);

            AddOrUpdateSkipEntry(doc, codeGeneratedSection, nsManager, relativePath);

            if (lowercasePath != relativePath)
            {
                AddOrUpdateSkipEntry(doc, codeGeneratedSection, nsManager, lowercasePath);
            }
        }

        private static string NormalizePath(string path)
        {
            return path.Replace('\\', '/');
        }

        internal static string EncodeAssetPath(string assetFolderPath)
        {
            return NormalizePath(assetFolderPath)
                .Replace(NormalizePath(Application.dataPath), "Assets")
                .Replace("/", "_005C");
        }

        internal static string EncodeLowercaseAssetPath(string assetFolderPath)
        {
            string relativePath = NormalizePath(assetFolderPath)
                .Replace(NormalizePath(Application.dataPath), "Assets");

            string lowered = relativePath.ToLower(TurkishCulture);

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
            registry.TryGetNearestModule(GetUnityAssetPath(modulePath), out ModuleDescriptor module);

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

            string moduleFolder = FindNearestModuleFolder(registry, fileDirectory);
            if (string.IsNullOrEmpty(moduleFolder))
            {
                return "Modules";
            }

            string baseNamespace = GetModuleNamespace(registry, moduleFolder);

            string relativeSubPath = "";
            try
            {
                relativeSubPath = Path.GetRelativePath(moduleFolder, fileDirectory);
            }
            catch
            {
                if (fileDirectory.StartsWith(moduleFolder))
                {
                    relativeSubPath = fileDirectory.Substring(moduleFolder.Length);
                }
            }

            if (string.IsNullOrWhiteSpace(relativeSubPath))
            {
                return baseNamespace;
            }

            char[] trimChars = {Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar};
            relativeSubPath = relativeSubPath.Trim(trimChars);

            string[] subFolders = relativeSubPath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            List<string> includedFolders = new List<string>();

            string currentPath = moduleFolder;
            foreach (string sf in subFolders)
            {
                if (string.IsNullOrEmpty(sf))
                    continue;

                currentPath = Path.Combine(currentPath, sf);

                if (IsFolderNamespaceProvider(currentPath))
                {
                    includedFolders.Add(sf);
                }
            }

            if (includedFolders.Count > 0)
            {
                return baseNamespace + "." + string.Join(".", includedFolders);
            }

            return baseNamespace;
        }

        private static string FindNearestModuleFolder(ModuleRegistry registry, string startDirectory)
        {
            if (!registry.TryGetNearestModule(GetUnityAssetPath(startDirectory), out ModuleDescriptor module))
                return string.Empty;

            return new ModuleAssetPathResolver().ToAbsolutePath(registry.PathOf(module));
        }
    }
}
#endif