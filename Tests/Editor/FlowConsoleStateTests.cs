using FlowIoC.Editor.Console;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    public class FlowConsoleStateTests
    {
        private FlowConsoleState _state;
        private bool _clearOnPlay;
        private bool _errorPause;
        private FlowConsoleTimeFormat _timeFormat;
        private float _filtersPanelWidth;

        [SetUp]
        public void Remember()
        {
            _state = new FlowConsoleState();
            _clearOnPlay = _state.ClearOnPlay;
            _errorPause = _state.ErrorPause;
            _timeFormat = _state.TimeFormat;
            _filtersPanelWidth = _state.FiltersPanelWidth;
        }

        [TearDown]
        public void Restore()
        {
            _state.ClearOnPlay = _clearOnPlay;
            _state.ErrorPause = _errorPause;
            _state.TimeFormat = _timeFormat;
            _state.FiltersPanelWidth = _filtersPanelWidth;
        }

        /// <summary>
        /// Saved as its number, so a value outside the enum - a build of the console that knew
        /// one more format, or a hand-edited pref - reads back as one of the three rather than as
        /// a format the row drawer has no branch for.
        /// </summary>
        [Test]
        public void The_time_format_reads_back_what_was_written_and_never_leaves_the_enum()
        {
            _state.TimeFormat = FlowConsoleTimeFormat.Frame;
            Assert.AreEqual(FlowConsoleTimeFormat.Frame, new FlowConsoleState().TimeFormat);

            UnityEditor.EditorPrefs.SetInt("FlowIoC.Console.TimeFormat", 7);
            Assert.AreEqual(FlowConsoleTimeFormat.Frame, new FlowConsoleState().TimeFormat);

            UnityEditor.EditorPrefs.SetInt("FlowIoC.Console.TimeFormat", -3);
            Assert.AreEqual(FlowConsoleTimeFormat.Classic, new FlowConsoleState().TimeFormat);
        }

        [Test]
        public void The_filters_panel_width_reads_back_what_was_dragged()
        {
            _state.FiltersPanelWidth = 310f;
            Assert.AreEqual(310f, new FlowConsoleState().FiltersPanelWidth);
        }

        [Test]
        public void A_switch_reads_back_what_was_written_to_it()
        {
            _state.ClearOnPlay = true;
            Assert.IsTrue(new FlowConsoleState().ClearOnPlay);

            _state.ClearOnPlay = false;
            Assert.IsFalse(new FlowConsoleState().ClearOnPlay);
        }

        /// <summary>
        /// These are one developer's toolbar toggles. Putting them in CD_FlowConsole would put
        /// them in everybody's diff, so they live in EditorPrefs instead.
        /// </summary>
        [Test]
        public void Error_pause_reads_back_what_was_written_to_it()
        {
            _state.ErrorPause = true;
            Assert.IsTrue(new FlowConsoleState().ErrorPause);
        }

        [Test]
        public void Every_switch_starts_off()
        {
            _state.ClearOnPlay = false;
            _state.ErrorPause = false;

            var fresh = new FlowConsoleState();

            Assert.IsFalse(fresh.ClearOnPlay);
            Assert.IsFalse(fresh.ErrorPause);
        }
    }
}