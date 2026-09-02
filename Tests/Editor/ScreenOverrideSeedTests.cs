using FlowIoC.BaseModule.Root;
using FlowIoC.Editor.Root;
using FlowIoC.ScreenModule.Data;
using FlowIoC.ScreenModule.Enums;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    /// <summary>
    /// Ticking the override starts the edit from what the context declares rather than from zero,
    /// but only while nothing has been edited yet - so toggling the override off and on again does
    /// not throw away work.
    /// </summary>
    public class ScreenOverrideSeedTests
    {
        private ScreenOverrideSeed _seed;

        [SetUp]
        public void SetUp() => _seed = new ScreenOverrideSeed();

        private static ScreenCVO Declaration() => new()
        {
            ManagerId = 1,
            Layer = 4,
            Tag = ScreenTag.GroupC,
            HasShowAnimation = true,
            HasHideAnimation = true,
            Load = ScreenLoadCVO.Addressable("Seeded")
        };

        [Test]
        public void An_untouched_entry_takes_the_declared_values()
        {
            SubContextData seeded = _seed.Apply(new SubContextData {OverrideScreen = true}, Declaration());

            Assert.AreEqual(1, seeded.ScreenManagerId);
            Assert.AreEqual(4, seeded.ScreenLayer);
            Assert.AreEqual(ScreenTag.GroupC, seeded.ScreenTag);
            Assert.IsTrue(seeded.ScreenHasShowAnimation);
            Assert.IsTrue(seeded.ScreenHasHideAnimation);
        }

        [Test]
        public void An_edited_entry_keeps_what_it_holds()
        {
            SubContextData edited = new SubContextData {OverrideScreen = true, ScreenLayer = 9};

            SubContextData seeded = _seed.Apply(edited, Declaration());

            Assert.AreEqual(9, seeded.ScreenLayer);
            Assert.AreEqual(0, seeded.ScreenManagerId);
        }

        [Test]
        public void A_missing_declaration_leaves_the_entry_alone()
        {
            SubContextData seeded = _seed.Apply(new SubContextData {OverrideScreen = true}, null);

            Assert.AreEqual(0, seeded.ScreenLayer);
            Assert.AreEqual(ScreenTag.Default, seeded.ScreenTag);
        }
    }
}
