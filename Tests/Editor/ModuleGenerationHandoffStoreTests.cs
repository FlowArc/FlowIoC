using FlowIoC.Editor.CodeGenerator.Menus.Module;
using FlowIoC.Editor.CodeGenerator.Menus.Module.ModuleGeneration;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    /// <summary>
    /// The two halves of Create Module talk through one record written whole at the end of the
    /// first half and read back after the domain reload. What these pin is that the record comes
    /// back as it was written - above all that a run which asked for no scene reads back as one
    /// that asked for no scene, which is the answer twelve loose EditorPrefs keys could not give.
    /// </summary>
    public class ModuleGenerationHandoffStoreTests
    {
        private readonly ModuleGenerationHandoffStore _store = new ModuleGenerationHandoffStore();

        [Test]
        public void A_handoff_survives_the_round_trip()
        {
            var written = new ModuleGenerationHandoffEVO
            {
                ModuleType = ModuleType.Screen,
                ModuleName = "Settings",
                RootName = "SettingsTestRoot",
                ContextNamespace = "Modules.Settings.zTestModules.SettingsTest.RootsContexts",
                ViewNamespace = "Modules.Settings.ViewsMediators",
                ScreenContextFullName = "Modules.Settings.RootsContexts.SettingsContext",
                ScreenPrefabPath = "D:/Project/Assets/Modules/SettingsModule/Prefabs",
                ScenePath = "Assets/Modules/SettingsModule/zTestModules/SettingsTestModule/Scenes/SettingsTestScene.unity"
            };

            ModuleGenerationHandoffEVO read = _store.Deserialize(_store.Serialize(written));

            Assert.AreEqual(ModuleType.Screen, read.ModuleType);
            Assert.AreEqual("Settings", read.ModuleName);
            Assert.AreEqual("SettingsTestRoot", read.RootName);
            Assert.AreEqual(written.ContextNamespace, read.ContextNamespace);
            Assert.AreEqual(written.ViewNamespace, read.ViewNamespace);
            Assert.AreEqual(written.ScreenContextFullName, read.ScreenContextFullName);
            Assert.AreEqual(written.ScreenPrefabPath, read.ScreenPrefabPath);
            Assert.AreEqual(written.ScenePath, read.ScenePath);
        }

        /// <summary>
        /// The run that wrote the owner's open scene to disk was a main module created with
        /// Create Scene unticked, read after the reload as one that wanted a scene. A record
        /// written whole cannot carry an earlier run's answer, and the reader relies on the
        /// scene path being empty rather than on a flag beside it.
        /// </summary>
        [Test]
        public void A_run_that_asked_for_no_scene_reads_back_with_none()
        {
            var written = new ModuleGenerationHandoffEVO
            {
                ModuleType = ModuleType.Main,
                ModuleName = "MobileNotification",
                RootName = "MobileNotificationServiceRoot",
                ContextNamespace = "Modules.MobileNotification.RootsContexts",
                ScenePath = null
            };

            ModuleGenerationHandoffEVO read = _store.Deserialize(_store.Serialize(written));

            Assert.IsTrue(string.IsNullOrEmpty(read.ScenePath));
            Assert.AreEqual("MobileNotificationServiceRoot", read.RootName);
        }

        [Test]
        public void Nothing_stored_reads_back_as_nothing()
        {
            Assert.IsNull(_store.Deserialize(null));
            Assert.IsNull(_store.Deserialize(""));
        }

        /// <summary>
        /// SessionState is shared with everything else in the editor and outlives the code that
        /// wrote it. A value this store did not write is no pending run, never an exception in
        /// the middle of a domain reload.
        /// </summary>
        [Test]
        public void An_unreadable_value_reads_back_as_nothing()
        {
            Assert.IsNull(_store.Deserialize("this is not json"));
        }
    }
}
