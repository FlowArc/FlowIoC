using FlowIoC.BaseModule.Root;
using FlowIoC.Editor.Root;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    /// <summary>
    /// A folded entry still has to say whether the Root overrode the screen it lists, because that
    /// is the one thing about it a reader cannot guess from the context class. An entry that takes
    /// the declaration as it comes says nothing, so the summary reads as a deviation marker rather
    /// than as noise on every row.
    /// </summary>
    public class ScreenOverrideSummaryTests
    {
        private ScreenOverrideSummary _summary;

        [SetUp]
        public void SetUp() => _summary = new ScreenOverrideSummary();

        [Test]
        public void An_entry_that_does_not_override_has_no_summary()
        {
            string text = _summary.For(new SubContextData {ScreenManagerId = 1, ScreenLayer = 3});

            Assert.AreEqual(string.Empty, text);
        }

        [Test]
        public void An_overriding_entry_names_its_manager_and_layer()
        {
            string text = _summary.For(new SubContextData
            {
                OverrideScreen = true,
                ScreenManagerId = 1,
                ScreenLayer = 3
            });

            Assert.AreEqual("M1 L3", text);
        }

        [Test]
        public void An_override_that_matches_the_defaults_still_says_so()
        {
            string text = _summary.For(new SubContextData {OverrideScreen = true});

            Assert.AreEqual("M0 L0", text);
        }
    }
}
