#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;

namespace FlowIoC.Editor.CodeStyle
{
    /// <summary>
    /// Writes, beside each project file Unity generates for the package's own assemblies, the
    /// settings that keep Rider's namespace inspection quiet inside the package.
    ///
    /// Rider derives the namespace it expects from the folders above a file, and a package
    /// installed from a registry sits under <c>Library/PackageCache/com.flowarc.flowioc.core@hash</c>
    /// - so every file in it is told to move to a namespace with the cache folder's name in it.
    /// The folders down to the assembly's own are marked as no namespace provider in
    /// <c>FlowIoC.csproj.DotSettings</c> and <c>FlowIoC.Editor.csproj.DotSettings</c> at the
    /// project root, which is where Rider reads a project's settings from. The hash changes with
    /// every version, so this is written on startup from where the package actually resolved
    /// rather than shipped, and the keys of an earlier hash are dropped as the new ones go in.
    /// Whatever else a file holds is kept.
    ///
    /// A folder named <c>Editor</c> inside an assembly is marked as a provider, because Rider
    /// treats one as Unity's special folder otherwise and drops it from the namespace, and the
    /// package names its namespaces after every folder.
    /// </summary>
    internal class PackageNamespaceFoldersWriter
    {
        internal const string SettingsExtension = ".csproj.DotSettings";
        private const string AssemblyDefinitionExtension = ".asmdef";
        private const string ProviderFolderName = "Editor";

        private readonly string _projectRoot;
        private readonly string _packageRoot;
        private readonly NamespaceSkipKey _key = new NamespaceSkipKey();
        private readonly DotSettingsDocument _file = new DotSettingsDocument();

        internal PackageNamespaceFoldersWriter(string projectRoot, string packageRoot)
        {
            _projectRoot = Path.GetFullPath(projectRoot);
            _packageRoot = Path.GetFullPath(packageRoot);
        }

        internal PackageNamespaceFoldersReport Run()
        {
            if (!TryRelativeToProject(_packageRoot, out string packageFolder))
                return new PackageNamespaceFoldersReport(Array.Empty<string>(), null);

            var written = new List<string>();

            try
            {
                foreach (string assemblyDefinition in FindAssemblyDefinitions(_packageRoot))
                {
                    string path = Write(assemblyDefinition, packageFolder, out bool changed);

                    if (changed)
                        written.Add(path);
                }
            }
            catch (Exception exception)
            {
                return new PackageNamespaceFoldersReport(written.ToArray(),
                    $"FlowIoC could not write the package's namespace folders: {exception.Message}");
            }

            return new PackageNamespaceFoldersReport(written.ToArray(), null);
        }

        private string Write(string assemblyDefinition, string packageFolder, out bool changed)
        {
            string name = AssemblyName(assemblyDefinition);
            string path = Path.Combine(_projectRoot, name + SettingsExtension);

            Dictionary<string, SettingsEntry> entries = File.Exists(path)
                ? _file.Read(path)
                : new Dictionary<string, SettingsEntry>(StringComparer.Ordinal);

            DropEarlierVersions(entries, packageFolder);

            foreach (KeyValuePair<string, bool> folder in Folders(assemblyDefinition))
            {
                foreach (string key in _key.For(folder.Key))
                    entries[key] = new SettingsEntry("Boolean", folder.Value ? "True" : "False");
            }

            string content = _file.Compose(entries);

            changed = !File.Exists(path)
                      || !string.Equals(File.ReadAllText(path), content, StringComparison.Ordinal);

            if (changed)
                File.WriteAllText(path, content);

            return path;
        }

        /// <summary>
        /// The folders one assembly needs, project-relative, each with whether Rider is to skip
        /// it as a namespace provider: every folder from the project down to the assembly's own is
        /// skipped, and a folder named Editor under the assembly is not.
        /// </summary>
        internal IReadOnlyList<KeyValuePair<string, bool>> Folders(string assemblyDefinition)
        {
            var folders = new List<KeyValuePair<string, bool>>();

            string assemblyFolder = Path.GetDirectoryName(assemblyDefinition) ?? _packageRoot;

            if (!TryRelativeToProject(assemblyFolder, out string assemblyRelative))
                return folders;

            string[] segments = assemblyRelative.Split('\\');
            var path = string.Empty;

            foreach (string segment in segments)
            {
                path = path.Length == 0 ? segment : path + "\\" + segment;
                folders.Add(new KeyValuePair<string, bool>(path, true));
            }

            foreach (string provider in Directory.GetDirectories(assemblyFolder, ProviderFolderName, SearchOption.AllDirectories))
            {
                if (Path.GetFileName(provider) != ProviderFolderName || IsHidden(provider, assemblyFolder))
                    continue;

                if (TryRelativeToProject(provider, out string providerRelative))
                    folders.Add(new KeyValuePair<string, bool>(providerRelative, false));
            }

            return folders;
        }

        /// <summary>
        /// A cache folder is the package's name, an at sign and a hash, and the hash is another
        /// one after every update. Any key spelt with the same name and at sign but written
        /// before is the old version's, and goes.
        /// </summary>
        private void DropEarlierVersions(Dictionary<string, SettingsEntry> entries, string packageFolder)
        {
            int at = packageFolder.LastIndexOf('@');

            if (at < 0 || packageFolder.IndexOf('\\', at) >= 0)
                return;

            var stale = new List<string>();

            foreach (string spelling in _key.Spellings(packageFolder.Substring(0, at + 1)))
            {
                string prefix = NamespaceSkipKey.Prefix + spelling;

                foreach (string key in entries.Keys)
                {
                    if (key.StartsWith(prefix, StringComparison.Ordinal))
                        stale.Add(key);
                }
            }

            foreach (string key in stale)
                entries.Remove(key);
        }

        private bool TryRelativeToProject(string fullPath, out string relative)
        {
            string root = _projectRoot.TrimEnd('\\', '/') + Path.DirectorySeparatorChar;
            string path = Path.GetFullPath(fullPath);

            if (!path.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            {
                relative = null;
                return false;
            }

            relative = path.Substring(root.Length).Replace('/', '\\').Trim('\\');
            return relative.Length > 0;
        }

        /// <summary>
        /// The assemblies the package compiles: every asmdef under it that Unity sees. A folder
        /// ending in a tilde or starting with a dot is hidden from Unity - the modules the package
        /// ships to be installed live in one - and its asmdefs are not projects of this solution.
        /// </summary>
        private static IEnumerable<string> FindAssemblyDefinitions(string folder)
        {
            foreach (string file in Directory.GetFiles(folder, "*" + AssemblyDefinitionExtension, SearchOption.TopDirectoryOnly))
                yield return file;

            foreach (string child in Directory.GetDirectories(folder))
            {
                string name = Path.GetFileName(child);

                if (name.EndsWith("~", StringComparison.Ordinal) || name.StartsWith(".", StringComparison.Ordinal))
                    continue;

                foreach (string file in FindAssemblyDefinitions(child))
                    yield return file;
            }
        }

        private static bool IsHidden(string folder, string under)
        {
            string relative = Path.GetFullPath(folder).Substring(Path.GetFullPath(under).Length);

            foreach (string segment in relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
            {
                if (segment.EndsWith("~", StringComparison.Ordinal) || segment.StartsWith(".", StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        private static string AssemblyName(string assemblyDefinition)
        {
            JToken name = JObject.Parse(File.ReadAllText(assemblyDefinition))["name"];

            return name == null || name.ToString().Length == 0
                ? Path.GetFileNameWithoutExtension(assemblyDefinition)
                : name.ToString();
        }
    }

    /// <summary>
    /// What one run wrote. <see cref="WrittenPaths"/> is empty when every file already said what
    /// the package's resolved path says, which is every session after the first on one version.
    /// </summary>
    internal readonly struct PackageNamespaceFoldersReport
    {
        internal string[] WrittenPaths { get; }
        internal string Error { get; }

        internal PackageNamespaceFoldersReport(string[] writtenPaths, string error)
        {
            WrittenPaths = writtenPaths ?? Array.Empty<string>();
            Error = error;
        }
    }
}

#endif
