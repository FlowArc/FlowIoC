using FlowIoC.Editor.Addressables;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    public class ScreenAddressableEntriesTests
    {
        private ScreenAddressableEntries _entries;

        [SetUp]
        public void SetUp()
        {
            _entries = new ScreenAddressableEntries();
        }

        [Test]
        public void The_prefab_goes_to_a_group_of_its_own_under_its_bare_name()
        {
            ScreenAddressableEntry prefab = _entries.For("MainScreen");

            Assert.AreEqual("MainScreen", prefab.Address);
            Assert.AreEqual("Local_Screen-Main", prefab.GroupName);
            Assert.AreEqual(ScreenAddressableEntries.PrefabLabel, prefab.Label);
        }

        [Test]
        public void A_screen_whose_name_does_not_end_in_Screen_keeps_its_whole_name_in_the_group()
        {
            Assert.AreEqual("Local_Screen-Hud", _entries.For("Hud").GroupName);
        }

        [Test]
        public void A_prefab_under_Resources_is_loaded_by_path_and_is_not_addressable()
        {
            Assert.IsTrue(_entries.IsAddressable("Assets/Modules/MainModule/zScreenModules/MainScreenModule/Prefabs/MainScreen.prefab"));
            Assert.IsFalse(_entries.IsAddressable("Assets/Modules/LoadingModule/zScreenModules/LoadingScreenModule/Resources/LoadingScreen.prefab"));
            Assert.IsFalse(_entries.IsAddressable(@"Assets\Modules\LoadingModule\zScreenModules\LoadingScreenModule\Resources\LoadingScreen.prefab"));
        }

        [Test]
        public void Art_goes_into_the_screens_group_under_its_own_name_and_carries_no_label()
        {
            ScreenAddressableEntry art = _entries.ForArt("LoadingScreen",
                "Assets/Modules/LoadingModule/zScreenModules/LoadingScreenModule/Art/T_LoadingBackground.png");

            Assert.AreEqual("T_LoadingBackground", art.Address);
            Assert.AreEqual("Local_Screen-Loading", art.GroupName);
            Assert.IsNull(art.Label);
            Assert.AreEqual("Assets/Modules/LoadingModule/zScreenModules/LoadingScreenModule/Art/T_LoadingBackground.png", art.AssetPath);
        }

        [Test]
        public void The_screen_an_Art_folder_belongs_to_is_read_off_the_module_folder_above_it()
        {
            Assert.AreEqual("LoadingScreen", _entries.ScreenOfArtFolder("Assets/Modules/LoadingModule/zScreenModules/LoadingScreenModule/Art"));
            Assert.AreEqual("LoadingScreen",
                _entries.ScreenOfArtFolder(@"D:\Game\Assets\Modules\LoadingModule\zScreenModules\LoadingScreenModule\Art"));
            Assert.IsNull(_entries.ScreenOfArtFolder("Assets/Modules/LoadingModule/Art"), "a main module's art has no screen group to go to");
            Assert.IsNull(_entries.ScreenOfArtFolder("Assets/Modules/LoadingModule/zScreenModules/LoadingScreenModule/Prefabs"));
        }
    }
}
