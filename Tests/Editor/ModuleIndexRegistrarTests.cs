using System.Collections.Generic;
using FlowIoC.Editor.CodeGenerator.Menus.Module.ModuleGeneration;
using FlowIoC.Editor.Config.ModuleConfig;
using FlowIoC.Editor.Modules;
using NUnit.Framework;
using UnityEngine;

namespace FlowIoC.Tests
{
    /// <summary>
    /// The registrar writes the folder-type GUID map, which is the one part of the index a
    /// rebuild cannot regenerate, so the rule that matters here is what it does when the module
    /// is not in the index at all: nothing, and it says so, rather than recording folder GUIDs
    /// onto a descriptor that does not exist.
    /// </summary>
    public class ModuleIndexRegistrarTests
    {
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
                {
                    if (pair.Value == guid) return pair.Key;
                }

                return string.Empty;
            }
        }

        private const string ModulePath = "Assets/Modules/HeroModule";
        private const string ModuleGuid = "hero-guid";
        private const string ControllersPath = "Assets/Modules/HeroModule/Scripts/Runtime/Controllers";
        private const string ModelsPath = "Assets/Modules/HeroModule/Scripts/Runtime/Models";

        private ED_ModuleIndex _index;
        private FakeAssetPaths _paths;
        private ModuleDescriptorEVO _hero;

        [SetUp]
        public void SetUp()
        {
            _index = ScriptableObject.CreateInstance<ED_ModuleIndex>();
            _hero = new ModuleDescriptorEVO {Name = "HeroModule", Kind = ModuleKind.Main, FolderGuid = ModuleGuid};
            _index.Replace(new[] {_hero});

            _paths = new FakeAssetPaths();
            _paths.Add(ModulePath, ModuleGuid);
            _paths.Add(ControllersPath, "controllers-guid");
            _paths.Add(ModelsPath, "models-guid");
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_index);
        }

        private static Dictionary<FolderEVO.FolderType, string> FolderPaths()
        {
            return new Dictionary<FolderEVO.FolderType, string>
            {
                {FolderEVO.FolderType.Controllers, ControllersPath},
                {FolderEVO.FolderType.Models, ModelsPath}
            };
        }

        private bool Record(Dictionary<FolderEVO.FolderType, string> folderPaths, string moduleAssetPath = ModulePath)
        {
            return new ModuleIndexRegistrar(_paths).RecordFolderGuids(
                _index,
                moduleAssetPath,
                new List<FolderEVO.FolderType> {FolderEVO.FolderType.Controllers, FolderEVO.FolderType.Models},
                type => folderPaths.TryGetValue(type, out string path) ? path : string.Empty);
        }

        [Test]
        public void Every_resolved_folder_is_recorded_against_the_module()
        {
            Assert.IsTrue(Record(FolderPaths()));

            Assert.IsTrue(_hero.TryGetFolderGuid(FolderEVO.FolderType.Controllers, out string controllers));
            Assert.AreEqual("controllers-guid", controllers);

            Assert.IsTrue(_hero.TryGetFolderGuid(FolderEVO.FolderType.Models, out string models));
            Assert.AreEqual("models-guid", models);
        }

        [Test]
        public void A_folder_that_does_not_resolve_is_skipped_and_the_rest_still_record()
        {
            Dictionary<FolderEVO.FolderType, string> folderPaths = FolderPaths();
            folderPaths.Remove(FolderEVO.FolderType.Models);

            Assert.IsTrue(Record(folderPaths));

            Assert.IsTrue(_hero.TryGetFolderGuid(FolderEVO.FolderType.Controllers, out _));
            Assert.IsFalse(_hero.TryGetFolderGuid(FolderEVO.FolderType.Models, out _));
        }

        [Test]
        public void A_module_missing_from_the_index_records_nothing_and_reports_it()
        {
            Assert.IsFalse(Record(FolderPaths(), "Assets/Modules/GhostModule"));

            Assert.IsFalse(_hero.TryGetFolderGuid(FolderEVO.FolderType.Controllers, out _));
            Assert.IsFalse(_hero.TryGetFolderGuid(FolderEVO.FolderType.Models, out _));
        }

        [Test]
        public void A_folder_whose_path_has_no_GUID_yet_is_skipped()
        {
            Dictionary<FolderEVO.FolderType, string> folderPaths = FolderPaths();
            folderPaths[FolderEVO.FolderType.Models] = "Assets/Modules/HeroModule/Scripts/Runtime/NotImportedYet";

            Assert.IsTrue(Record(folderPaths));

            Assert.IsFalse(_hero.TryGetFolderGuid(FolderEVO.FolderType.Models, out _));
        }
    }
}
