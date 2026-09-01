using FlowIoC.BaseModule.ProjectPaths;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    public class FlowIoCProjectPathsTests
    {
        [Test]
        public void Every_project_local_path_lives_under_the_single_root()
        {
            var paths = new FlowIoCProjectPaths();

            Assert.That(paths.CodeGeneratorSettings, Does.StartWith(paths.Root));
            Assert.That(paths.FolderPainterConfig, Does.StartWith(paths.Root));
            Assert.That(paths.FlowLogType, Does.StartWith(paths.Root));
            Assert.That(paths.GeneratedAsmRef, Does.StartWith(paths.Root));
            Assert.That(paths.ConsoleSettings, Does.StartWith(paths.Root));
            Assert.That(paths.DirectoryStructureConfig("Main"), Does.StartWith(paths.Root));
        }

        /// <summary>
        /// ED_CodeGenerator, the directory structure configs and the folder painter config are
        /// ScriptableObjects whose scripts live in the editor-only FlowIoC.Editor assembly. Unity can
        /// only resolve the script behind such an asset while the asset sits inside a folder named
        /// Editor, so this is a layout constraint and not a preference.
        /// </summary>
        [Test]
        public void Editor_only_assets_sit_inside_a_folder_named_Editor()
        {
            var paths = new FlowIoCProjectPaths();

            Assert.That(paths.CodeGeneratorSettings, Does.Contain("/Editor/"));
            Assert.That(paths.FolderPainterConfig, Does.Contain("/Editor/"));
            Assert.That(paths.DirectoryStructureConfig("Screen"), Does.Contain("/Editor/"));
        }

        /// <summary>
        /// FlowLogger reads the settings with Resources.Load, which only searches folders named
        /// exactly Resources.
        /// </summary>
        [Test]
        public void The_console_settings_live_in_a_folder_named_Resources()
        {
            var paths = new FlowIoCProjectPaths();

            Assert.AreEqual(paths.Root + "/Resources", paths.ResourcesRoot);
            Assert.AreEqual(paths.ResourcesRoot + "/CD_FlowConsole.asset", paths.ConsoleSettings);
        }

        [Test]
        public void The_generated_script_and_its_assembly_reference_share_one_folder()
        {
            var paths = new FlowIoCProjectPaths();

            Assert.AreEqual(paths.GeneratedRoot + "/FlowLogType.cs", paths.FlowLogType);
            Assert.AreEqual(paths.GeneratedRoot + "/FlowIoC.Generated.asmref", paths.GeneratedAsmRef);
        }

        [Test]
        public void Directory_structure_configs_are_named_after_their_config_key()
        {
            var paths = new FlowIoCProjectPaths();

            Assert.AreEqual(
                paths.CodeGeneratorRoot + "/ED_MainModuleDirectoryStructure.asset",
                paths.DirectoryStructureConfig("Main"));
            Assert.AreEqual(
                paths.CodeGeneratorRoot + "/ED_TestModuleDirectoryStructure.asset",
                paths.DirectoryStructureConfig("Test"));
        }

        [Test]
        public void The_root_is_the_agreed_plugins_folder()
        {
            Assert.AreEqual("Assets/Plugins/FlowIoC", new FlowIoCProjectPaths().Root);
        }

        [Test]
        public void The_module_index_sits_beside_the_code_generator_settings()
        {
            Assert.AreEqual(
                "Assets/Plugins/FlowIoC/Editor/CodeGenerator/ED_ModuleIndex.asset",
                new FlowIoCProjectPaths().ModuleIndex);
        }
    }
}