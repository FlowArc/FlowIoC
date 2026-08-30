using System.Collections.Generic;
using System.IO;
using FlowIoC.Editor.ModuleInstall;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    public class ModuleInstallerTests
    {
        private const string ModuleName = "CountdownServiceModule";
        private const string AssemblyName = "Modules.CountdownService";

        private string _package;
        private string _project;
        private List<string> _registered;

        [SetUp]
        public void SetUp()
        {
            string temp = Path.Combine(Path.GetTempPath(), "FlowIoCModules_" + Path.GetRandomFileName());
            _package = Path.Combine(temp, "package");
            _project = Path.Combine(temp, "project");
            _registered = new List<string>();

            Directory.CreateDirectory(_project);
            WriteShippedModule(ModuleName);
        }

        [TearDown]
        public void TearDown()
        {
            string temp = Directory.GetParent(_project)?.FullName;

            if (temp != null && Directory.Exists(temp))
                Directory.Delete(temp, true);
        }

        [Test]
        public void A_folder_with_an_asmdef_is_a_module_and_one_without_is_not()
        {
            Directory.CreateDirectory(Path.Combine(_package, ModulesSource.ModulesFolder, "NotAModule"));

            Assert.IsTrue(NewSource().TryList(out string[] modules, out string error), error);
            Assert.AreEqual(1, modules.Length);
            Assert.AreEqual(ModuleName, Path.GetFileName(modules[0]));
        }

        [Test]
        public void A_package_that_ships_no_modules_says_so_rather_than_throwing()
        {
            var source = new ModulesSource(Path.Combine(_package, "elsewhere"));

            Assert.IsFalse(source.TryList(out _, out string error));
            Assert.IsNotEmpty(error);
        }

        [Test]
        public void A_module_the_project_does_not_have_is_reported_as_not_installed()
        {
            Assert.IsFalse(NewInstaller().IsInstalled(ModuleName));
        }

        [Test]
        public void Installing_copies_the_whole_tree_into_the_project()
        {
            Assert.IsTrue(NewInstaller().TryInstall(ModuleName, out string error), error);

            Assert.IsTrue(File.Exists(Installed("Modules.CountdownService.asmdef")));
            Assert.IsTrue(File.Exists(Installed("Modules.CountdownService.asmdef.meta")));
            Assert.IsTrue(File.Exists(Installed("Scripts/Runtime/Services/CountdownService.cs")));
        }

        /// <summary>
        /// The meta files travel with the code. They carry the GUIDs a scene or prefab in the
        /// module points at, so leaving them behind would install a module whose own references
        /// are broken.
        /// </summary>
        [Test]
        public void Installing_keeps_the_guids_the_module_shipped_with()
        {
            NewInstaller().TryInstall(ModuleName, out _);

            Assert.AreEqual(
                File.ReadAllText(Shipped("Modules.CountdownService.asmdef.meta")),
                File.ReadAllText(Installed("Modules.CountdownService.asmdef.meta")));
        }

        [Test]
        public void Installing_registers_the_module_with_the_editor()
        {
            NewInstaller().TryInstall(ModuleName, out _);

            CollectionAssert.AreEqual(new[] {ModuleName}, _registered);
        }

        /// <summary>
        /// The copy in the project is the one the game has been editing. Overwriting it because
        /// somebody pressed Install twice would throw that work away, so the second press is
        /// refused and says why.
        /// </summary>
        [Test]
        public void A_module_already_in_the_project_is_left_alone()
        {
            ModuleInstaller installer = NewInstaller();
            installer.TryInstall(ModuleName, out _);

            File.WriteAllText(Installed("Scripts/Runtime/Services/CountdownService.cs"), "edited by the game");

            Assert.IsFalse(installer.TryInstall(ModuleName, out string error));
            Assert.IsNotEmpty(error);
            Assert.AreEqual("edited by the game",
                File.ReadAllText(Installed("Scripts/Runtime/Services/CountdownService.cs")));
        }

        /// <summary>
        /// Once installed the folder belongs to the game, which may rename it. The check has to
        /// follow, or the button offers to install a second copy - and two asmdefs claiming one
        /// assembly name stop the whole project compiling.
        /// </summary>
        [Test]
        public void A_renamed_module_is_still_found()
        {
            ModuleInstaller installer = NewInstaller();
            installer.TryInstall(ModuleName, out _);

            Directory.Move(
                Path.Combine(_project, ModuleInstaller.TargetFolder, ModuleName),
                Path.Combine(_project, ModuleInstaller.TargetFolder, "TimerModule"));

            Assert.IsTrue(installer.IsInstalled(ModuleName));
            Assert.IsFalse(installer.TryInstall(ModuleName, out string error));
            StringAssert.Contains("TimerModule", error);
        }

        /// <summary>
        /// And which may move it somewhere that suits the project better than Assets/Modules.
        /// </summary>
        [Test]
        public void A_module_moved_elsewhere_under_assets_is_still_found()
        {
            ModuleInstaller installer = NewInstaller();
            installer.TryInstall(ModuleName, out _);

            string elsewhere = Path.Combine(_project, "Assets", "Game", "Systems", ModuleName);
            Directory.CreateDirectory(Path.GetDirectoryName(elsewhere));
            Directory.Move(Path.Combine(_project, ModuleInstaller.TargetFolder, ModuleName), elsewhere);

            Assert.IsTrue(installer.IsInstalled(ModuleName));
            StringAssert.Contains("Systems", installer.InstalledAt(ModuleName));
        }

        /// <summary>
        /// A folder in the way that is not the module. Copying into it would mix two things
        /// together, so it is refused - but with a different reason than "already installed".
        /// </summary>
        [Test]
        public void A_foreign_folder_on_the_target_path_is_refused_without_being_touched()
        {
            string target = Path.Combine(_project, ModuleInstaller.TargetFolder, ModuleName);
            Directory.CreateDirectory(target);
            File.WriteAllText(Path.Combine(target, "SomethingElse.cs"), "// not the module");

            ModuleInstaller installer = NewInstaller();

            Assert.IsFalse(installer.IsInstalled(ModuleName));
            Assert.IsFalse(installer.TryInstall(ModuleName, out string error));
            StringAssert.Contains(AssemblyName, error);
            Assert.IsFalse(File.Exists(Path.Combine(target, "Modules.CountdownService.asmdef")));
        }

        [Test]
        public void A_module_the_package_does_not_ship_is_reported_rather_than_installed()
        {
            Assert.IsFalse(NewInstaller().TryInstall("NoSuchModule", out string error));
            Assert.IsNotEmpty(error);
            Assert.IsFalse(Directory.Exists(Path.Combine(_project, ModuleInstaller.TargetFolder, "NoSuchModule")));
        }

        private ModulesSource NewSource() => new ModulesSource(_package);

        private ModuleInstaller NewInstaller() =>
            new ModuleInstaller(_project, NewSource(), name => _registered.Add(name));

        private void WriteShippedModule(string moduleName)
        {
            string root = Path.Combine(_package, ModulesSource.ModulesFolder, moduleName);
            string services = Path.Combine(root, "Scripts", "Runtime", "Services");

            Directory.CreateDirectory(services);

            File.WriteAllText(Path.Combine(root, "Modules.CountdownService.asmdef"),
                "{\n  \"name\": \"" + AssemblyName + "\",\n  \"references\": [\"FlowIoC\"]\n}");
            File.WriteAllText(Path.Combine(root, "Modules.CountdownService.asmdef.meta"),
                "fileFormatVersion: 2\nguid: 0123456789abcdef0123456789abcdef");
            File.WriteAllText(Path.Combine(services, "CountdownService.cs"), "// shipped");
        }

        private string Shipped(string relativePath) =>
            Path.Combine(_package, ModulesSource.ModulesFolder, ModuleName, relativePath);

        private string Installed(string relativePath) =>
            Path.Combine(_project, ModuleInstaller.TargetFolder, ModuleName, relativePath);
    }
}