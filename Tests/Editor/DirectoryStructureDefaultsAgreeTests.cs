using System.Collections.Generic;
using FlowIoC.Editor.Config.ModuleConfig;
using NUnit.Framework;
using UnityEngine;

namespace FlowIoC.Tests
{
    /// <summary>
    /// Each layout declares its folders twice: once in the field initializer, and once in
    /// InitializeDefaultFolderStructure, which is what GetOrCreateConfig runs and therefore what
    /// ends up serialized into the project's asset. Nothing kept the two in step.
    ///
    /// The screen layout had `Scriptables` optional in one and mandatory in the other. It went
    /// unnoticed for as long as nothing acted on the flag - Create Module only reads the optional
    /// ones, to decide which checkboxes to offer. Module Scan is the first tool that reads
    /// IsMandatory and creates what is missing, so it duly created a `Scriptables` folder in every
    /// screen module that did not have one.
    /// </summary>
    public class DirectoryStructureDefaultsAgreeTests
    {
        private class ScreenLayout : ED_ScreenModuleDirectoryStructure
        {
            internal void Initialize() => InitializeDefaultFolderStructure();
        }

        private class MainLayout : ED_MainModuleDirectoryStructure
        {
            internal void Initialize() => InitializeDefaultFolderStructure();
        }

        private class TestLayout : ED_TestModuleDirectoryStructure
        {
            internal void Initialize() => InitializeDefaultFolderStructure();
        }

        private readonly List<Object> _created = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (Object asset in _created)
                Object.DestroyImmediate(asset);

            _created.Clear();
        }

        private T Layout<T>() where T : DirectoryStructureConfig
        {
            var layout = ScriptableObject.CreateInstance<T>();
            _created.Add(layout);

            return layout;
        }

        private static Dictionary<string, string> FlagsByName(List<FolderEVO> folders)
        {
            var flags = new Dictionary<string, string>();

            Walk(folders, flags);

            return flags;
        }

        private static void Walk(List<FolderEVO> folders, Dictionary<string, string> flags)
        {
            if (folders == null) return;

            foreach (FolderEVO folder in folders)
            {
                flags[folder.FolderName] = $"mandatory={folder.IsMandatory} optional={folder.IsOptional}";

                Walk(folder.SubFolders, flags);
            }
        }

        /// <summary>
        /// Only names the two declarations share are compared: the default structure reads some
        /// folder names out of the code generator settings, so a renamed folder is a difference
        /// about naming rather than about what the folder is.
        /// </summary>
        private static void AssertDeclarationsAgree(DirectoryStructureConfig layout, System.Action initialize)
        {
            Dictionary<string, string> declared = FlagsByName(layout.RootFolders);

            initialize();

            Dictionary<string, string> defaults = FlagsByName(layout.RootFolders);

            foreach (KeyValuePair<string, string> folder in defaults)
            {
                if (!declared.TryGetValue(folder.Key, out string declaredFlags)) continue;

                Assert.AreEqual(
                    declaredFlags,
                    folder.Value,
                    $"'{folder.Key}' is declared one way in the field initializer and another in "
                    + "InitializeDefaultFolderStructure. The second is what gets serialized, and "
                    + "Module Scan creates whatever it calls mandatory.");
            }
        }

        [Test]
        public void The_main_layout_says_the_same_thing_twice()
        {
            MainLayout layout = Layout<MainLayout>();

            AssertDeclarationsAgree(layout, layout.Initialize);
        }

        [Test]
        public void The_screen_layout_says_the_same_thing_twice()
        {
            ScreenLayout layout = Layout<ScreenLayout>();

            AssertDeclarationsAgree(layout, layout.Initialize);
        }

        [Test]
        public void The_test_layout_says_the_same_thing_twice()
        {
            TestLayout layout = Layout<TestLayout>();

            AssertDeclarationsAgree(layout, layout.Initialize);
        }

        /// <summary>
        /// Scriptables is where a module keeps ScriptableObject assets it authored. A module that
        /// has none should not be told it is missing a folder, in any layout.
        /// </summary>
        [Test]
        public void Scriptables_is_optional_in_every_layout()
        {
            MainLayout main = Layout<MainLayout>();
            ScreenLayout screen = Layout<ScreenLayout>();
            TestLayout test = Layout<TestLayout>();

            main.Initialize();
            screen.Initialize();
            test.Initialize();

            Assert.AreEqual("mandatory=False optional=True", FlagsByName(main.RootFolders)["Scriptables"]);
            Assert.AreEqual("mandatory=False optional=True", FlagsByName(screen.RootFolders)["Scriptables"]);
            Assert.AreEqual("mandatory=False optional=True", FlagsByName(test.RootFolders)["Scriptables"]);
        }

        /// <summary>
        /// Correcting the code does not reach a config asset already written into a project, so
        /// GetOrCreateConfig heals one - the same way it already removes the retired ScreenConfigs
        /// folder. Without this, every project that installed the package before the fix would go
        /// on having Scriptables folders created for it.
        /// </summary>
        [Test]
        public void A_config_that_still_has_Scriptables_mandatory_is_healed()
        {
            ScreenLayout layout = Layout<ScreenLayout>();
            layout.Initialize();

            FolderEVO scriptables = layout.RootFolders.Find(folder => folder.FolderName == "Scriptables");
            scriptables.IsMandatory = true;
            scriptables.IsOptional = false;

            Assert.IsTrue(layout.MakeFolderOptional("Scriptables"), "the heal reported no change");
            Assert.IsFalse(scriptables.IsMandatory);
            Assert.IsTrue(scriptables.IsOptional);
        }

        [Test]
        public void Healing_a_config_that_is_already_right_changes_nothing()
        {
            ScreenLayout layout = Layout<ScreenLayout>();
            layout.Initialize();

            Assert.IsFalse(layout.MakeFolderOptional("Scriptables"));
        }
    }
}