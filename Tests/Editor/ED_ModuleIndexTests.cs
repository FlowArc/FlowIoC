using FlowIoC.Editor.Config.ModuleConfig;
using FlowIoC.Editor.Modules;
using NUnit.Framework;
using UnityEngine;

namespace FlowIoC.Tests
{
    public class ED_ModuleIndexTests
    {
        private ED_ModuleIndex _index;

        [SetUp]
        public void SetUp()
        {
            _index = ScriptableObject.CreateInstance<ED_ModuleIndex>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_index);
        }

        private ModuleDescriptorEVO Descriptor(string name, ModuleKind kind, string guid)
        {
            return new ModuleDescriptorEVO { Name = name, Kind = kind, FolderGuid = guid };
        }

        [Test]
        public void A_fresh_index_holds_no_modules()
        {
            Assert.IsFalse(_index.TryGetByFolderGuid("abc", out _));
        }

        [Test]
        public void A_module_is_found_by_its_folder_guid()
        {
            _index.Replace(new[] { Descriptor("CameraModule", ModuleKind.Main, "abc") });

            Assert.IsTrue(_index.TryGetByFolderGuid("abc", out ModuleDescriptorEVO module));
            Assert.AreEqual("CameraModule", module.Name);
            Assert.AreEqual(ModuleKind.Main, module.Kind);
        }

        [Test]
        public void A_module_is_found_by_its_name_ignoring_case()
        {
            _index.Replace(new[] { Descriptor("CameraModule", ModuleKind.Main, "abc") });

            Assert.IsTrue(_index.TryGetByName("cameramodule", out ModuleDescriptorEVO module));
            Assert.AreEqual("abc", module.FolderGuid);
        }

        [Test]
        public void Replace_drops_the_modules_that_were_there_before()
        {
            _index.Replace(new[] { Descriptor("CameraModule", ModuleKind.Main, "abc") });
            _index.Replace(new[] { Descriptor("HudModule", ModuleKind.Screen, "def") });

            Assert.IsFalse(_index.TryGetByFolderGuid("abc", out _));
            Assert.IsTrue(_index.TryGetByFolderGuid("def", out _));
        }

        [Test]
        public void Remove_takes_a_single_module_out()
        {
            _index.Replace(new[]
            {
                Descriptor("CameraModule", ModuleKind.Main, "abc"),
                Descriptor("HudModule", ModuleKind.Screen, "def")
            });

            _index.Remove("abc");

            Assert.IsFalse(_index.TryGetByFolderGuid("abc", out _));
            Assert.IsTrue(_index.TryGetByFolderGuid("def", out _));
        }

        [Test]
        public void A_folder_guid_recorded_on_a_descriptor_is_read_back()
        {
            ModuleDescriptorEVO module = Descriptor("CameraModule", ModuleKind.Main, "abc");

            module.RecordFolderGuid(FolderEVO.FolderType.Controllers, "ctrl-guid");

            Assert.IsTrue(module.TryGetFolderGuid(FolderEVO.FolderType.Controllers, out string guid));
            Assert.AreEqual("ctrl-guid", guid);
        }

        /// <summary>
        /// A folder type the index has never seen — a type added by a later FlowIoC version, or
        /// a folder the project never created — must report itself missing rather than throw.
        /// Blind dictionary indexing on exactly this kind of value is what took module creation
        /// down with a KeyNotFoundException in 1.1.1.
        /// </summary>
        [Test]
        public void An_unrecorded_folder_type_reports_itself_missing()
        {
            ModuleDescriptorEVO module = Descriptor("CameraModule", ModuleKind.Main, "abc");

            Assert.IsFalse(module.TryGetFolderGuid(FolderEVO.FolderType.Systems, out string guid));
            Assert.IsNull(guid);
        }

        [Test]
        public void Recording_a_folder_type_twice_keeps_the_newer_guid()
        {
            ModuleDescriptorEVO module = Descriptor("CameraModule", ModuleKind.Main, "abc");

            module.RecordFolderGuid(FolderEVO.FolderType.Models, "old");
            module.RecordFolderGuid(FolderEVO.FolderType.Models, "new");

            module.TryGetFolderGuid(FolderEVO.FolderType.Models, out string guid);
            Assert.AreEqual("new", guid);
        }
    }
}
