using System.Linq;
using FlowIoC.ConsoleModule;
using FlowIoC.Editor.Console;
using NUnit.Framework;
using UnityEngine;

namespace FlowIoC.Tests
{
    /// <summary>
    /// The tag is baked into the message as rich text when the line is logged, and the console
    /// takes it off the message to draw it on the source line - so what the row is handed has to
    /// be exactly what the logger wrote, colour tags included, or the row cuts the wrong thing.
    /// </summary>
    public class FlowConsoleChannelTagTests
    {
        private readonly FlowConsoleChannelTag _tag = new FlowConsoleChannelTag();

        private static FlowLogChannel Framework(SystemLogType type)
        {
            return new SystemLogChannelTable().All().First(channel => channel.SystemType == type);
        }

        [Test]
        public void A_framework_tag_is_found_colour_tags_and_all_and_in_its_own_colour()
        {
            FlowLogChannel signal = Framework(SystemLogType.Signal);
            const string written = "<color=#FFC739FF>[Signal]</color>";

            bool found = _tag.TryFind(signal, written + " AddCurrency dispatched", out string tag, out Color color);

            Assert.IsTrue(found);
            Assert.AreEqual(written, tag);
            Assert.AreEqual(signal.Color, color);
        }

        /// <summary>What the logger puts on the front of a line is what the row is handed.</summary>
        [Test]
        public void The_tag_found_is_the_one_the_logger_writes()
        {
            FlowLogChannel command = Framework(SystemLogType.Command);
            string written = FlowLogger.FormatPrefix(command.Profile);

            Assert.IsTrue(_tag.TryFind(command, written + " Execute - AddCurrencyCommand", out string tag, out _));
            Assert.AreEqual(written, tag);
        }

        /// <summary>A module's tag - its own from the card, or the default - is found the same way.</summary>
        [Test]
        public void A_module_tag_is_found_in_the_colour_the_card_gave_it()
        {
            var profile = new FlowLogProfile().SetPrefix("[Player]", FlowTextStyle.Bold, "#39FF00");
            var player = new FlowLogChannel("PlayerModule", null, Color.green, true, profile);
            string written = FlowLogger.FormatPrefix(profile);

            Assert.IsTrue(_tag.TryFind(player, written + " hello", out string tag, out Color color));
            Assert.AreEqual(written, tag);
            Assert.AreEqual(profile.PrefixColor, color);
        }

        [Test]
        public void A_line_that_does_not_begin_with_its_channels_tag_carries_none()
        {
            Assert.IsFalse(_tag.TryFind(Framework(SystemLogType.Signal), "AddCurrency dispatched", out _, out _));
        }

        /// <summary>A line Unity wrote carries an icon for a tag, and nothing in the text.</summary>
        [Test]
        public void A_line_Unity_wrote_carries_no_tag()
        {
            Assert.IsFalse(_tag.TryFind(Framework(SystemLogType.Unity), "NullReferenceException", out _, out _));
        }

        /// <summary>Default, and a module whose card says "Profile: none", decorate nothing.</summary>
        [Test]
        public void A_channel_whose_profile_decorates_nothing_carries_no_tag()
        {
            var quiet = new FlowLogChannel("QuietModule", null, Color.green, true, new FlowLogProfile());
            var byDefault = new FlowLogChannel("Default", null, Color.white, true, null);

            Assert.IsFalse(_tag.TryFind(quiet, "hello", out _, out _));
            Assert.IsFalse(_tag.TryFind(byDefault, "hello", out _, out _));
        }

        [Test]
        public void An_empty_message_and_a_missing_channel_carry_no_tag()
        {
            Assert.IsFalse(_tag.TryFind(Framework(SystemLogType.Signal), "", out _, out _));
            Assert.IsFalse(_tag.TryFind(null, "<color=#FFC739FF>[Signal]</color> x", out _, out _));
        }
    }
}
