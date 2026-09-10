using FlowIoC.ConsoleModule;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace FlowIoC.Tests
{
    public class FlowConsoleChannelVisibilityTests
    {
        private const string PROBE_KEY = "FlowIoC.Console.Channels.Probe";

        private FlowConsoleChannelVisibility _visibility;

        [SetUp]
        public void SetUp()
        {
            _visibility = new FlowConsoleChannelVisibility(PROBE_KEY);
            _visibility.Reset();
        }

        [TearDown]
        public void TearDown()
        {
            _visibility.Reset();
        }

        private static CD_FlowConsole.FlowConsoleLogTypeCVO Channel(string name, bool onByDefault)
        {
            return new CD_FlowConsole.FlowConsoleLogTypeCVO {Name = name, IsVisibleByDefault = onByDefault};
        }

        /// <summary>
        /// The project default is what the asset says, and a developer who never touched a channel
        /// sees exactly that - which is also what a channel a module added yesterday shows as.
        /// </summary>
        [Test]
        public void A_channel_nobody_touched_follows_the_project_default()
        {
            Assert.IsTrue(_visibility.IsShown(Channel("Signal", true)));
            Assert.IsFalse(_visibility.IsShown(Channel("Injection", false)));
        }

        /// <summary>
        /// A switch the developer threw outranks the default in both directions, and it is still
        /// thrown for a fresh instance - which is what the window is after every domain reload.
        /// </summary>
        [Test]
        public void A_switch_the_developer_threw_outranks_the_default()
        {
            _visibility.Show(Channel("Signal", true), false);
            _visibility.Show(Channel("Injection", false), true);

            var reloaded = new FlowConsoleChannelVisibility(PROBE_KEY);

            Assert.IsFalse(reloaded.IsShown(Channel("Signal", true)));
            Assert.IsTrue(reloaded.IsShown(Channel("Injection", false)));
        }

        /// <summary>
        /// Reset is the way back to what the project agreed on, and it has to reach the disk: a
        /// switch that came back after a domain reload was never reset.
        /// </summary>
        [Test]
        public void Reset_puts_every_channel_back_on_the_project_default()
        {
            _visibility.Show(Channel("Signal", true), false);

            _visibility.Reset();

            Assert.IsFalse(_visibility.HasSwitches);
            Assert.IsTrue(new FlowConsoleChannelVisibility(PROBE_KEY).IsShown(Channel("Signal", true)));
        }

        /// <summary>
        /// A channel switched back to where the project has it carries no switch at all. The
        /// developer said nothing lasting about it, so when the project moves its default later
        /// they follow - and the Reset row has nothing to offer when nothing stands out.
        /// </summary>
        [Test]
        public void A_channel_put_back_on_its_default_leaves_no_switch_behind()
        {
            var signal = Channel("Signal", true);

            _visibility.Show(signal, false);
            _visibility.Show(signal, true);

            Assert.IsFalse(_visibility.HasSwitches);
            Assert.IsFalse(EditorPrefs.HasKey(PROBE_KEY), "An empty list was written where no key should be.");
        }

        /// <summary>
        /// The settings asset is what the logger asks before it forwards a row to Unity's console,
        /// and in the Editor that answer has to be the developer's - a channel they hid in the
        /// window must not go on flooding the other console. The switch is thrown through the
        /// asset's own Visibility, on this project's real key, and put back where it was.
        /// </summary>
        [Test]
        public void In_the_Editor_the_settings_answer_with_the_developers_switch()
        {
            var settings = ScriptableObject.CreateInstance<CD_FlowConsole>();

            try
            {
                Assert.IsTrue(settings.TryGetLogType("Signal", out var signal));
                bool before = settings.Visibility.IsShown(signal);

                try
                {
                    settings.Visibility.Show(signal, false);

                    Assert.IsFalse(settings.IsLogTypeVisible("Signal"));
                    Assert.IsFalse(settings.IsLogTypeVisible((int) SystemLogType.Signal));
                }
                finally
                {
                    settings.Visibility.Show(signal, before);
                }
            }
            finally
            {
                ScriptableObject.DestroyImmediate(settings);
            }
        }
    }
}