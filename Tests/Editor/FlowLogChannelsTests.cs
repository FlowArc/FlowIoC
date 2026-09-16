using System.Collections.Generic;
using FlowIoC.ConsoleModule;
using NUnit.Framework;
using UnityEngine;

namespace FlowIoC.Tests
{
    /// <summary>
    /// The table reads the project's channels off a class by reflection, the way it reads
    /// FlowModule. The class here stands in for what the generator writes, so the test says what
    /// a part has to look like for the table to understand it - and is not tied to whichever
    /// modules this project happens to have.
    /// </summary>
    public class FlowLogChannelsTests
    {
        private static class DeclaredChannels
        {
            public const string Default = "Default";

            public const string PlayerModule = "PlayerModule";
            public static readonly Color PlayerModuleColor = new Color32(229, 165, 10, 255);

            public static readonly FlowLogProfile PlayerModuleProfile =
                new FlowLogProfile().SetPrefix("[Player]", FlowTextStyle.Bold, "#39FF00");

            public const string AbTestModule = "AbTestModule";

            /// <summary>A card that says "Profile: none": the part holds a profile that decorates nothing.</summary>
            public const string QuietModule = "QuietModule";
            public static readonly FlowLogProfile QuietModuleProfile = new FlowLogProfile();

            /// <summary>Not a channel: a const that is not a string, and a field that is not a const.</summary>
            public const int NotAChannel = 3;

            public static string NotAChannelEither = "NotAChannelEither";
        }

        private FlowLogChannels _channels;

        [SetUp]
        public void SetUp() => _channels = new FlowLogChannels(typeof(DeclaredChannels));

        [Test]
        public void Every_const_string_on_the_class_is_a_channel_and_nothing_else_is()
        {
            Assert.IsTrue(_channels.TryGet("PlayerModule", out _));
            Assert.IsTrue(_channels.TryGet("AbTestModule", out _));
            Assert.IsTrue(_channels.TryGet("Default", out _));
            Assert.IsFalse(_channels.TryGet("NotAChannel", out _));
            Assert.IsFalse(_channels.TryGet("NotAChannelEither", out _));
        }

        [Test]
        public void A_channel_is_found_whatever_its_case_and_answers_with_its_own_spelling()
        {
            Assert.IsTrue(_channels.TryGet("playermodule", out FlowLogChannel channel));
            Assert.AreEqual("PlayerModule", channel.Name);
        }

        /// <summary>
        /// The colour and the profile hang off the identifier: PlayerModule, PlayerModuleColor,
        /// PlayerModuleProfile. A part written before colours were is white rather than missing.
        /// </summary>
        [Test]
        public void The_colour_and_the_profile_beside_a_channel_are_read_with_it()
        {
            Assert.IsTrue(_channels.TryGet("PlayerModule", out FlowLogChannel player));
            Assert.AreEqual((Color) new Color32(229, 165, 10, 255), player.Color);
            Assert.AreEqual("[Player]", player.Profile.Prefix);
            Assert.AreEqual(FlowTextStyle.Bold, player.Profile.PrefixStyle);
            Assert.AreSame(player.Profile, _channels.ProfileOf("PlayerModule"));

            Assert.IsTrue(_channels.TryGet("AbTestModule", out FlowLogChannel abTest));
            Assert.AreEqual(Color.white, abTest.Color);
        }

        /// <summary>
        /// A module whose card says nothing about its profile carries its name as a tag, the way a
        /// framework channel carries "[Signal]": "[AbTest]" for AbTestModule, in the module's colour.
        /// Default is no module and carries none, and a card that says "Profile: none" is the one
        /// way a module's lines carry no tag.
        /// </summary>
        [Test]
        public void A_module_with_no_profile_line_carries_its_name_as_a_tag()
        {
            Assert.IsTrue(_channels.TryGet("AbTestModule", out FlowLogChannel abTest));
            Assert.AreEqual("[AbTest]", abTest.Profile.Prefix);
            Assert.AreEqual(FlowTextStyle.None, abTest.Profile.PrefixStyle);
            Assert.AreEqual(abTest.Color, abTest.Profile.PrefixColor);

            Assert.IsTrue(_channels.TryGet("Default", out FlowLogChannel byDefault));
            Assert.IsNull(byDefault.Profile);

            Assert.IsTrue(_channels.TryGet("QuietModule", out FlowLogChannel quiet));
            Assert.IsNotNull(quiet.Profile);
            Assert.IsNull(quiet.Profile.Prefix);
        }

        [Test]
        public void The_default_tag_is_the_module_name_without_its_suffix()
        {
            Assert.AreEqual("[Player]", FlowLogChannels.DefaultProfileFor("PlayerModule", Color.red).Prefix);
            Assert.AreEqual("[Module]", FlowLogChannels.DefaultProfileFor("Module", Color.red).Prefix);
            Assert.AreEqual("[Hud]", FlowLogChannels.DefaultProfileFor("Hud", Color.red).Prefix);
            Assert.AreEqual(Color.red, FlowLogChannels.DefaultProfileFor("PlayerModule", Color.red).PrefixColor);
            Assert.IsNull(FlowLogChannels.DefaultProfileFor("Default", Color.red));
        }

        [Test]
        public void A_project_channel_is_not_the_frameworks_and_is_on_by_default()
        {
            Assert.IsTrue(_channels.TryGet("PlayerModule", out FlowLogChannel player));

            Assert.IsFalse(player.IsFrameworkOwned);
            Assert.IsFalse(player.IsWrittenByUnity);
            Assert.IsNull(player.SystemType);
            Assert.IsTrue(player.IsVisibleByDefault);
        }

        /// <summary>
        /// The framework's channels come first in enum order, then Default, then the modules by
        /// name - the order the Filters panel and the presets read the list in.
        /// </summary>
        [Test]
        public void The_framework_leads_then_Default_then_the_modules_by_name()
        {
            var names = new List<string>();
            foreach (FlowLogChannel channel in _channels.All) names.Add(channel.Name);

            int lastFramework = names.IndexOf("Shader");
            int defaultAt = names.IndexOf("Default");

            Assert.AreEqual(0, names.IndexOf("Context"));
            Assert.Less(lastFramework, defaultAt);
            Assert.AreEqual(defaultAt + 1, names.IndexOf("AbTestModule"));
            Assert.AreEqual(defaultAt + 2, names.IndexOf("PlayerModule"));
            Assert.AreEqual(defaultAt + 3, names.IndexOf("QuietModule"));
            Assert.AreEqual(defaultAt + 4, names.Count);
        }

        [Test]
        public void A_framework_channel_is_found_by_its_enum_value()
        {
            Assert.IsTrue(_channels.TryGet(SystemLogType.Command, out FlowLogChannel command));
            Assert.AreEqual("Command", command.Name);
            Assert.AreEqual(SystemLogType.Command, command.SystemType);

            Assert.IsFalse(_channels.TryGet(SystemLogType.All, out _), "All is not a channel.");
        }

        /// <summary>
        /// A channel nobody declared has no switch anywhere that could hide it, so a log on it is
        /// shown - and a null name is asked about safely, because a row from a device may carry one.
        /// </summary>
        [Test]
        public void An_undeclared_channel_is_shown_and_a_null_name_is_not_a_fault()
        {
            Assert.IsTrue(_channels.IsShown("NobodyDeclaredThis"));
            Assert.IsFalse(_channels.TryGet((string) null, out _));
            Assert.IsNull(_channels.ProfileOf(null));
        }

        /// <summary>
        /// The real FlowModule has a part in the package that declares only Default, so a project that
        /// has generated nothing yet still has a table - the framework's channels and Default.
        /// </summary>
        [Test]
        public void The_default_table_reads_FlowModule_and_always_carries_the_framework()
        {
            var channels = new FlowLogChannels();

            Assert.IsTrue(channels.TryGet("Signal", out _));
            Assert.IsTrue(channels.TryGet("Default", out FlowLogChannel byDefault));
            Assert.IsFalse(byDefault.IsFrameworkOwned);
            Assert.GreaterOrEqual(channels.All.Count, 14, "the thirteen framework channels and Default are always there");
        }
    }
}
