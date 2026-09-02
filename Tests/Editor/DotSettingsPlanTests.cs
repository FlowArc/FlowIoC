using System.Collections.Generic;
using System.Linq;
using FlowIoC.Editor.ModuleScan;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    public class DotSettingsPlanTests
    {
        private const string MODULES_ROOT = "C:/proj/Assets/Modules";

        private static ModuleTargetEVO Target(string absolutePath) => new ModuleTargetEVO
        {
            Name = "PlayerModule",
            AbsolutePath = absolutePath,
            Layout = TestModuleLayout.With(
                TestModuleLayout.Folder("Scripts", isMandatory: true, isNamespaceProvider: false, subFolders: new[]
                {
                    TestModuleLayout.Folder("Runtime", isMandatory: true, isNamespaceProvider: false),
                    TestModuleLayout.Folder("Models", isMandatory: true)
                }),
                TestModuleLayout.Folder("Prefabs", isOptional: true, isNamespaceProvider: false))
        };

        private static List<string> Skip(string modulePath) =>
            new DotSettingsPlan()
                .SkipFoldersFor(Target(modulePath), MODULES_ROOT)
                .Select(path => path.Replace('\\', '/'))
                .ToList();

        /// <summary>
        /// Scripts and Runtime are structure, not namespace: a Model has to land in
        /// Modules.PlayerModule.Models rather than Modules.PlayerModule.Scripts.Runtime.Models.
        /// </summary>
        [Test]
        public void Folders_that_do_not_provide_a_namespace_are_listed()
        {
            List<string> skip = Skip(MODULES_ROOT + "/PlayerModule");

            CollectionAssert.Contains(skip, MODULES_ROOT + "/PlayerModule/Scripts");
            CollectionAssert.Contains(skip, MODULES_ROOT + "/PlayerModule/Scripts/Runtime");
        }

        [Test]
        public void A_folder_that_does_provide_a_namespace_is_not_listed()
        {
            CollectionAssert.DoesNotContain(
                Skip(MODULES_ROOT + "/PlayerModule"),
                MODULES_ROOT + "/PlayerModule/Scripts/Models");
        }

        /// <summary>
        /// An optional folder is still part of the layout when it is taken, so its namespace
        /// setting has to be written the same as a mandatory one's.
        /// </summary>
        [Test]
        public void An_optional_folder_is_listed_too()
        {
            CollectionAssert.Contains(
                Skip(MODULES_ROOT + "/PlayerModule"),
                MODULES_ROOT + "/PlayerModule/Prefabs");
        }

        /// <summary>
        /// The container folders a nested module sits in belong to no one's namespace, so a
        /// screen module under zScreenModules is Modules.Hud.Screen rather than
        /// Modules.Gameplay.zScreenModules.Hud.
        /// </summary>
        [Test]
        public void The_container_folders_above_a_nested_module_are_listed()
        {
            CollectionAssert.Contains(
                Skip(MODULES_ROOT + "/GameplayModule/zScreenModules/HudScreenModule"),
                MODULES_ROOT + "/GameplayModule/zScreenModules");
        }

        /// <summary>
        /// The module folder itself is a namespace provider - it is what the namespace is named
        /// after - so it must never be skipped.
        /// </summary>
        [Test]
        public void The_module_folder_itself_is_not_listed()
        {
            CollectionAssert.DoesNotContain(
                Skip(MODULES_ROOT + "/GameplayModule/zScreenModules/HudScreenModule"),
                MODULES_ROOT + "/GameplayModule/zScreenModules/HudScreenModule");
        }

        [Test]
        public void Nothing_above_the_modules_root_is_listed()
        {
            List<string> skip = Skip(MODULES_ROOT + "/PlayerModule");

            CollectionAssert.DoesNotContain(skip, MODULES_ROOT);
            CollectionAssert.DoesNotContain(skip, "C:/proj/Assets");
        }

        [Test]
        public void A_target_with_no_layout_plans_nothing_but_still_walks_its_containers()
        {
            var target = new ModuleTargetEVO
            {
                Name = "HudScreenModule",
                AbsolutePath = MODULES_ROOT + "/GameplayModule/zScreenModules/HudScreenModule",
                Layout = null
            };

            List<string> skip = new DotSettingsPlan()
                .SkipFoldersFor(target, MODULES_ROOT)
                .Select(path => path.Replace('\\', '/'))
                .ToList();

            CollectionAssert.AreEqual(new[] {MODULES_ROOT + "/GameplayModule/zScreenModules"}, skip);
        }

        /// <summary>
        /// The code generator asks the same question the settings file answers - which folder
        /// names a namespace - but only about the folders inside the module, because the
        /// containers above one never appear in a namespace it computes. Sharing the walk is
        /// what stops the generator from writing a namespace the settings file contradicts.
        /// </summary>
        [Test]
        public void The_folders_inside_a_module_can_be_planned_on_their_own()
        {
            List<string> skip = new DotSettingsPlan()
                .SkipFoldersInside(
                    MODULES_ROOT + "/GameplayModule/zScreenModules/HudScreenModule",
                    Target(MODULES_ROOT + "/PlayerModule").Layout)
                .Select(path => path.Replace('\\', '/'))
                .ToList();

            CollectionAssert.AreEqual(
                new[]
                {
                    MODULES_ROOT + "/GameplayModule/zScreenModules/HudScreenModule/Scripts",
                    MODULES_ROOT + "/GameplayModule/zScreenModules/HudScreenModule/Scripts/Runtime",
                    MODULES_ROOT + "/GameplayModule/zScreenModules/HudScreenModule/Prefabs"
                },
                skip);
        }
    }
}