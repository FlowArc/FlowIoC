using System.IO;
using FlowIoC.Editor.CodeStyle;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    public class SolutionCodeStyleAutoInstallTests
    {
        private const string ShippedKey = "/Default/CodeStyle/Naming/CSharpNaming/ApplyAutoDetectedRules/@EntryValue";

        private string _root;
        private string _templatePath;

        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), "FlowIoCSolutionCodeStyle_" + Path.GetRandomFileName());
            Directory.CreateDirectory(_root);

            _templatePath = Path.Combine(_root, "template.DotSettings");
            File.WriteAllText(_templatePath, Document(Entry("Boolean", ShippedKey, "False")));
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_root))
                Directory.Delete(_root, true);
        }

        [Test]
        public void Run_writes_the_settings_file_a_fresh_project_does_not_have()
        {
            File.WriteAllText(Path.Combine(_root, "MyGame.sln"), string.Empty);

            SolutionCodeStyleReport report = AutoInstall().Run();

            Assert.IsNull(report.Error);
            Assert.AreEqual("MyGame.sln.DotSettings", Path.GetFileName(report.WrittenPath));
            StringAssert.Contains(ShippedKey, File.ReadAllText(report.WrittenPath));
        }

        [Test]
        public void Run_writes_nothing_the_second_time_around()
        {
            File.WriteAllText(Path.Combine(_root, "MyGame.sln"), string.Empty);
            AutoInstall().Run();

            SolutionCodeStyleReport report = AutoInstall().Run();

            Assert.IsNull(report.Error);
            Assert.IsNull(report.WrittenPath);
        }

        [Test]
        public void Run_reports_the_error_when_the_package_ships_no_code_style()
        {
            var install = new SolutionCodeStyleAutoInstall(_root, Path.Combine(_root, "absent.DotSettings"));

            SolutionCodeStyleReport report = install.Run();

            Assert.IsNull(report.WrittenPath);
            StringAssert.Contains("absent.DotSettings", report.Error);
        }

        [Test]
        public void Run_sweeps_the_settings_of_a_solution_that_is_gone()
        {
            File.WriteAllText(Path.Combine(_root, "MyGame.sln"), string.Empty);
            string orphan = Path.Combine(_root, "OldName.sln.DotSettings");
            File.WriteAllText(orphan, string.Empty);

            SolutionCodeStyleReport report = AutoInstall().Run();

            Assert.IsNull(report.Error);
            Assert.IsFalse(File.Exists(orphan));
            CollectionAssert.AreEqual(new[] {orphan}, report.RemovedPaths);
            Assert.AreEqual("MyGame.sln.DotSettings", Path.GetFileName(report.WrittenPath));
        }

        [Test]
        public void Run_sweeps_nothing_while_no_solution_exists_to_compare_against()
        {
            string settings = Path.Combine(_root, "OldName.sln.DotSettings");
            File.WriteAllText(settings, string.Empty);

            SolutionCodeStyleReport report = AutoInstall().Run();

            Assert.IsTrue(File.Exists(settings));
            Assert.IsEmpty(report.RemovedPaths);
        }

        [Test]
        public void Run_removes_nothing_the_second_time_around()
        {
            File.WriteAllText(Path.Combine(_root, "MyGame.sln"), string.Empty);
            File.WriteAllText(Path.Combine(_root, "OldName.sln.DotSettings"), string.Empty);
            AutoInstall().Run();

            SolutionCodeStyleReport report = AutoInstall().Run();

            Assert.IsEmpty(report.RemovedPaths);
        }

        private SolutionCodeStyleAutoInstall AutoInstall() => new SolutionCodeStyleAutoInstall(_root, _templatePath);

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
