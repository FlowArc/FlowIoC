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
        public void Solo_leaves_only_the_channel_it_was_asked_for()
        {
            var all = new List<int> {5, 11, 14};

            HashSet<int> visible = _presets.Solo(11, all);

            Assert.AreEqual(1, visible.Count);
            Assert.IsTrue(visible.Contains(11));
        }

        [Test]
        public void Mute_takes_one_channel_out_and_leaves_the_rest()
        {
            HashSet<int> visible = _presets.Mute(11, new List<int> {5, 11, 14});

            Assert.AreEqual(2, visible.Count);
            Assert.IsFalse(visible.Contains(11));
            Assert.IsTrue(visible.Contains(5));
        }

        [Test]
        public void Muting_a_channel_that_is_already_hidden_changes_nothing()
        {
            HashSet<int> visible = _presets.Mute(99, new List<int> {5, 11});

            Assert.AreEqual(2, visible.Count);
        }

        [Test]
        public void Signal_chase_carries_the_three_channels_a_flow_is_read_from()
        {
            FilterPreset preset = _presets.BuiltIn.Find(p => p.Name == "Signal chase");

            Assert.IsNotNull(preset);
            CollectionAssert.Contains(preset.VisibleChannels, (int) SystemLogType.Signal);
            CollectionAssert.Contains(preset.VisibleChannels, (int) SystemLogType.Command);
            CollectionAssert.Contains(preset.VisibleChannels, (int) SystemLogType.CommandOperation);
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
