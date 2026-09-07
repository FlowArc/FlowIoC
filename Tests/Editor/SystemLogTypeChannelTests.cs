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
        /// renumber the assets already on disk. Two values sharing a number would let one
        /// channel's rows be filtered by the other's toggle.
        /// </summary>
        [Test]
        public void No_two_system_channels_share_a_number()
        {
            var seen = new HashSet<int>();

            foreach (SystemLogType value in Enum.GetValues(typeof(SystemLogType)))
                Assert.IsTrue(seen.Add((int) value), $"{value} shares its number with another channel.");
        }

        [Test]
        public void A_fresh_settings_asset_carries_a_row_for_every_channel()
        {
            var settings = ScriptableObject.CreateInstance<CD_FlowConsole>();

            try
            {
                foreach (SystemLogType value in Enum.GetValues(typeof(SystemLogType)))
                    Assert.IsTrue(settings.TryGetLogType((int) value, out _), $"{value} has no row.");
            }
            finally
            {
                ScriptableObject.DestroyImmediate(settings);
            }
        }

        [Test]
        public void The_new_channels_are_not_left_white()
        {
            var settings = ScriptableObject.CreateInstance<CD_FlowConsole>();

            try
            {
                settings.TryGetLogType((int) SystemLogType.Unity, out var unity);
                settings.TryGetLogType((int) SystemLogType.Compiler, out var compiler);

                Assert.AreNotEqual(Color.white, unity.LogColor);
                Assert.AreNotEqual(Color.white, compiler.LogColor);
            }
            finally
            {
                ScriptableObject.DestroyImmediate(settings);
            }
        }
    }
}
