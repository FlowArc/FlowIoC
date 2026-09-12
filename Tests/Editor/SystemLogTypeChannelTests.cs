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
        /// A settings asset written before a channel was retired still carries its row, and a
        /// mandatory row cannot be deleted from the Filters panel. Without the prune the project
        /// keeps a column nothing can fill and nobody can remove.
        /// </summary>
        [Test]
        public void A_row_for_a_channel_the_enum_no_longer_declares_is_dropped()
        {
            var settings = ScriptableObject.CreateInstance<CD_FlowConsole>();

            try
            {
                settings.LogTypes.Add(new CD_FlowConsole.FlowConsoleLogTypeCVO
                {
                    Name = "Model", Value = 35, IsVisibleByDefault = true, IsMandatory = true
                });

                // Looked up once so the name cache holds the stale row. A prune that drops it from
                // the list and leaves the cache alone still answers this call with it.
                settings.RebuildCache();
                Assert.IsTrue(settings.TryGetLogType("Model", out _), "The row under test was never there.");

                Assert.IsTrue(settings.PruneRetiredSystemLogTypes(), "The stale row was not seen.");
                Assert.IsFalse(settings.TryGetLogType("Model", out _), "The stale row is still there.");
            }
            finally
            {
                ScriptableObject.DestroyImmediate(settings);
            }
        }

        /// <summary>
        /// The prune reads a row's mandatory flag, not its name, so a game's own channel - which
        /// is never in the enum - has to survive it.
        /// </summary>
        [Test]
        public void A_projects_own_channel_survives_the_prune()
        {
            var settings = ScriptableObject.CreateInstance<CD_FlowConsole>();

            try
            {
                settings.LogTypes.Add(new CD_FlowConsole.FlowConsoleLogTypeCVO
                {
                    Name = "PlayerModule", Value = 1000, IsVisibleByDefault = true, IsMandatory = false
                });

                settings.RebuildCache();
                settings.PruneRetiredSystemLogTypes();

                Assert.IsTrue(settings.TryGetLogType("PlayerModule", out _), "A project channel was pruned.");
            }
            finally
            {
                ScriptableObject.DestroyImmediate(settings);
            }
        }

        /// <summary>
        /// Default is a mandatory profile and is not a channel, so the enum cannot vouch for it.
        /// Pruning it would leave every log written without a profile printing no tag at all.
        /// </summary>
        [Test]
        public void The_Default_profile_survives_the_prune()
        {
            var settings = ScriptableObject.CreateInstance<CD_FlowConsole>();

            try
            {
                settings.LogProfiles.Add(new FlowLogProfileData {Name = "Model", IsMandatory = true});

                settings.PruneRetiredSystemProfiles();

                Assert.IsNull(settings.LogProfiles.Find(p => p.Name == "Model"), "The stale profile is still there.");
                Assert.IsNotNull(settings.LogProfiles.Find(p => p.Name == "Default"), "Default was pruned.");
            }
            finally
            {
                ScriptableObject.DestroyImmediate(settings);
            }
        }

        /// <summary>
        /// A channel Unity writes has an icon for a tag, drawn by the console on the row, so its
        /// profile writes nothing into the message: a line Unity wrote is recorded as Unity wrote
        /// it. The framework's own channels keep their text tag.
        /// </summary>
        [Test]
        public void The_channels_Unity_writes_carry_no_text_tag()
        {
            var settings = ScriptableObject.CreateInstance<CD_FlowConsole>();

            try
            {
                foreach (string name in new[] {"Unity", "Compiler", "Shader"})
                {
                    FlowLogProfileData profile = settings.LogProfiles.Find(p => p.Name == name);

                    Assert.IsNotNull(profile, name + " has no profile.");
                    Assert.IsTrue(string.IsNullOrEmpty(profile.Prefix), name + "'s profile writes a tag.");
                    Assert.IsNull(settings.GetResolvedProfile(name), "A " + name + " line is decorated.");
                }

                Assert.AreEqual("[Context]", settings.LogProfiles.Find(p => p.Name == "Context").Prefix);
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