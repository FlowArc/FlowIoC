using System.Collections.Generic;
using System.IO;
using System.Linq;
using FlowIoC.Editor.CodeStyle;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    /// <summary>
    /// The package's own namespace folders, written against a project laid out in a temporary
    /// directory: a package resolved into the cache, its three assemblies, and the settings
    /// files that have to appear at the root for Rider to read.
    /// </summary>
    public class PackageNamespaceFoldersWriterTests
    {
        private const string Prefix = "/Default/CodeInspection/NamespaceProvider/NamespaceFoldersToSkip/=";
        private const string Suffix = "/@EntryIndexedValue";
        private const string CachePath = "com.flowarc.flowioc.core@b41df353fcba";
        private const string CacheKey = "library_005Cpackagecache_005Ccom_002Eflowarc_002Eflowioc_002Ecore_0040b41df353fcba";

        private string _root;
        private string _package;

        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), "FlowIoCNamespaceFolders_" + Path.GetRandomFileName());
            _package = Path.Combine(_root, "Library", "PackageCache", CachePath);

            Assembly("Runtime", "FlowIoC");
            Assembly("Editor", "FlowIoC.Editor");
            Assembly(Path.Combine("Tests", "Editor"), "FlowIoC.Tests");

            Directory.CreateDirectory(Path.Combine(_package, "Editor", "CodeGenerator", "Editor"));
            Directory.CreateDirectory(Path.Combine(_package, "Modules~", "HiddenModule"));
            File.WriteAllText(Path.Combine(_package, "Modules~", "HiddenModule", "Modules.Hidden.asmdef"),
                "{\"name\": \"Modules.Hidden\"}");
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_root))
                Directory.Delete(_root, true);
        }

        [Test]
        public void One_settings_file_is_written_per_assembly_the_package_compiles()
        {
            PackageNamespaceFoldersReport report = Writer().Run();

            Assert.IsNull(report.Error);
            CollectionAssert.AreEquivalent(
                new[] {"FlowIoC.csproj.DotSettings", "FlowIoC.Editor.csproj.DotSettings", "FlowIoC.Tests.csproj.DotSettings"},
                report.WrittenPaths.Select(Path.GetFileName));
            FileAssert.DoesNotExist(Path.Combine(_root, "Modules.Hidden.csproj.DotSettings"));
        }

        [Test]
        public void Every_folder_down_to_the_assembly_is_no_namespace_provider()
        {
            Writer().Run();

            Dictionary<string, SettingsEntry> entries = Entries("FlowIoC.Tests.csproj.DotSettings");

            Assert.AreEqual("True", entries[Prefix + "library" + Suffix].Value);
            Assert.AreEqual("True", entries[Prefix + "library_005Cpackagecache" + Suffix].Value);
            Assert.AreEqual("True", entries[Prefix + CacheKey + Suffix].Value);
            Assert.AreEqual("True", entries[Prefix + CacheKey + "_005Ctests" + Suffix].Value);
            Assert.AreEqual("True", entries[Prefix + CacheKey + "_005Ctests_005Ceditor" + Suffix].Value);
        }

        /// <summary>
        /// Rider drops a folder named Editor from the namespace on its own, as Unity's special
        /// folder, and the package's namespaces name every folder - so one nested under the
        /// assembly is put back as a provider.
        /// </summary>
        [Test]
        public void An_Editor_folder_inside_the_assembly_is_a_namespace_provider()
        {
            Writer().Run();

            Dictionary<string, SettingsEntry> entries = Entries("FlowIoC.Editor.csproj.DotSettings");

            Assert.AreEqual("True", entries[Prefix + CacheKey + "_005Ceditor" + Suffix].Value);
            Assert.AreEqual("False", entries[Prefix + CacheKey + "_005Ceditor_005Ccodegenerator_005Ceditor" + Suffix].Value);
        }

        [Test]
        public void A_second_run_on_the_same_path_writes_nothing()
        {
            Writer().Run();

            PackageNamespaceFoldersReport report = Writer().Run();

            Assert.IsEmpty(report.WrittenPaths);
        }

        /// <summary>
        /// An update moves the package to another hash. The keys written for the old one would
        /// otherwise stay for ever, one set per version ever installed.
        /// </summary>
        [Test]
        public void The_keys_of_an_earlier_version_are_dropped()
        {
            const string earlier = Prefix + "library_005Cpackagecache_005Ccom_002Eflowarc_002Eflowioc_002Ecore_00400ldhash_005Cruntime" + Suffix;

            File.WriteAllText(Path.Combine(_root, "FlowIoC.csproj.DotSettings"),
                Document(Entry(earlier, "True")));

            Writer().Run();

            Dictionary<string, SettingsEntry> entries = Entries("FlowIoC.csproj.DotSettings");

            Assert.IsFalse(entries.ContainsKey(earlier));
            Assert.IsTrue(entries.ContainsKey(Prefix + CacheKey + "_005Cruntime" + Suffix));
        }

        [Test]
        public void A_key_of_somebody_elses_is_kept()
        {
            const string foreign = "/Default/CodeInspection/Highlighting/InspectionSeverities/=CheckNamespace/@EntryIndexedValue";

            File.WriteAllText(Path.Combine(_root, "FlowIoC.csproj.DotSettings"),
                Document(Entry(foreign, "DO_NOT_SHOW")));

            Writer().Run();

            Assert.AreEqual("DO_NOT_SHOW", Entries("FlowIoC.csproj.DotSettings")[foreign].Value);
        }

        /// <summary>A package embedded under Packages/ has no hash and is written all the same.</summary>
        [Test]
        public void An_embedded_package_is_written_under_its_own_folder()
        {
            string embedded = Path.Combine(_root, "Packages", "FlowIoC");
            Directory.CreateDirectory(Path.Combine(embedded, "Runtime"));
            File.WriteAllText(Path.Combine(embedded, "Runtime", "FlowIoC.asmdef"), "{\"name\": \"FlowIoC\"}");

            new PackageNamespaceFoldersWriter(_root, embedded).Run();

            Dictionary<string, SettingsEntry> entries = Entries("FlowIoC.csproj.DotSettings");

            Assert.AreEqual("True", entries[Prefix + "packages_005Cflowioc_005Cruntime" + Suffix].Value);
            Assert.AreEqual("True", entries[Prefix + "packages_005Cflow_0131oc_005Cruntime" + Suffix].Value);
        }

        [Test]
        public void A_package_outside_the_project_writes_nothing()
        {
            string outside = Path.Combine(Path.GetTempPath(), "FlowIoCOutside_" + Path.GetRandomFileName());
            Directory.CreateDirectory(Path.Combine(outside, "Runtime"));
            File.WriteAllText(Path.Combine(outside, "Runtime", "FlowIoC.asmdef"), "{\"name\": \"FlowIoC\"}");

            try
            {
                PackageNamespaceFoldersReport report = new PackageNamespaceFoldersWriter(_root, outside).Run();

                Assert.IsNull(report.Error);
                Assert.IsEmpty(report.WrittenPaths);
            }
            finally
            {
                Directory.Delete(outside, true);
            }
        }

        private PackageNamespaceFoldersWriter Writer() => new PackageNamespaceFoldersWriter(_root, _package);

        private void Assembly(string folder, string name)
        {
            string path = Path.Combine(_package, folder);
            Directory.CreateDirectory(path);
            File.WriteAllText(Path.Combine(path, name + ".asmdef"), "{\"name\": \"" + name + "\"}");
        }

        private Dictionary<string, SettingsEntry> Entries(string fileName) =>
            new DotSettingsFile().Read(Path.Combine(_root, fileName));

        private static string Entry(string key, string value) =>
            "\t<s:String x:Key=\"" + key + "\">" + value + "</s:String>\n";

        private static string Document(params string[] entries) =>
            "<wpf:ResourceDictionary xml:space=\"preserve\""
            + " xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\""
            + " xmlns:s=\"clr-namespace:System;assembly=mscorlib\""
            + " xmlns:ss=\"urn:shemas-jetbrains-com:settings-storage-xaml\""
            + " xmlns:wpf=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\">\n"
            + string.Concat(entries)
            + "</wpf:ResourceDictionary>\n";
    }
}
