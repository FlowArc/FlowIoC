using FlowIoC.ScreenModule.Data;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    /// <summary>
    /// Where a screen's prefab comes from is one of two things, and a screen that says neither
    /// must be recognisable as such before anything tries to load it.
    /// </summary>
    public class ScreenLoadCVOTests
    {
        [Test]
        public void Addressable_carries_its_address()
        {
            ScreenLoadCVO load = ScreenLoadCVO.Addressable("MainScreen");

            Assert.AreEqual(ScreenLoadType.Addressable, load.Kind);
            Assert.AreEqual("MainScreen", load.Key);
            Assert.IsTrue(load.IsValid);
        }

        [Test]
        public void Resource_carries_its_path()
        {
            ScreenLoadCVO load = ScreenLoadCVO.Resource("Screens/Main");

            Assert.AreEqual(ScreenLoadType.Resource, load.Kind);
            Assert.AreEqual("Screens/Main", load.Key);
            Assert.IsTrue(load.IsValid);
        }

        [Test]
        public void A_load_nobody_filled_in_is_not_valid()
        {
            Assert.IsFalse(default(ScreenLoadCVO).IsValid);
            Assert.IsFalse(new ScreenCVO().Load.IsValid);
            Assert.IsFalse(ScreenLoadCVO.Addressable("").IsValid);
        }

        [Test]
        public void A_screen_defaults_to_manager_zero_layer_zero_and_the_default_tag()
        {
            ScreenCVO screen = new ScreenCVO();

            Assert.AreEqual(0, screen.ManagerId);
            Assert.AreEqual(0, screen.Layer);
            Assert.AreEqual(FlowIoC.ScreenModule.Enums.ScreenTag.Default, screen.Tag);
            Assert.IsFalse(screen.HasShowAnimation);
            Assert.IsFalse(screen.HasHideAnimation);
        }
    }
}
