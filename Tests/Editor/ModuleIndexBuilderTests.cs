using System.Collections.Generic;
using System.Linq;
using FlowIoC.Editor.Config.ModuleConfig;
using FlowIoC.Editor.Modules;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    public class ModuleIndexBuilderTests
    {
        private ScannedModule Scanned(string name, ModuleKind kind, string path)
        {
            return new ScannedModule { Name = name, Kind = kind, AbsolutePath = path };
        }

        private List<ModuleDescriptor> Build(
            IReadOnlyList<ScannedModule> scanned,
            IReadOnlyList<ModuleDescriptor> previous = null)
        {
            return new ModuleIndexBuilder().Build(
                scanned,
                path => "guid-of:" + path,
                previous ?? new List<ModuleDescriptor>());
        }

        [Test]
        public void Every_scanned_module_becomes_a_descriptor()
        {
            List<ModuleDescriptor> built = Build(new[]
            {
                Scanned("CameraModule", ModuleKind.Main, "/p/CameraModule"),
                Scanned("HudModule", ModuleKind.Screen, "/p/MainModule/zScreenModules/HudModule")
            });

            CollectionAssert.AreEquivalent(
                new[] { "CameraModule", "HudModule" },
                built.Select(d => d.Name).ToArray());
        }

        [Test]
        public void The_folder_guid_comes_from_the_injected_lookup()
        {
            ModuleDescriptor built = Build(new[] { Scanned("CameraModule", ModuleKind.Main, "/p/CameraModule") }).Single();

            Assert.AreEqual("guid-of:/p/CameraModule", built.FolderGuid);
        }

        /// <summary>
        /// A rebuild must not throw away the folder GUIDs recorded on the previous pass. Those
        /// are the one thing in the index that cannot be recomputed from the folder tree, and
        /// losing them means a folder the user renamed by hand stops being recognised.
        /// </summary>
        [Test]
        public void Folder_guids_recorded_before_survive_a_rebuild()
        {
            var previous = new ModuleDescriptor
            {
                Name = "CameraModule",
                Kind = ModuleKind.Main,
                FolderGuid = "guid-of:/p/CameraModule"
            };
            previous.RecordFolderGuid(FolderConfig.FolderType.Controllers, "ctrl-guid");

            ModuleDescriptor built = Build(
                new[] { Scanned("CameraModule", ModuleKind.Main, "/p/CameraModule") },
                new[] { previous }).Single();

            Assert.IsTrue(built.TryGetFolderGuid(FolderConfig.FolderType.Controllers, out string guid));
            Assert.AreEqual("ctrl-guid", guid);
        }

        [Test]
        public void A_module_that_is_gone_from_the_scan_is_gone_from_the_index()
        {
            var previous = new ModuleDescriptor
            {
                Name = "DeletedModule",
                Kind = ModuleKind.Main,
                FolderGuid = "guid-of:/p/DeletedModule"
            };

            List<ModuleDescriptor> built = Build(
                new[] { Scanned("CameraModule", ModuleKind.Main, "/p/CameraModule") },
                new[] { previous });

            Assert.IsFalse(built.Any(d => d.Name == "DeletedModule"));
        }

        /// <summary>
        /// A module folder renamed in the Project window keeps its GUID, so the previous entry
        /// is matched by GUID rather than by name and its folder map is carried over.
        /// </summary>
        [Test]
        public void A_renamed_module_keeps_its_folder_map_because_the_guid_did_not_change()
        {
            var previous = new ModuleDescriptor
            {
                Name = "CameraModule",
                Kind = ModuleKind.Main,
                FolderGuid = "guid-of:/p/CameraModule"
            };
            previous.RecordFolderGuid(FolderConfig.FolderType.Models, "models-guid");

            ModuleDescriptor built = new ModuleIndexBuilder().Build(
                new[] { Scanned("CamModule", ModuleKind.Main, "/p/CamModule") },
                _ => "guid-of:/p/CameraModule",
                new[] { previous }).Single();

            Assert.AreEqual("CamModule", built.Name);
            Assert.IsTrue(built.TryGetFolderGuid(FolderConfig.FolderType.Models, out string guid));
            Assert.AreEqual("models-guid", guid);
        }

        [Test]
        public void The_kind_is_taken_from_the_scan_not_from_the_previous_index()
        {
            var previous = new ModuleDescriptor
            {
                Name = "HudModule",
                Kind = ModuleKind.Main,
                FolderGuid = "guid-of:/p/HudModule"
            };

            ModuleDescriptor built = Build(
                new[] { Scanned("HudModule", ModuleKind.Screen, "/p/HudModule") },
                new[] { previous }).Single();

            Assert.AreEqual(ModuleKind.Screen, built.Kind);
        }

        [Test]
        public void A_folder_with_no_guid_is_left_out_of_the_index()
        {
            List<ModuleDescriptor> built = new ModuleIndexBuilder().Build(
                new[] { Scanned("CameraModule", ModuleKind.Main, "/p/CameraModule") },
                _ => string.Empty,
                new List<ModuleDescriptor>());

            Assert.IsEmpty(built);
        }
    }
}
