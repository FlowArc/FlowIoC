using System.IO;
using FlowIoC.Editor.CodeStyle;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    public class SolutionDotSettingsWriterTests
    {
        private const string ShippedKey = "/Default/CodeStyle/Naming/CSharpNaming/ApplyAutoDetectedRules/@EntryValue";
        private const string ForeignKey = "/Default/CodeStyle/CodeFormatting/CSharpFormat/ALIGN_MULTILINE_PARAMETER/@EntryValue";

        private string _root;
        private string _templatePath;

        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), "FlowIoCSolutionDotSettings_" + Path.GetRandomFileName());
            Directory.CreateDirectory(_root);

            _templatePath = Path.Combine(_root, "template.DotSettings");
            File.WriteAllText(_templatePath, Document(
                Entry("Boolean", ShippedKey, "False"),
                Entry("String", "/Default/CodeStyle/Naming/CSharpNaming/UserRules/=abc/@EntryIndexedValue",
                    "&lt;Policy Prefix=\"_\" /&gt;")));
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_root))
                Directory.Delete(_root, true);
        }

        [Test]
        public void TryWrite_names_the_file_after_the_solution()
        {
            File.WriteAllText(Path.Combine(_root, "MyGame.sln"), string.Empty);

            bool ok = Writer().TryWrite(out string path, out string error);

            Assert.IsTrue(ok, error);
            Assert.AreEqual("MyGame.sln.DotSettings", Path.GetFileName(path));
            FileAssert.Exists(path);
        }

        [Test]
        public void TryWrite_falls_back_to_the_folder_name_when_no_solution_exists()
        {
            Writer().TryWrite(out string path, out string error);

            Assert.IsNotNull(path, error);
            Assert.AreEqual(new DirectoryInfo(_root).Name + ".sln.DotSettings", Path.GetFileName(path));
        }

        [Test]
        public void TryWrite_prefers_the_solution_that_matches_the_project_folder()
        {
            File.WriteAllText(Path.Combine(_root, "Aardvark.sln"), string.Empty);
            File.WriteAllText(Path.Combine(_root, new DirectoryInfo(_root).Name + ".sln"), string.Empty);

            Writer().TryWrite(out string path, out _);

            Assert.AreEqual(new DirectoryInfo(_root).Name + ".sln.DotSettings", Path.GetFileName(path));
        }

        [Test]
        public void TryWrite_keeps_keys_it_does_not_own_and_replaces_the_ones_it_does()
        {
            File.WriteAllText(Path.Combine(_root, "MyGame.sln"), string.Empty);
            File.WriteAllText(Path.Combine(_root, "MyGame.sln.DotSettings"), Document(
                Entry("Boolean", ShippedKey, "True"),
                Entry("Boolean", ForeignKey, "True")));

            bool ok = Writer().TryWrite(out string path, out string error);

            Assert.IsTrue(ok, error);
            string written = File.ReadAllText(path);
            StringAssert.Contains(ForeignKey, written);
            StringAssert.Contains($"x:Key=\"{ShippedKey}\">False<", written);
            StringAssert.DoesNotContain($"x:Key=\"{ShippedKey}\">True<", written);
        }

        [Test]
        public void TryWrite_round_trips_an_escaped_policy_value()
        {
            Writer().TryWrite(out string path, out _);

            StringAssert.Contains("&lt;Policy Prefix=\"_\" /&gt;", File.ReadAllText(path));
        }

        [Test]
        public void TryWrite_fails_with_a_message_when_the_shipped_style_is_missing()
        {
            var writer = new SolutionDotSettingsWriter(_root, Path.Combine(_root, "absent.DotSettings"));

            bool ok = writer.TryWrite(out string path, out string error);

            Assert.IsFalse(ok);
            Assert.IsNull(path);
            StringAssert.Contains("absent.DotSettings", error);
        }

        [Test]
        public void CleanupOrphaned_deletes_settings_whose_solution_is_gone()
        {
            File.WriteAllText(Path.Combine(_root, "MyGame.sln"), string.Empty);
            File.WriteAllText(Path.Combine(_root, "MyGame.sln.DotSettings"), Document());
            File.WriteAllText(Path.Combine(_root, "Renamed.sln.DotSettings"), Document());
            File.WriteAllText(Path.Combine(_root, "MyGame.sln.DotSettings.user"), Document());

            string[] removed = Writer().CleanupOrphaned();

            Assert.AreEqual(1, removed.Length);
            Assert.AreEqual("Renamed.sln.DotSettings", Path.GetFileName(removed[0]));
            FileAssert.Exists(Path.Combine(_root, "MyGame.sln.DotSettings"));
            FileAssert.Exists(Path.Combine(_root, "MyGame.sln.DotSettings.user"));
        }

        [Test]
        public void CleanupOrphaned_leaves_everything_alone_when_no_solution_exists()
        {
            File.WriteAllText(Path.Combine(_root, "MyGame.sln.DotSettings"), Document());

            string[] removed = Writer().CleanupOrphaned();

            Assert.IsEmpty(removed);
            FileAssert.Exists(Path.Combine(_root, "MyGame.sln.DotSettings"));
        }

        private SolutionDotSettingsWriter Writer() => new SolutionDotSettingsWriter(_root, _templatePath);

        private static string Document(params string[] entries) =>
            "<wpf:ResourceDictionary xml:space=\"preserve\""
            + " xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\""
            + " xmlns:s=\"clr-namespace:System;assembly=mscorlib\""
            + " xmlns:ss=\"urn:shemas-jetbrains-com:settings-storage-xaml\""
            + " xmlns:wpf=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\">\n"
            + string.Join("\n", entries)
            + "\n</wpf:ResourceDictionary>";

        private static string Entry(string elementName, string key, string value) =>
            $"\t<s:{elementName} x:Key=\"{key}\">{value}</s:{elementName}>";
    }
}
