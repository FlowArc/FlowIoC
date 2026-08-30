using System.IO;
using FlowIoC.Editor.ModuleInstall;
using FlowIoC.Editor.SetupModules;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    public class SetupModulesInstallerTests
    {
        private string _projectRoot;
        private string _packageRoot;
        private SetupModulesInstaller _installer;

        [SetUp]
        public void SetUp()
        {
            _projectRoot = Path.Combine(Path.GetTempPath(), "FlowIoCSetupInstall_" + Path.GetRandomFileName());
            _packageRoot = Path.Combine(_projectRoot, "Packages", "FlowIoC");
            Directory.CreateDirectory(Path.Combine(_projectRoot, "Assets"));

            WritePayload("MainModule", "Modules.Main");
            WritePayload("ScreenModule", "Modules.Screen");

            _installer = new SetupModulesInstaller(
                _projectRoot, new ModulesSource(_packageRoot, ModulesSource.SetupModulesFolder));
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_projectRoot))
                Directory.Delete(_projectRoot, true);
        }

        private void WritePayload(string moduleName, string assemblyName)
        {
            string module = Path.Combine(_packageRoot, ModulesSource.SetupModulesFolder, moduleName);
            Directory.CreateDirectory(Path.Combine(module, "Scripts"));
            File.WriteAllText(Path.Combine(module, assemblyName + ".asmdef"), "{\"name\":\"" + assemblyName + "\"}");
            File.WriteAllText(Path.Combine(module, "Scripts", "Placeholder.cs"), "// placeholder");
        }

        private string Installed(string moduleName) =>
            Path.Combine(_projectRoot, "Assets", "Modules", moduleName);

        [Test]
        public void Install_copies_every_module_in_the_set()
        {
            SetupInstallReport report = _installer.Install();

            Assert.IsTrue(report.Succeeded);
            Assert.AreEqual(2, report.Installed.Length);
            Assert.IsTrue(File.Exists(Path.Combine(Installed("MainModule"), "Modules.Main.asmdef")));
            Assert.IsTrue(File.Exists(Path.Combine(Installed("ScreenModule"), "Scripts", "Placeholder.cs")));
        }

        [Test]
        public void An_occupied_target_folder_stops_the_whole_set()
        {
            Directory.CreateDirectory(Installed("ScreenModule"));

            SetupInstallReport report = _installer.Install();

            Assert.IsFalse(report.Succeeded);
            StringAssert.Contains("ScreenModule", report.Blocked);
            Assert.IsFalse(Directory.Exists(Installed("MainModule")), "nothing at all should have been copied");
        }

        [Test]
        public void An_assembly_already_in_the_project_stops_the_whole_set()
        {
            string elsewhere = Path.Combine(_projectRoot, "Assets", "Game", "Renamed");
            Directory.CreateDirectory(elsewhere);
            File.WriteAllText(Path.Combine(elsewhere, "whatever.asmdef"), "{\"name\":\"Modules.Screen\"}");

            SetupInstallReport report = _installer.Install();

            Assert.IsFalse(report.Succeeded);
            StringAssert.Contains("Modules.Screen", report.Blocked);
            Assert.IsFalse(Directory.Exists(Installed("MainModule")));
        }

        [Test]
        public void IsInstalled_is_true_only_once_every_assembly_of_the_set_is_present()
        {
            Assert.IsFalse(_installer.IsInstalled());

            _installer.Install();

            Assert.IsTrue(_installer.IsInstalled());
        }
    }
}
