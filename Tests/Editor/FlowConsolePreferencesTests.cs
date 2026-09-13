using FlowIoC.ConsoleModule;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    /// <summary>
    /// The preferences are the developer's own EditorPrefs, so every test puts back what it found:
    /// a test that left them switched differently would change what the developer sees in their
    /// next session.
    /// </summary>
    public class FlowConsolePreferencesTests
    {
        private bool _wasLoggingEnabled;
        private bool _wasForwarding;
        private bool _wasDeep;
        private FlowStackTraceCapture _wasCapture;
        private int _wasMax;

        [SetUp]
        public void Remember()
        {
            FlowConsolePreferences preferences = FlowLogger.Preferences;

            _wasLoggingEnabled = preferences.IsLoggingEnabled;
            _wasForwarding = preferences.SendLogsToUnityConsole;
            _wasDeep = preferences.DeepAnalysis;
            _wasCapture = preferences.StackTraceCapture;
            _wasMax = preferences.MaxLogCount;
        }

        [TearDown]
        public void Restore()
        {
            FlowConsolePreferences preferences = FlowLogger.Preferences;

            preferences.IsLoggingEnabled = _wasLoggingEnabled;
            preferences.SendLogsToUnityConsole = _wasForwarding;
            preferences.DeepAnalysis = _wasDeep;
            preferences.StackTraceCapture = _wasCapture;
            preferences.MaxLogCount = _wasMax;
        }

        /// <summary>
        /// A value written is read back by a fresh instance - which is what the logger holds after
        /// every domain reload - so a preference survives the reload the way an asset field did.
        /// </summary>
        [Test]
        public void A_value_written_is_read_back_by_a_fresh_instance()
        {
            FlowConsolePreferences preferences = FlowLogger.Preferences;

            preferences.IsLoggingEnabled = !_wasLoggingEnabled;
            preferences.SendLogsToUnityConsole = !_wasForwarding;
            preferences.DeepAnalysis = !_wasDeep;
            preferences.StackTraceCapture = FlowStackTraceCapture.Always;
            preferences.MaxLogCount = 1234;

            var reloaded = new FlowConsolePreferences();

            Assert.AreEqual(!_wasLoggingEnabled, reloaded.IsLoggingEnabled);
            Assert.AreEqual(!_wasForwarding, reloaded.SendLogsToUnityConsole);
            Assert.AreEqual(!_wasDeep, reloaded.DeepAnalysis);
            Assert.AreEqual(FlowStackTraceCapture.Always, reloaded.StackTraceCapture);
            Assert.AreEqual(1234, reloaded.MaxLogCount);
        }

        [Test]
        public void A_negative_log_count_is_clamped_to_keeping_everything()
        {
            FlowLogger.Preferences.MaxLogCount = -5;

            Assert.AreEqual(0, FlowLogger.Preferences.MaxLogCount);
        }

        /// <summary>
        /// IsEnabled is what a hot call site asks before building a message, and it has to say
        /// what the master switch says.
        /// </summary>
        [Test]
        public void IsEnabled_follows_the_master_switch()
        {
            FlowLogger.Preferences.IsLoggingEnabled = false;
            Assert.IsFalse(FlowLogger.IsEnabled);

            FlowLogger.Preferences.IsLoggingEnabled = true;
            Assert.IsTrue(FlowLogger.IsEnabled);
        }
    }
}
