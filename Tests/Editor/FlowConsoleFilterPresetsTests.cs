using System.Collections.Generic;
using FlowIoC.ConsoleModule;
using FlowIoC.Editor.Console;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    public class FlowConsoleFilterPresetsTests
    {
        private readonly FlowConsoleFilterPresets _presets = new FlowConsoleFilterPresets();

        [Test]
        public void Signal_chase_carries_the_two_channels_a_flow_is_read_from()
        {
            FilterPreset preset = _presets.BuiltIn.Find(p => p.Name == "Signal chase");

            Assert.IsNotNull(preset);
            CollectionAssert.AreEquivalent(
                new[] {"Signal", "Command"},
                preset.VisibleChannels);
        }

        /// <summary>
        /// The signal channel is in both, because a screen is opened by a signal and the two rows
        /// beside each other are what says whether the open ever left the module that asked.
        /// </summary>
        [Test]
        public void Screen_debug_carries_the_signal_that_opened_the_screen()
        {
            FilterPreset preset = _presets.BuiltIn.Find(p => p.Name == "Screen debug");

            Assert.IsNotNull(preset);
            CollectionAssert.AreEquivalent(
                new[] {"Signal", "Screen"},
                preset.VisibleChannels);
        }

        [Test]
        public void A_saved_preset_reads_back_with_its_channels()
        {
            _presets.Delete("probe");
            _presets.Save("probe", new List<string> {"Context", "Command"});

            List<FilterPreset> saved = _presets.LoadSaved();
            FilterPreset probe = saved.Find(p => p.Name == "probe");

            Assert.IsNotNull(probe);
            CollectionAssert.AreEquivalent(new[] {"Context", "Command"}, probe.VisibleChannels);

            _presets.Delete("probe");
            Assert.IsNull(_presets.LoadSaved().Find(p => p.Name == "probe"));
        }

        /// <summary>
        /// Presets are one developer's, so they live in EditorPrefs. Saving one is not applying
        /// it: the switches a developer has thrown stay exactly where they were.
        /// </summary>
        [Test]
        public void Saving_a_preset_does_not_throw_a_switch()
        {
            _presets.Delete("probe");
            Assert.IsTrue(FlowLogger.Channels.TryGet("Context", out FlowLogChannel context));
            bool before = FlowLogger.Channels.IsShown(context);

            _presets.Save("probe", new List<string> {"Context"});

            Assert.AreEqual(before, FlowLogger.Channels.IsShown(context));
            _presets.Delete("probe");
        }
    }
}