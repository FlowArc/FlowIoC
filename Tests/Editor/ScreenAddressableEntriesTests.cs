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
    }
}
