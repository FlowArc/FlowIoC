using System.Collections.Generic;
using System.Linq;
using FlowIoC.Editor.Modules;
using NUnit.Framework;
using UnityEngine;

namespace FlowIoC.Tests
{
    public class ModuleRegistryTests
    {
        /// <summary>
        /// A path-to-GUID table standing in for the AssetDatabase, so the registry's own logic
        /// is tested without needing folders to exist in the project running the tests.
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
                    if (pair.Value == guid) return pair.Key;
                return string.Empty;
            }

            public bool IsValidFolder(string assetPath) => _guidByPath.ContainsKey(assetPath);
        }

        private FlowIoCModuleIndex _index;
        private FakeAssetPaths _paths;

        private const string CameraPath = "Assets/Modules/CameraModule";
        private const string HudPath = "Assets/Modules/CameraModule/zScreenModules/HudModule";
        private const string HudTestPath =
            "Assets/Modules/CameraModule/zScreenModules/HudModule/zTestModules/HudTestModule";

        [SetUp]
        public void SetUp()
        {
            _index = ScriptableObject.CreateInstance<FlowIoCModuleIndex>();
            _paths = new FakeAssetPaths();

            _paths.Add(CameraPath, "camera-guid");
            _paths.Add(HudPath, "hud-guid");
            _paths.Add(HudTestPath, "hud-test-guid");

            _index.Replace(new[]
            {
                new ModuleDescriptor { Name = "CameraModule", Kind = ModuleKind.Main, FolderGuid = "camera-guid" },
                new ModuleDescriptor { Name = "HudModule", Kind = ModuleKind.Screen, FolderGuid = "hud-guid" },
                new ModuleDescriptor { Name = "HudTestModule", Kind = ModuleKind.Test, FolderGuid = "hud-test-guid" }
            });
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_index);
        }

        private ModuleRegistry NewRegistry() => new ModuleRegistry(_index, _paths);

        [Test]
        public void A_module_folder_is_a_module()
        {
            Assert.IsTrue(NewRegistry().IsModule(CameraPath));
        }

        [Test]
        public void A_folder_inside_a_module_is_not_itself_a_module()
        {
            _paths.Add("Assets/Modules/CameraModule/Scripts", "scripts-guid");

            Assert.IsFalse(NewRegistry().IsModule("Assets/Modules/CameraModule/Scripts"));
        }

        [Test]
        public void The_nearest_module_of_a_folder_deep_inside_one_is_that_module()
        {
            _paths.Add("Assets/Modules/CameraModule/Scripts/Runtime/Models", "models-guid");

            Assert.IsTrue(NewRegistry().TryGetNearestModule(
                "Assets/Modules/CameraModule/Scripts/Runtime/Models", out ModuleDescriptor module));
            Assert.AreEqual("CameraModule", module.Name);
        }

        /// <summary>
        /// A screen module sits inside its parent module's folder, so walking up from inside it
        /// must stop at the screen module rather than running on to the parent.
        /// </summary>
        [Test]
        public void The_nearest_module_stops_at_the_innermost_one()
        {
            _paths.Add(HudPath + "/Scripts", "hud-scripts-guid");

            NewRegistry().TryGetNearestModule(HudPath + "/Scripts", out ModuleDescriptor module);

            Assert.AreEqual("HudModule", module.Name);
        }

        [Test]
        public void A_path_outside_any_module_has_no_nearest_module()
        {
            _paths.Add("Assets/Plugins", "plugins-guid");

            Assert.IsFalse(NewRegistry().TryGetNearestModule("Assets/Plugins", out _));
        }

        [Test]
        public void The_path_of_a_module_is_resolved_from_its_guid()
        {
            _index.TryGetByName("HudModule", out ModuleDescriptor hud);

            Assert.AreEqual(HudPath, NewRegistry().PathOf(hud));
        }

        /// <summary>
        /// HudTestModule sits two modules deep under CameraModule, so asking CameraModule for
        /// children must not reach it; only HudModule, its direct parent, reaches it.
        /// </summary>
        [Test]
        public void The_screen_modules_of_a_module_are_its_screen_kind_children()
        {
            _index.TryGetByName("CameraModule", out ModuleDescriptor camera);
            _index.TryGetByName("HudModule", out ModuleDescriptor hud);

            Assert.AreEqual("HudModule", NewRegistry().ChildrenOf(camera, ModuleKind.Screen).Single().Name);
            Assert.AreEqual("HudTestModule", NewRegistry().ChildrenOf(hud, ModuleKind.Test).Single().Name);
        }

        [Test]
        public void A_module_with_no_children_of_that_kind_yields_nothing()
        {
            _index.TryGetByName("CameraModule", out ModuleDescriptor camera);

            Assert.IsEmpty(NewRegistry().ChildrenOf(camera, ModuleKind.Test));
        }

        /// <summary>
        /// Namespaces are built from the chain of modules a module sits inside, so the walk has
        /// to come back nearest first.
        /// </summary>
        [Test]
        public void The_ancestors_of_a_nested_module_come_back_nearest_first()
        {
            _index.TryGetByName("HudTestModule", out ModuleDescriptor hudTest);

            CollectionAssert.AreEqual(
                new[] { "HudModule", "CameraModule" },
                NewRegistry().AncestorsOf(hudTest).Select(m => m.Name).ToArray());
        }

        [Test]
        public void A_top_level_module_has_no_ancestors()
        {
            _index.TryGetByName("CameraModule", out ModuleDescriptor camera);

            Assert.IsEmpty(NewRegistry().AncestorsOf(camera));
        }
    }
}
