using System.Linq;
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
        public void A_screen_yields_an_entry_for_its_prefab_and_one_for_its_config()
        {
            Assert.AreEqual(2, _entries.For("MainScreen").Length);
        }

        [Test]
        public void The_prefab_goes_to_a_group_of_its_own_under_its_bare_name()
        {
            ScreenAddressableEntry prefab = _entries.For("MainScreen").First(e => e.Address == "MainScreen");

            Assert.AreEqual("Local_Screen-Main", prefab.GroupName);
            Assert.AreEqual(ScreenAddressableEntries.PrefabLabel, prefab.Label);
        }

        [Test]
        public void The_config_joins_the_shared_config_group_and_carries_the_label()
        {
            ScreenAddressableEntry config = _entries.For("MainScreen").First(e => e.Address == "MainScreenConfig");

            Assert.AreEqual("Local_Screen-Configs", config.GroupName);
            Assert.AreEqual("ScreenConfig", config.Label);
        }

        [Test]
        public void A_screen_whose_name_does_not_end_in_Screen_keeps_its_whole_name_in_the_group()
        {
            ScreenAddressableEntry prefab = _entries.For("Hud").First(e => e.Address == "Hud");

            Assert.AreEqual("Local_Screen-Hud", prefab.GroupName);
        }
    }
}
