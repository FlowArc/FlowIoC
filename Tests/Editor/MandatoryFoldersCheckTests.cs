using System.Collections.Generic;
using FlowIoC.Editor.ModuleScan;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    public class MandatoryFoldersCheckTests
    {
        private const string MODULE = "C:/proj/Assets/Modules/PlayerModule";

        private static ModuleTargetEVO Target() => new ModuleTargetEVO
        {
            Name = "PlayerModule",
            AbsolutePath = MODULE,
            Layout = TestModuleLayout.With(
                TestModuleLayout.Folder("Scripts", isMandatory: true, subFolders: new[]
                {
                    TestModuleLayout.Folder("Runtime", isMandatory: true)
                }),
                TestModuleLayout.Folder("Prefabs", isOptional: true))
        };

        private static bool Ends(string path, string tail) => path.Replace('\\', '/').EndsWith(tail);

        [Test]
        public void A_module_with_every_mandatory_folder_is_Ok()
        {
            var check = new MandatoryFoldersCheck(path => true, path => { });

            Assert.AreEqual(ModuleCheckStatus.Ok, check.Inspect(Target()).Status);
        }

        [Test]
        public void A_missing_mandatory_folder_is_Fixable_and_named_in_the_message()
        {
            var check = new MandatoryFoldersCheck(path => !Ends(path, "Scripts/Runtime"), path => { });

            FindingEVO finding = check.Inspect(Target());

            Assert.AreEqual(ModuleCheckStatus.Fixable, finding.Status);
            StringAssert.Contains("Runtime", finding.Message);
        }

        /// <summary>
        /// An optional folder is optional. Reporting one as missing would put a permanent warning
        /// on every module that simply chose not to have Prefabs.
        /// </summary>
        [Test]
        public void A_missing_optional_folder_is_not_a_finding()
        {
            var check = new MandatoryFoldersCheck(path => !Ends(path, "Prefabs"), path => { });

            Assert.AreEqual(ModuleCheckStatus.Ok, check.Inspect(Target()).Status);
        }

        [Test]
        public void Fix_creates_every_missing_mandatory_folder()
        {
            var created = new List<string>();
            var check = new MandatoryFoldersCheck(
                path => !Ends(path, "Scripts/Runtime"),
                path => created.Add(path.Replace('\\', '/')));

            check.Fix(Target());

            CollectionAssert.AreEqual(new[] {MODULE + "/Scripts/Runtime"}, created);
        }

        /// <summary>
        /// Shared is optional, but a module that took it owes the folders that were laid down
        /// with it. An optional folder that is absent takes its children with it; one that is
        /// present does not.
        /// </summary>
        [Test]
        public void A_mandatory_folder_under_a_present_optional_folder_is_still_required()
        {
            var target = new ModuleTargetEVO
            {
                Name = "PlayerModule",
                AbsolutePath = MODULE,
                Layout = TestModuleLayout.With(
                    TestModuleLayout.Folder("Shared", isOptional: true, subFolders: new[]
                    {
                        TestModuleLayout.Folder("Signals", isMandatory: true)
                    }))
            };

            var check = new MandatoryFoldersCheck(path => !Ends(path, "Shared/Signals"), path => { });

            Assert.AreEqual(ModuleCheckStatus.Fixable, check.Inspect(target).Status);
        }

        [Test]
        public void An_absent_optional_folder_takes_its_children_with_it()
        {
            var target = new ModuleTargetEVO
            {
                Name = "PlayerModule",
                AbsolutePath = MODULE,
                Layout = TestModuleLayout.With(
                    TestModuleLayout.Folder("Shared", isOptional: true, subFolders: new[]
                    {
                        TestModuleLayout.Folder("Signals", isMandatory: true)
                    }))
            };

            var check = new MandatoryFoldersCheck(path => false, path => { });

            Assert.AreEqual(ModuleCheckStatus.Ok, check.Inspect(target).Status);
        }

        /// <summary>
        /// A module whose layout could not be resolved is not a module with no folders. Walking a
        /// null layout would report every mandatory folder as missing and Fix All would then
        /// create a tree inside whatever path the target happens to carry.
        /// </summary>
        [Test]
        public void A_target_with_no_layout_reports_nothing()
        {
            var check = new MandatoryFoldersCheck(path => false, path => { });

            var target = new ModuleTargetEVO {Name = "PlayerModule", AbsolutePath = MODULE, Layout = null};

            Assert.AreEqual(ModuleCheckStatus.Ok, check.Inspect(target).Status);
        }
    }
}