using System.Collections.Generic;
using FlowIoC.Editor.ModuleScan;
using FlowIoC.Editor.Modules;
using NUnit.Framework;
using UnityEngine;

namespace FlowIoC.Tests
{
    public class ModuleIndexCheckTests
    {
        private readonly List<ED_ModuleIndex> _created = new List<ED_ModuleIndex>();

        [TearDown]
        public void TearDown()
        {
            foreach (ED_ModuleIndex index in _created)
                Object.DestroyImmediate(index);

            _created.Clear();
        }

        private ED_ModuleIndex IndexOf(params (string Name, ModuleKind Kind)[] modules)
        {
            var index = ScriptableObject.CreateInstance<ED_ModuleIndex>();
            _created.Add(index);

            var descriptors = new List<ModuleDescriptorEVO>();

            foreach ((string name, ModuleKind kind) in modules)
                descriptors.Add(new ModuleDescriptorEVO {Name = name, Kind = kind, FolderGuid = name});

            index.Replace(descriptors);

            return index;
        }

        private static ProjectTargetEVO Project(ED_ModuleIndex index, params (string Name, ModuleKind Kind)[] scanned)
        {
            var found = new List<ScannedModule>();

            foreach ((string name, ModuleKind kind) in scanned)
                found.Add(new ScannedModule {Name = name, Kind = kind, AbsolutePath = name});

            return new ProjectTargetEVO {Index = index, ScannedModules = found};
        }

        [Test]
        public void An_index_matching_the_disk_is_Ok()
        {
            var check = new ModuleIndexCheck(() => { });

            ProjectTargetEVO project = Project(
                IndexOf(("PlayerModule", ModuleKind.Main), ("InputModule", ModuleKind.Main)),
                ("PlayerModule", ModuleKind.Main), ("InputModule", ModuleKind.Main));

            Assert.AreEqual(ModuleCheckStatus.Ok, check.Inspect(project).Status);
        }

        [Test]
        public void A_module_on_disk_that_the_index_has_never_seen_is_Fixable()
        {
            var check = new ModuleIndexCheck(() => { });

            ProjectTargetEVO project = Project(
                IndexOf(("PlayerModule", ModuleKind.Main)),
                ("PlayerModule", ModuleKind.Main), ("InputModule", ModuleKind.Main));

            FindingEVO finding = check.Inspect(project);

            Assert.AreEqual(ModuleCheckStatus.Fixable, finding.Status);
            StringAssert.Contains("InputModule", finding.Message);
        }

        [Test]
        public void A_module_in_the_index_that_is_gone_from_disk_is_Fixable()
        {
            var check = new ModuleIndexCheck(() => { });

            ProjectTargetEVO project = Project(
                IndexOf(("PlayerModule", ModuleKind.Main), ("OldModule", ModuleKind.Main)),
                ("PlayerModule", ModuleKind.Main));

            FindingEVO finding = check.Inspect(project);

            Assert.AreEqual(ModuleCheckStatus.Fixable, finding.Status);
            StringAssert.Contains("OldModule", finding.Message);
        }

        /// <summary>
        /// A module's kind is the folder it sits in, so moving one from zSubModules to
        /// zScreenModules changes its kind without changing its name. The index would go on
        /// handing out the old layout, which is what decides its folders and its namespaces.
        /// </summary>
        [Test]
        public void A_module_whose_kind_changed_is_Fixable()
        {
            var check = new ModuleIndexCheck(() => { });

            ProjectTargetEVO project = Project(
                IndexOf(("HudScreenModule", ModuleKind.Sub)),
                ("HudScreenModule", ModuleKind.Screen));

            FindingEVO finding = check.Inspect(project);

            Assert.AreEqual(ModuleCheckStatus.Fixable, finding.Status);
            StringAssert.Contains("HudScreenModule", finding.Message);
        }

        /// <summary>
        /// A scan that found nothing is far more likely to be a failed scan than a project with
        /// no modules, and rebuilding on the strength of it would empty the index. The same
        /// caution ModuleLogTypePlan already takes about removals.
        /// </summary>
        [Test]
        public void An_empty_scan_is_not_treated_as_an_empty_project()
        {
            var check = new ModuleIndexCheck(() => { });

            ProjectTargetEVO project = Project(IndexOf(("PlayerModule", ModuleKind.Main)));

            Assert.AreEqual(ModuleCheckStatus.Ok, check.Inspect(project).Status);
        }

        [Test]
        public void Fix_rebuilds_the_index()
        {
            bool rebuilt = false;

            new ModuleIndexCheck(() => rebuilt = true).Fix(new ProjectTargetEVO());

            Assert.IsTrue(rebuilt);
        }
    }
}
