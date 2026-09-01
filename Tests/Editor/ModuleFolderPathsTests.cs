using System.Collections.Generic;
using System.Linq;
using FlowIoC.Editor.CodeGenerator.Provider;
using FlowIoC.Editor.Modules;
using NUnit.Framework;
using UnityEngine;

namespace FlowIoC.Tests
{
    public class ModuleFolderPathsTests
    {
        /// <summary>
        /// A path-to-GUID table standing in for the AssetDatabase, so the resolver is tested
        /// without folders having to exist in the project running the tests.
        /// </summary>
        private class FakeAssetPaths : IAssetPaths
        {
            private readonly Dictionary<string, string> _guidByPath = new Dictionary<string, string>();

            public void Add(string path, string guid) => _guidByPath[path] = guid;

            public string GuidOf(string assetPath)
            {
                return assetPath != null && _guidByPath.TryGetValue(assetPath, out string guid)
                    ? guid
                    : string.Empty;
            }

            public string PathOf(string guid)
            {
                foreach (var pair in _guidByPath)
                    if (pair.Value == guid)
                        return pair.Key;
                return string.Empty;
            }
        }

        private const string CameraPath = "Assets/Modules/CameraModule";
        private const string HudPath = "Assets/Modules/CameraModule/zScreenModules/HudModule";

        private ED_ModuleIndex _index;
        private FakeAssetPaths _paths;
        private HashSet<string> _onDisk;

        [SetUp]
        public void SetUp()
        {
            _index = ScriptableObject.CreateInstance<ED_ModuleIndex>();
            _paths = new FakeAssetPaths();
            _onDisk = new HashSet<string> {CameraPath, HudPath};

            _paths.Add(CameraPath, "camera-guid");
            _paths.Add(HudPath, "hud-guid");

            _index.Replace(new[]
            {
                new ModuleDescriptorEVO {Name = "CameraModule", Kind = ModuleKind.Main, FolderGuid = "camera-guid"},
                new ModuleDescriptorEVO {Name = "HudModule", Kind = ModuleKind.Screen, FolderGuid = "hud-guid"}
            });
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_index);

        [Test]
        public void Every_module_whose_folder_is_on_disk_is_returned()
        {
            ModuleFolders folders = Resolve();

            CollectionAssert.AreEqual(new[] {CameraPath, HudPath}, folders.Paths.ToList());
            CollectionAssert.IsEmpty(folders.Skipped.ToList());
        }

        /// <summary>
        /// The failure this class exists for. GUIDToAssetPath answers for a deleted folder with
        /// the last path it knew, so the path looks usable and the first Directory call on it
        /// throws DirectoryNotFoundException.
        /// </summary>
        [Test]
        public void A_module_whose_folder_is_no_longer_on_disk_is_skipped()
        {
            _onDisk.Remove(HudPath);

            ModuleFolders folders = Resolve();

            CollectionAssert.AreEqual(new[] {CameraPath}, folders.Paths.ToList());
            Assert.AreEqual(1, folders.Skipped.Count);
            Assert.AreEqual("HudModule", folders.Skipped[0].Name);
            StringAssert.Contains(HudPath, folders.Skipped[0].Reason);
        }

        [Test]
        public void A_module_whose_folder_guid_no_longer_resolves_is_skipped()
        {
            _index.Replace(new[]
            {
                new ModuleDescriptorEVO {Name = "CameraModule", Kind = ModuleKind.Main, FolderGuid = "camera-guid"},
                new ModuleDescriptorEVO {Name = "GhostModule", Kind = ModuleKind.Main, FolderGuid = "no-such-guid"}
            });

            ModuleFolders folders = Resolve();

            CollectionAssert.AreEqual(new[] {CameraPath}, folders.Paths.ToList());
            Assert.AreEqual(1, folders.Skipped.Count);
            Assert.AreEqual("GhostModule", folders.Skipped[0].Name);
            StringAssert.Contains("GUID", folders.Skipped[0].Reason);
        }

        /// <summary>
        /// One stale entry used to take the whole run with it, including the parts that have
        /// nothing to do with modules - the orphan cleanup and the solution code style.
        /// </summary>
        [Test]
        public void One_stale_entry_does_not_cost_the_healthy_modules()
        {
            _paths.Add("Assets/Modules/AaaModule", "aaa-guid");
            _index.Replace(new[]
            {
                new ModuleDescriptorEVO {Name = "AaaModule", Kind = ModuleKind.Main, FolderGuid = "aaa-guid"},
                new ModuleDescriptorEVO {Name = "CameraModule", Kind = ModuleKind.Main, FolderGuid = "camera-guid"},
                new ModuleDescriptorEVO {Name = "HudModule", Kind = ModuleKind.Screen, FolderGuid = "hud-guid"}
            });

            ModuleFolders folders = Resolve();

            CollectionAssert.AreEqual(new[] {CameraPath, HudPath}, folders.Paths.ToList());
            Assert.AreEqual("AaaModule", folders.Skipped.Single().Name);
        }

        [Test]
        public void An_empty_index_resolves_to_nothing_rather_than_failing()
        {
            _index.Replace(new ModuleDescriptorEVO[0]);

            ModuleFolders folders = Resolve();

            CollectionAssert.IsEmpty(folders.Paths.ToList());
            CollectionAssert.IsEmpty(folders.Skipped.ToList());
        }

        /// <summary>
        /// The fake stands in for both collaborators the real one has: the asset path to absolute
        /// path conversion, which needs Application.dataPath, and the existence check.
        /// </summary>
        private ModuleFolders Resolve()
        {
            var resolver = new ModuleFolderPaths(assetPath => assetPath, path => _onDisk.Contains(path));

            return resolver.Resolve(new ModuleRegistry(_index, _paths));
        }
    }
}
