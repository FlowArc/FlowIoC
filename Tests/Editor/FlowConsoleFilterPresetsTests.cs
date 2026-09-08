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
                new[] {(int) SystemLogType.Signal, (int) SystemLogType.Command},
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
                new[] {(int) SystemLogType.Signal, (int) SystemLogType.Screen},
                preset.VisibleChannels);
        }

        [Test]
        public void A_saved_preset_reads_back_with_its_channels()
        {
            _presets.Delete("probe");
            _presets.Save("probe", new List<int> {5, 14});

            List<FilterPreset> saved = _presets.LoadSaved();
            FilterPreset probe = saved.Find(p => p.Name == "probe");

            Assert.IsNotNull(probe);
            CollectionAssert.AreEquivalent(new[] {5, 14}, probe.VisibleChannels);

            _presets.Delete("probe");
            Assert.IsNull(_presets.LoadSaved().Find(p => p.Name == "probe"));
        }

        /// <summary>
        /// Presets are one developer's, so they live in EditorPrefs. CD_FlowConsole is committed
        /// and a personal filter must not turn up in somebody's diff.
        /// </summary>
        [Test]
        public void Saving_a_preset_does_not_touch_the_settings_asset()
        {
            _presets.Delete("probe");
            FlowLogger.Settings.LogTypes[0].IsVisible = true;

            _presets.Save("probe", new List<int> {5});

            Assert.IsTrue(FlowLogger.Settings.LogTypes[0].IsVisible);
            _presets.Delete("probe");
        }
    }
}