using System.IO;
using FlowIoC.Editor.ModuleInstall;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    public class ModulesSourceTests
    {
        private string _packageRoot;

        [SetUp]
        public void SetUp()
        {
            _packageRoot = Path.Combine(Path.GetTempPath(), "FlowIoCModulesSource_" + Path.GetRandomFileName());
            Directory.CreateDirectory(_packageRoot);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_packageRoot))
                Directory.Delete(_packageRoot, true);
        }

        private void WriteModule(string payloadFolder, string moduleName)
        {
            string module = Path.Combine(_packageRoot, payloadFolder, moduleName);
            Directory.CreateDirectory(module);
            File.WriteAllText(Path.Combine(module, moduleName + ".asmdef"), "{\"name\":\"" + moduleName + "\"}");
        }

        [Test]
        public void A_source_defaults_to_the_button_installed_modules()
        {
            WriteModule(ModulesSource.ModulesFolder, "CountdownServiceModule");
            WriteModule(ModulesSource.SetupModulesFolder, "MainModule");

            Assert.IsTrue(new ModulesSource(_packageRoot).TryList(out string[] found, out _));

            Assert.AreEqual(1, found.Length);
            StringAssert.EndsWith("CountdownServiceModule", found[0]);
        }

        [Test]
        public void A_source_told_to_read_the_setup_folder_lists_only_the_set()
        {
            WriteModule(ModulesSource.ModulesFolder, "CountdownServiceModule");
            WriteModule(ModulesSource.SetupModulesFolder, "MainModule");

            var source = new ModulesSource(_packageRoot, ModulesSource.SetupModulesFolder);

            Assert.IsTrue(source.TryList(out string[] found, out _));

            Assert.AreEqual(1, found.Length);
            StringAssert.EndsWith("MainModule", found[0]);
        }
    }
}
