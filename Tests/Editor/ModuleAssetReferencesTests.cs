using System.Collections.Generic;
using FlowIoC.Editor.CodeGenerator.Menus.Module.DeleteModule;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    /// <summary>
    /// What Delete Module says before it deletes: which scenes and prefabs outside the module point
    /// into it.
    ///
    /// A Root lists its sub-contexts by script reference now, so a scene that lists a context of
    /// this module depends on that script and this finds it. The report is the whole of the answer -
    /// nothing is opened and nothing is saved, because editing somebody's scene is not a tool's
    /// decision. What the reader gets is the list of files to look at afterwards.
    /// </summary>
    public class ModuleAssetReferencesTests
    {
        private const string MODULE = "Assets/Modules/PlayerModule";
        private const string CONTEXT = "Assets/Modules/PlayerModule/Scripts/Runtime/RootsContexts/PlayerContext.cs";

        private static ModuleAssetReferences References(
            IReadOnlyList<string> candidates, Dictionary<string, IReadOnlyList<string>> dependencies)
        {
            return new ModuleAssetReferences(
                () => candidates,
                path => dependencies.TryGetValue(path, out IReadOnlyList<string> found)
                    ? found
                    : new List<string>());
        }

        [Test]
        public void A_scene_depending_on_a_script_in_the_module_is_reported()
        {
            ModuleAssetReferences references = References(
                new List<string> {"Assets/Scenes/MainScene.unity"},
                new Dictionary<string, IReadOnlyList<string>>
                {
                    {"Assets/Scenes/MainScene.unity", new List<string> {CONTEXT}}
                });

            IReadOnlyList<string> found = references.Find(MODULE);

            Assert.AreEqual(1, found.Count);
            StringAssert.Contains("MainScene.unity", found[0]);
            StringAssert.Contains("PlayerContext", found[0]);
        }

        [Test]
        public void A_scene_depending_on_nothing_in_the_module_is_not_reported()
        {
            ModuleAssetReferences references = References(
                new List<string> {"Assets/Scenes/MainScene.unity"},
                new Dictionary<string, IReadOnlyList<string>>
                {
                    {"Assets/Scenes/MainScene.unity", new List<string> {"Assets/Modules/HudModule/X.cs"}}
                });

            Assert.IsEmpty(references.Find(MODULE));
        }

        /// <summary>
        /// The module's own scenes and prefabs are going with it, so naming them would be a list of
        /// things the reader does not have to do anything about.
        /// </summary>
        [Test]
        public void An_asset_inside_the_module_is_not_reported()
        {
            string ownScene = MODULE + "/zTestModules/PlayerTestModule/Scenes/PlayerTestScene.unity";

            ModuleAssetReferences references = References(
                new List<string> {ownScene},
                new Dictionary<string, IReadOnlyList<string>> {{ownScene, new List<string> {CONTEXT}}});

            Assert.IsEmpty(references.Find(MODULE));
        }

        /// <summary>
        /// A module whose name is a prefix of another's must not swallow it: PlayerModule is being
        /// deleted, PlayerHudModule is not.
        /// </summary>
        [Test]
        public void A_module_whose_path_merely_starts_the_same_is_a_different_module()
        {
            string neighbour = "Assets/Modules/PlayerHudModule/Scripts/Runtime/RootsContexts/HudContext.cs";

            ModuleAssetReferences references = References(
                new List<string> {"Assets/Scenes/MainScene.unity"},
                new Dictionary<string, IReadOnlyList<string>>
                {
                    {"Assets/Scenes/MainScene.unity", new List<string> {neighbour}}
                });

            Assert.IsEmpty(references.Find(MODULE));
        }

        [Test]
        public void One_asset_naming_two_of_the_modules_files_is_reported_once_per_file()
        {
            string root = MODULE + "/Prefabs/PlayerRoot.prefab";

            ModuleAssetReferences references = References(
                new List<string> {"Assets/Scenes/MainScene.unity"},
                new Dictionary<string, IReadOnlyList<string>>
                {
                    {"Assets/Scenes/MainScene.unity", new List<string> {CONTEXT, root}}
                });

            Assert.AreEqual(2, references.Find(MODULE).Count);
        }

        [Test]
        public void Every_candidate_that_points_into_the_module_is_reported()
        {
            ModuleAssetReferences references = References(
                new List<string> {"Assets/Scenes/A.unity", "Assets/Prefabs/B.prefab", "Assets/Scenes/C.unity"},
                new Dictionary<string, IReadOnlyList<string>>
                {
                    {"Assets/Scenes/A.unity", new List<string> {CONTEXT}},
                    {"Assets/Prefabs/B.prefab", new List<string> {CONTEXT}},
                    {"Assets/Scenes/C.unity", new List<string> {"Assets/Other.cs"}}
                });

            Assert.AreEqual(2, references.Find(MODULE).Count);
        }

        [Test]
        public void A_module_path_that_is_empty_reports_nothing()
        {
            ModuleAssetReferences references = References(
                new List<string> {"Assets/Scenes/MainScene.unity"},
                new Dictionary<string, IReadOnlyList<string>>
                {
                    {"Assets/Scenes/MainScene.unity", new List<string> {CONTEXT}}
                });

            Assert.IsEmpty(references.Find(string.Empty));
        }

        /// <summary>
        /// The unwiring opens the assets it changes, so what it needs is the list of assets rather
        /// than the list of lines. Opening every scene and prefab in the project to find out which
        /// ones matter would be the expensive half of the job; the dependency query answers it from
        /// the index instead.
        /// </summary>
        [Test]
        public void The_assets_pointing_into_the_module_come_back_as_paths()
        {
            ModuleAssetReferences references = References(
                new List<string> {"Assets/Scenes/A.unity", "Assets/Prefabs/B.prefab", "Assets/Scenes/C.unity"},
                new Dictionary<string, IReadOnlyList<string>>
                {
                    {"Assets/Scenes/A.unity", new List<string> {CONTEXT}},
                    {"Assets/Prefabs/B.prefab", new List<string> {CONTEXT}},
                    {"Assets/Scenes/C.unity", new List<string> {"Assets/Other.cs"}}
                });

            Assert.AreEqual(
                new[] {"Assets/Scenes/A.unity", "Assets/Prefabs/B.prefab"},
                references.AssetsPointingInto(MODULE));
        }

        /// <summary>
        /// The line report names one asset once per file of the module it points at; this one is
        /// about opening the asset, and opening it twice would do the work twice.
        /// </summary>
        [Test]
        public void An_asset_naming_two_of_the_modules_files_comes_back_once()
        {
            ModuleAssetReferences references = References(
                new List<string> {"Assets/Scenes/MainScene.unity"},
                new Dictionary<string, IReadOnlyList<string>>
                {
                    {"Assets/Scenes/MainScene.unity", new List<string> {CONTEXT, MODULE + "/Prefabs/PlayerRoot.prefab"}}
                });

            Assert.AreEqual(1, references.AssetsPointingInto(MODULE).Count);
        }

        [Test]
        public void An_asset_inside_the_module_is_not_one_to_open()
        {
            string ownScene = MODULE + "/Scenes/PlayerTestScene.unity";

            ModuleAssetReferences references = References(
                new List<string> {ownScene},
                new Dictionary<string, IReadOnlyList<string>> {{ownScene, new List<string> {CONTEXT}}});

            Assert.IsEmpty(references.AssetsPointingInto(MODULE));
        }

        [Test]
        public void An_empty_module_path_names_no_assets_to_open()
        {
            ModuleAssetReferences references = References(
                new List<string> {"Assets/Scenes/MainScene.unity"},
                new Dictionary<string, IReadOnlyList<string>>
                {
                    {"Assets/Scenes/MainScene.unity", new List<string> {CONTEXT}}
                });

            Assert.IsEmpty(references.AssetsPointingInto(string.Empty));
        }

        /// <summary>
        /// Delete Module is driven from a window on Windows, where a module path arrives with
        /// backslashes while the asset database speaks forward slashes.
        /// </summary>
        [Test]
        public void A_module_path_with_backslashes_matches_the_same_assets()
        {
            ModuleAssetReferences references = References(
                new List<string> {"Assets/Scenes/MainScene.unity"},
                new Dictionary<string, IReadOnlyList<string>>
                {
                    {"Assets/Scenes/MainScene.unity", new List<string> {CONTEXT}}
                });

            Assert.AreEqual(1, references.Find(@"Assets\Modules\PlayerModule").Count);
        }
    }
}