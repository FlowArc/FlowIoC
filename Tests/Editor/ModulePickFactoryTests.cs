using System.Collections.Generic;
using System.Linq;
using FlowIoC.Editor.CodeGenerator.Menus.Module;
using FlowIoC.Editor.Modules;
using NUnit.Framework;
using UnityEngine;

namespace FlowIoC.Tests
{
    /// <summary>
    /// The picks a generator offers as a parent come off the module index, and what the tree
    /// needs of each - above all which module it lives in - is read off the index's paths here
    /// rather than in the window.
    /// </summary>
    public class ModulePickFactoryTests
    {
        private class FakeAssetPaths : IAssetPaths
        {
            private readonly Dictionary<string, string> _guidByPath = new Dictionary<string, string>();

            public void Add(string path, string guid) => _guidByPath[path] = guid;

            public string GuidOf(string assetPath) =>
                assetPath != null && _guidByPath.TryGetValue(assetPath, out string guid) ? guid : string.Empty;

            public string PathOf(string guid) =>
                _guidByPath.FirstOrDefault(pair => pair.Value == guid).Key ?? string.Empty;
        }

        private const string CameraPath = "Assets/Modules/CameraModule";
        private const string HudPath = "Assets/Modules/CameraModule/zScreenModules/HudModule";
        private const string HudTestPath = "Assets/Modules/CameraModule/zScreenModules/HudModule/zTestModules/HudTestModule";

        private ED_ModuleIndex _index;
        private FakeAssetPaths _paths;

        [SetUp]
        public void SetUp()
        {
            _index = ScriptableObject.CreateInstance<ED_ModuleIndex>();
            _paths = new FakeAssetPaths();

            _paths.Add(CameraPath, "camera-guid");
            _paths.Add(HudPath, "hud-guid");
            _paths.Add(HudTestPath, "hud-test-guid");

            _index.Replace(new[]
            {
                new ModuleDescriptorEVO {Name = "HudTestModule", Kind = ModuleKind.Test, FolderGuid = "hud-test-guid"},
                new ModuleDescriptorEVO {Name = "CameraModule", Kind = ModuleKind.Main, FolderGuid = "camera-guid"},
                new ModuleDescriptorEVO {Name = "HudModule", Kind = ModuleKind.Screen, FolderGuid = "hud-guid"}
            });
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_index);
        }

        private List<ModulePickEVO> Picks() => new ModulePickFactory().From(new ModuleRegistry(_index, _paths));

        [Test]
        public void Every_module_in_the_index_is_a_pick()
        {
            CollectionAssert.AreEquivalent(
                new[] {"CameraModule", "HudModule", "HudTestModule"},
                Picks().Select(pick => pick.Name));
        }

        /// <summary>
        /// The nearest module above, not the top of the chain: a test module inside a screen
        /// module hangs from the screen, and the screen from the module it belongs to.
        /// </summary>
        [Test]
        public void A_pick_names_the_nearest_module_it_lives_in()
        {
            Dictionary<string, ModulePickEVO> picks = Picks().ToDictionary(pick => pick.Name);

            Assert.IsNull(picks["CameraModule"].ParentName);
            Assert.AreEqual("CameraModule", picks["HudModule"].ParentName);
            Assert.AreEqual("HudModule", picks["HudTestModule"].ParentName);
        }

        [Test]
        public void A_pick_carries_the_kind_the_index_holds()
        {
            Dictionary<string, ModulePickEVO> picks = Picks().ToDictionary(pick => pick.Name);

            Assert.AreEqual(ModuleKind.Screen, picks["HudModule"].Kind);
            Assert.AreEqual(ModuleKind.Test, picks["HudTestModule"].Kind);
        }

        /// <summary>
        /// The generators compare the pick against paths they build with Path.Combine off
        /// Application.dataPath, so the pick's path is absolute and in that same shape rather
        /// than the index's Assets-relative one.
        /// </summary>
        [Test]
        public void A_picks_path_is_absolute_under_the_project_data_folder()
        {
            ModulePickEVO camera = Picks().First(pick => pick.Name == "CameraModule");

            StringAssert.StartsWith(Application.dataPath, camera.Path);
            StringAssert.EndsWith("CameraModule", camera.Path);
            StringAssert.Contains("Modules", camera.Path);
        }
    }
}
