using System.Collections.Generic;
using System.IO;
using FlowIoC.Editor.Config.ModuleConfig;
using FlowIoC.Editor.Modules;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    public class MarkerFileMigrationGuardTests
    {
        private const string ProjectA = @"D:\work\ProjectA";
        private const string ProjectB = @"D:\work\ProjectB";

        private MarkerFileMigrationGuard _guard;

        [SetUp]
        public void SetUp() => _guard = new MarkerFileMigrationGuard();

        [TearDown]
        public void TearDown()
        {
            _guard.Clear(ProjectA);
            _guard.Clear(ProjectB);
        }

        [Test]
        public void Nothing_has_run_before_anything_is_recorded()
        {
            Assert.IsFalse(_guard.HasRun(ProjectA));
        }

        [Test]
        public void MarkRun_makes_HasRun_true()
        {
            _guard.MarkRun(ProjectA);

            Assert.IsTrue(_guard.HasRun(ProjectA));
        }

        /// <summary>
        /// The failure this type exists to prevent: EditorPrefs is one machine-wide store, so a
        /// key that did not carry the project root would mark the migration done everywhere the
        /// first time it ran anywhere.
        /// </summary>
        [Test]
        public void Marking_one_project_leaves_another_project_unmarked()
        {
            _guard.MarkRun(ProjectA);

            Assert.IsTrue(_guard.HasRun(ProjectA));
            Assert.IsFalse(_guard.HasRun(ProjectB));
        }

        [Test]
        public void Clear_makes_it_run_again()
        {
            _guard.MarkRun(ProjectA);

            _guard.Clear(ProjectA);

            Assert.IsFalse(_guard.HasRun(ProjectA));
        }

        [Test]
        public void Two_project_roots_produce_two_keys()
        {
            Assert.AreNotEqual(_guard.KeyFor(ProjectA), _guard.KeyFor(ProjectB));
        }

        [Test]
        public void One_project_root_produces_a_stable_key()
        {
            Assert.AreEqual(_guard.KeyFor(ProjectA), _guard.KeyFor(ProjectA));
        }

        /// <summary>
        /// Unity hands the project root back with either separator and with whatever casing the
        /// drive reports, so the same project must not end up with two different keys.
        /// </summary>
        [Test]
        public void Separators_casing_and_a_trailing_slash_do_not_change_the_key()
        {
            string expected = _guard.KeyFor(@"D:\work\ProjectA");

            Assert.AreEqual(expected, _guard.KeyFor("D:/work/ProjectA"));
            Assert.AreEqual(expected, _guard.KeyFor(@"d:\WORK\projecta"));
            Assert.AreEqual(expected, _guard.KeyFor(@"D:\work\ProjectA\"));
        }
    }

    public class ModuleFolderGuidBackfillerTests
    {
        private static ModuleDescriptorEVO NewModule(string name, ModuleKind kind = ModuleKind.Main) =>
            new ModuleDescriptorEVO { Name = name, Kind = kind, FolderGuid = name + "-folder-guid" };

        [Test]
        public void A_module_with_no_recorded_guid_gets_one_from_the_resolver()
        {
            ModuleDescriptorEVO module = NewModule("Camera");

            int recorded = new ModuleFolderGuidBackfiller().Backfill(
                new[] { module },
                new[] { FolderEVO.FolderType.Controllers },
                (m, t) => "controllers-guid");

            Assert.AreEqual(1, recorded);
            Assert.IsTrue(module.TryGetFolderGuid(FolderEVO.FolderType.Controllers, out string guid));
            Assert.AreEqual("controllers-guid", guid);
        }

        /// <summary>
        /// The rule the whole backfill exists to honour: a GUID Task 12 (or an earlier backfill
        /// pass) already recorded is more trustworthy than a name lookup made after the fact, so
        /// it is never replaced - even when the resolver would have returned something else.
        /// </summary>
        [Test]
        public void An_existing_guid_is_not_overwritten_even_if_the_resolver_disagrees()
        {
            ModuleDescriptorEVO module = NewModule("Camera");
            module.RecordFolderGuid(FolderEVO.FolderType.Controllers, "original-guid");

            int recorded = new ModuleFolderGuidBackfiller().Backfill(
                new[] { module },
                new[] { FolderEVO.FolderType.Controllers },
                (m, t) => "different-guid");

            Assert.AreEqual(0, recorded);
            Assert.IsTrue(module.TryGetFolderGuid(FolderEVO.FolderType.Controllers, out string guid));
            Assert.AreEqual("original-guid", guid);
        }

        [Test]
        public void The_resolver_is_never_consulted_for_a_type_that_already_has_a_guid()
        {
            ModuleDescriptorEVO module = NewModule("Camera");
            module.RecordFolderGuid(FolderEVO.FolderType.Controllers, "original-guid");

            bool called = false;

            new ModuleFolderGuidBackfiller().Backfill(
                new[] { module },
                new[] { FolderEVO.FolderType.Controllers },
                (m, t) =>
                {
                    called = true;
                    return "different-guid";
                });

            Assert.IsFalse(called);
        }

        [Test]
        public void A_type_the_resolver_finds_nothing_for_records_nothing()
        {
            ModuleDescriptorEVO module = NewModule("Camera");

            int recorded = new ModuleFolderGuidBackfiller().Backfill(
                new[] { module },
                new[] { FolderEVO.FolderType.Controllers },
                (m, t) => string.Empty);

            Assert.AreEqual(0, recorded);
            Assert.IsFalse(module.TryGetFolderGuid(FolderEVO.FolderType.Controllers, out _));
        }

        /// <summary>
        /// A second pass over the same modules is what stands between this and a migration that
        /// keeps doing work every time it runs; once every type is recorded, nothing is left to
        /// find.
        /// </summary>
        [Test]
        public void Backfilling_twice_records_nothing_new_the_second_time()
        {
            ModuleDescriptorEVO module = NewModule("Camera");
            var backfiller = new ModuleFolderGuidBackfiller();
            FolderEVO.FolderType[] types = { FolderEVO.FolderType.Controllers };

            int first = backfiller.Backfill(new[] { module }, types, (m, t) => "controllers-guid");
            int second = backfiller.Backfill(new[] { module }, types, (m, t) => "controllers-guid");

            Assert.AreEqual(1, first);
            Assert.AreEqual(0, second);
        }

        [Test]
        public void Every_module_and_type_combination_is_offered_to_the_resolver()
        {
            ModuleDescriptorEVO moduleA = NewModule("A");
            ModuleDescriptorEVO moduleB = NewModule("B", ModuleKind.Screen);
            var seen = new List<(string, FolderEVO.FolderType)>();

            new ModuleFolderGuidBackfiller().Backfill(
                new[] { moduleA, moduleB },
                new[] { FolderEVO.FolderType.Controllers, FolderEVO.FolderType.Models },
                (m, t) =>
                {
                    seen.Add((m.Name, t));
                    return string.Empty;
                });

            CollectionAssert.AreEquivalent(
                new[]
                {
                    ("A", FolderEVO.FolderType.Controllers),
                    ("A", FolderEVO.FolderType.Models),
                    ("B", FolderEVO.FolderType.Controllers),
                    ("B", FolderEVO.FolderType.Models),
                },
                seen);
        }

        [Test]
        public void The_returned_count_is_how_many_were_newly_recorded_across_every_module()
        {
            ModuleDescriptorEVO moduleA = NewModule("A");
            ModuleDescriptorEVO moduleB = NewModule("B");
            moduleB.RecordFolderGuid(FolderEVO.FolderType.Controllers, "already-there");

            int recorded = new ModuleFolderGuidBackfiller().Backfill(
                new[] { moduleA, moduleB },
                new[] { FolderEVO.FolderType.Controllers, FolderEVO.FolderType.Models },
                (m, t) => t == FolderEVO.FolderType.Models ? string.Empty : "found-guid");

            // moduleA.Controllers recorded, moduleA.Models not found, moduleB.Controllers already
            // present (skipped), moduleB.Models not found: exactly one new recording.
            Assert.AreEqual(1, recorded);
        }

        [Test]
        public void A_null_module_list_records_nothing_rather_than_throwing()
        {
            int recorded = new ModuleFolderGuidBackfiller().Backfill(
                null, new[] { FolderEVO.FolderType.Controllers }, (m, t) => "guid");

            Assert.AreEqual(0, recorded);
        }

        [Test]
        public void A_null_type_list_records_nothing_rather_than_throwing()
        {
            int recorded = new ModuleFolderGuidBackfiller().Backfill(
                new[] { NewModule("Camera") }, null, (m, t) => "guid");

            Assert.AreEqual(0, recorded);
        }
    }

    /// <summary>
    /// Proves the reason the migration backfills before it sweeps: a GUID recorded before the
    /// sweep runs is a property of the index, not of the marker file, so deleting the marker
    /// (and its meta) afterward does not touch it. This is the same safety property that makes
    /// the migration resumable if the Editor closes between the backfill and the sweep - the
    /// next run's rebuild carries the recorded GUID forward and the backfill finds nothing left
    /// to do for it.
    /// </summary>
    public class MarkerMigrationOrderingTests
    {
        private string _root;

        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), "FlowIoCMigrationOrder_" + Path.GetRandomFileName());
            Directory.CreateDirectory(_root);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_root)) Directory.Delete(_root, true);
        }

        [Test]
        public void A_guid_backfilled_before_a_sweep_survives_the_sweep()
        {
            string controllersFolder = Path.Combine(_root, "Controllers");
            Directory.CreateDirectory(controllersFolder);
            File.WriteAllText(Path.Combine(_root, "_module_info.txt"), "Main");
            File.WriteAllText(Path.Combine(_root, "_module_info.txt.meta"), "guid: x");

            var module = new ModuleDescriptorEVO { Name = "Probe", Kind = ModuleKind.Main, FolderGuid = "probe-guid" };

            int recorded = new ModuleFolderGuidBackfiller().Backfill(
                new[] { module },
                new[] { FolderEVO.FolderType.Controllers },
                (m, t) => "controllers-guid");
            Assert.AreEqual(1, recorded);

            List<string> deleted = new MarkerFileSweeper().Sweep(_root);
            Assert.IsTrue(deleted.Exists(d => d.EndsWith("_module_info.txt")));

            Assert.IsTrue(module.TryGetFolderGuid(FolderEVO.FolderType.Controllers, out string guid));
            Assert.AreEqual("controllers-guid", guid);
        }
    }
}
