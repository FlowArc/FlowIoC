using System.IO;
using FlowIoC.Editor.CodeGenerator.Menus.Module.ModuleGeneration;
using FlowIoC.Editor.Config.ModuleConfig;
using NUnit.Framework;
using UnityEngine;

namespace FlowIoC.Tests
{
    public class SignalsAssemblyDefinitionTests
    {
        private string _root;
        private string _modulePath;
        private ED_MainModuleDirectoryStructure _config;
        private SignalsAssemblyDefinition _signalsAssembly;

        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), "FlowIoCSignalsAssembly_" + Path.GetRandomFileName());
            _modulePath = Path.Combine(_root, "PlayerModule");
            Directory.CreateDirectory(_modulePath);

            _config = ScriptableObject.CreateInstance<ED_MainModuleDirectoryStructure>();
            _signalsAssembly = new SignalsAssemblyDefinition();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_config);

            if (Directory.Exists(_root))
                Directory.Delete(_root, true);
        }

        [Test]
        public void CreateFor_writes_the_Signals_assembly_into_the_Signals_folder()
        {
            CreateSignalsFolder();

            string name = _signalsAssembly.CreateFor(_modulePath, _config, "Modules.Player");

            Assert.AreEqual("Modules.Player.Signals", name);
            Assert.IsTrue(File.Exists(Path.Combine(SignalsFolder(), "Modules.Player.Signals.asmdef")));
        }

        /// <summary>
        /// The folder is Scripts/Signals, a sibling of Runtime and Shared - not Scripts/Shared/
        /// Signals, where the holder used to sit. Putting it back inside Shared would mean that
        /// referencing a module's published data handed the reader its signals too.
        /// </summary>
        [Test]
        public void The_Signals_folder_is_a_sibling_of_Runtime_rather_than_a_child_of_Shared()
        {
            CreateSignalsFolder();

            _signalsAssembly.CreateFor(_modulePath, _config, "Modules.Player");

            Assert.AreEqual(Path.Combine(_modulePath, "Scripts", "Signals"), SignalsFolder());
            Assert.IsFalse(Directory.Exists(Path.Combine(_modulePath, "Scripts", "Shared", "Signals")));
        }

        /// <summary>
        /// A public signal may be generic over a type the module publishes, so the holder's
        /// assembly has to see the Shared assembly that type lives in.
        /// </summary>
        [Test]
        public void The_written_assembly_carries_the_references_it_was_given()
        {
            CreateSignalsFolder();

            _signalsAssembly.CreateFor(_modulePath, _config, "Modules.Player", "Modules.Player.Shared");

            string asmdef = File.ReadAllText(Path.Combine(SignalsFolder(), "Modules.Player.Signals.asmdef"));

            StringAssert.Contains("\"name\": \"Modules.Player.Signals\"", asmdef);
            StringAssert.Contains("\"Modules.Player.Shared\"", asmdef);
        }

        /// <summary>
        /// A module that publishes no data is handed no Shared assembly name, and the template
        /// drops the empty entry rather than writing a reference to an assembly nothing produces.
        /// </summary>
        [Test]
        public void A_reference_that_was_never_found_is_not_written()
        {
            CreateSignalsFolder();

            _signalsAssembly.CreateFor(_modulePath, _config, "Modules.Player", null);

            string asmdef = File.ReadAllText(Path.Combine(SignalsFolder(), "Modules.Player.Signals.asmdef"));

            StringAssert.Contains("\"FlowIoC\"", asmdef);
            StringAssert.DoesNotContain("\"\"", asmdef);
        }

        [Test]
        public void CreateFor_writes_nothing_when_the_module_has_no_Signals_folder()
        {
            Assert.IsNull(_signalsAssembly.CreateFor(_modulePath, _config, "Modules.Player"));
            Assert.IsFalse(Directory.Exists(SignalsFolder()));
        }

        [Test]
        public void FindIn_hands_back_the_name_of_the_assembly_it_finds()
        {
            CreateSignalsFolder();
            _signalsAssembly.CreateFor(_modulePath, _config, "Modules.Player");

            Assert.AreEqual("Modules.Player.Signals", _signalsAssembly.FindIn(_modulePath, _config));
        }

        [Test]
        public void FindIn_reads_the_name_off_the_file_rather_than_deriving_it()
        {
            CreateSignalsFolder();
            File.WriteAllText(Path.Combine(SignalsFolder(), "Renamed.By.Hand.asmdef"), "{}");

            Assert.AreEqual("Renamed.By.Hand", _signalsAssembly.FindIn(_modulePath, _config));
        }

        [Test]
        public void FindIn_finds_nothing_when_the_Signals_folder_holds_no_assembly()
        {
            CreateSignalsFolder();

            Assert.IsNull(_signalsAssembly.FindIn(_modulePath, _config));
        }

        [Test]
        public void FindIn_finds_nothing_for_a_module_path_that_was_never_given()
        {
            Assert.IsNull(_signalsAssembly.FindIn(null, _config));
            Assert.IsNull(_signalsAssembly.FindIn(_modulePath, null));
        }

        private string SignalsFolder() => Path.Combine(_modulePath, "Scripts", "Signals");

        private void CreateSignalsFolder() => Directory.CreateDirectory(SignalsFolder());
    }
}
