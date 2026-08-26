using System.IO;
using FlowIoC.Editor.CodeGenerator.Menus.Module.ModuleGeneration;
using FlowIoC.Editor.Config.ModuleConfig;
using NUnit.Framework;
using UnityEngine;

namespace FlowIoC.Tests
{
    public class SharedAssemblyDefinitionTests
    {
        private string _root;
        private string _modulePath;
        private MainModuleDirectoryStructureConfig _config;
        private SharedAssemblyDefinition _sharedAssembly;

        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), "FlowIoCSharedAssembly_" + Path.GetRandomFileName());
            _modulePath = Path.Combine(_root, "PlayerModule");
            Directory.CreateDirectory(_modulePath);

            _config = ScriptableObject.CreateInstance<MainModuleDirectoryStructureConfig>();
            _sharedAssembly = new SharedAssemblyDefinition();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_config);

            if (Directory.Exists(_root))
                Directory.Delete(_root, true);
        }

        [Test]
        public void CreateFor_writes_the_Shared_assembly_into_the_Shared_folder()
        {
            CreateSharedFolder();

            string name = _sharedAssembly.CreateFor(_modulePath, _config, "Modules.Player");

            Assert.AreEqual("Modules.Player.Shared", name);
            Assert.IsTrue(File.Exists(Path.Combine(SharedFolder(), "Modules.Player.Shared.asmdef")));
        }

        [Test]
        public void The_written_assembly_carries_the_name_it_was_given()
        {
            CreateSharedFolder();

            _sharedAssembly.CreateFor(_modulePath, _config, "Modules.Player");

            string asmdef = File.ReadAllText(Path.Combine(SharedFolder(), "Modules.Player.Shared.asmdef"));

            StringAssert.Contains("\"name\": \"Modules.Player.Shared\"", asmdef);
        }

        /// <summary>
        /// Shared is an optional folder and the screen and test layouts do not offer it at all, so
        /// a module without one is the ordinary case rather than a fault.
        /// </summary>
        [Test]
        public void CreateFor_writes_nothing_when_the_module_has_no_Shared_folder()
        {
            Assert.IsNull(_sharedAssembly.CreateFor(_modulePath, _config, "Modules.Player"));
            Assert.IsFalse(Directory.Exists(SharedFolder()));
        }

        [Test]
        public void FindIn_hands_back_the_name_of_the_assembly_it_finds()
        {
            CreateSharedFolder();
            _sharedAssembly.CreateFor(_modulePath, _config, "Modules.Player");

            Assert.AreEqual("Modules.Player.Shared", _sharedAssembly.FindIn(_modulePath, _config));
        }

        /// <summary>
        /// The name is read off the file rather than derived from the module, so a module created
        /// before Shared existed - or one whose assembly was renamed since - is still found.
        /// </summary>
        [Test]
        public void FindIn_reads_the_name_off_the_file_rather_than_deriving_it()
        {
            CreateSharedFolder();
            File.WriteAllText(Path.Combine(SharedFolder(), "Renamed.By.Hand.asmdef"), "{}");

            Assert.AreEqual("Renamed.By.Hand", _sharedAssembly.FindIn(_modulePath, _config));
        }

        [Test]
        public void FindIn_finds_nothing_when_the_Shared_folder_holds_no_assembly()
        {
            CreateSharedFolder();

            Assert.IsNull(_sharedAssembly.FindIn(_modulePath, _config));
        }

        [Test]
        public void FindIn_finds_nothing_when_there_is_no_Shared_folder()
        {
            Assert.IsNull(_sharedAssembly.FindIn(_modulePath, _config));
        }

        [Test]
        public void FindIn_finds_nothing_for_a_module_path_that_was_never_given()
        {
            Assert.IsNull(_sharedAssembly.FindIn(null, _config));
            Assert.IsNull(_sharedAssembly.FindIn(_modulePath, null));
        }

        private string SharedFolder() => Path.Combine(_modulePath, "Scripts", "Shared");

        private void CreateSharedFolder() => Directory.CreateDirectory(SharedFolder());
    }
}
