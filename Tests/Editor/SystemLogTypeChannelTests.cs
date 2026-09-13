using System;
using System.Collections.Generic;
using FlowIoC.ConsoleModule;
using NUnit.Framework;
using UnityEngine;

namespace FlowIoC.Tests
{
    public class SystemLogTypeChannelTests
    {
        [Test]
        public void Unity_and_Compiler_are_channels_of_their_own()
        {
            Assert.AreEqual(45, (int) SystemLogType.Unity);
            Assert.AreEqual(50, (int) SystemLogType.Compiler);
        }

        /// <summary>
        /// Every enum value carries its number so that inserting one in the middle cannot
        /// renumber what is already on disk. Two values sharing a number would let one channel's
        /// rows be filtered by the other's toggle.
        /// </summary>
        [Test]
        public void No_two_system_channels_share_a_number()
        {
            var seen = new HashSet<int>();

            foreach (SystemLogType value in Enum.GetValues(typeof(SystemLogType)))
                Assert.IsTrue(seen.Add((int) value), $"{value} shares its number with another channel.");
        }

        /// <summary>
        /// The table is the framework's channels, built from code rather than read from an asset,
        /// so every value the enum declares is there - except All, which is not a channel anything
        /// writes to.
        /// </summary>
        [Test]
        public void The_table_carries_a_channel_for_every_framework_channel()
        {
            var table = new SystemLogChannelTable();
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (FlowLogChannel channel in table.All())
            {
                Assert.IsTrue(channel.IsFrameworkOwned, channel.Name + " is not framework-owned.");
                names.Add(channel.Name);
            }

            foreach (SystemLogType value in Enum.GetValues(typeof(SystemLogType)))
            {
                if (value == SystemLogType.All)
                    Assert.IsFalse(names.Contains("All"), "All is not a channel.");
                else
                    Assert.IsTrue(names.Contains(value.ToString()), $"{value} has no channel.");
            }
        }

        /// <summary>
        /// 35 was Model, retired because nothing could write to it. A retired number is never
        /// reused, so this is what says so - the next channel added takes the next free number
        /// rather than the hole this one left.
        /// </summary>
        [Test]
        public void The_number_a_retired_channel_used_is_not_handed_out_again()
        {
            foreach (SystemLogType value in Enum.GetValues(typeof(SystemLogType)))
                Assert.AreNotEqual(35, (int) value, $"{value} took the number the Model channel retired.");
        }

        /// <summary>
        /// A channel Unity writes has an icon for a tag, drawn by the console on the row, so its
        /// profile writes nothing into the message: a line Unity wrote is recorded as Unity wrote
        /// it. The framework's own channels keep their text tag, in the channel's own colour.
        /// </summary>
        [Test]
        public void The_channels_Unity_writes_carry_no_text_tag()
        {
            var channels = new FlowLogChannels(null);

            foreach (string name in new[] {"Unity", "Compiler", "Shader"})
            {
                Assert.IsTrue(channels.TryGet(name, out FlowLogChannel channel), name + " is missing.");
                Assert.IsTrue(channel.IsWrittenByUnity, name + " is not marked as Unity's.");
                Assert.IsNull(channel.Profile, "A " + name + " line is decorated.");
                Assert.IsNull(channels.ProfileOf(name));
            }

            Assert.IsTrue(channels.TryGet("Context", out FlowLogChannel context));
            Assert.AreEqual("[Context]", context.Profile.Prefix);
            Assert.AreEqual(context.Color, context.Profile.PrefixColor);
            Assert.IsFalse(context.IsWrittenByUnity);
        }

        [Test]
        public void No_framework_channel_is_left_white()
        {
            foreach (FlowLogChannel channel in new SystemLogChannelTable().All())
                Assert.AreNotEqual(Color.white, channel.Color, channel.Name + " is white.");
        }

        /// <summary>
        /// What a reader sees before they throw a switch: the game's own traffic and what Unity
        /// wrote, with the framework's machinery underneath switched off until it is wanted.
        /// </summary>
        [Test]
        public void The_defaults_show_the_traffic_and_hide_the_machinery()
        {
            var channels = new FlowLogChannels(null);

            foreach (string on in new[] {"Signal", "Command", "Unity", "Compiler", "Shader"})
            {
                Assert.IsTrue(channels.TryGet(on, out FlowLogChannel channel));
                Assert.IsTrue(channel.IsVisibleByDefault, on + " is off by default.");
            }

            foreach (string off in new[] {"Context", "Injection", "SignalOperation", "CommandOperation", "Function", "Screen", "Pool", "Asset"})
            {
                Assert.IsTrue(channels.TryGet(off, out FlowLogChannel channel));
                Assert.IsFalse(channel.IsVisibleByDefault, off + " is on by default.");
            }
        }
    }
}