using FlowIoC.Editor.Console;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    public class FlowConsoleStateTests
    {
        private FlowConsoleState _state;
        private bool _clearOnPlay;
        private bool _errorPause;

        [SetUp]
        public void Remember()
        {
            _state = new FlowConsoleState();
            _clearOnPlay = _state.ClearOnPlay;
            _errorPause = _state.ErrorPause;
        }

        [TearDown]
        public void Restore()
        {
            _state.ClearOnPlay = _clearOnPlay;
            _state.ErrorPause = _errorPause;
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
