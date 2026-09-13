using FlowIoC.ConsoleModule;
using FlowIoC.Editor.Console;
using FlowIoC.Editor.ModuleCards;
using NUnit.Framework;
using UnityEngine;

namespace FlowIoC.Tests
{
    /// <summary>
    /// What a module's generated part says. The table reads the part by reflection, so the shape
    /// pinned here - the const, the Color field named after it, the Profile field - is a contract
    /// between the generator and <see cref="FlowLogChannels"/>.
    /// </summary>
    public class FlowLogTypeGeneratorPartTests
    {
        [Test]
        public void A_part_declares_the_channel_and_its_colour_in_bytes()
        {
            string content = FlowLogTypeGenerator.GeneratePartContent(new ChannelPartEVO
            {
                Name = "PlayerModule",
                Color = new Color32(229, 165, 10, 255)
            });

            StringAssert.Contains("using UnityEngine;", content);
            StringAssert.Contains("public static partial class FlowLogType", content);
            StringAssert.Contains("public const string PlayerModule = \"PlayerModule\";", content);
            StringAssert.Contains("public static readonly Color PlayerModuleColor = new Color32(229, 165, 10, 255);", content);
            StringAssert.DoesNotContain("PlayerModuleProfile", content);
            StringAssert.Contains("Colour: #RRGGBB", content);
        }

        [Test]
        public void A_profile_is_written_as_the_fluent_calls_that_rebuild_it()
        {
            string content = FlowLogTypeGenerator.GeneratePartContent(new ChannelPartEVO
            {
                Name = "PlayerModule",
                Color = new Color32(229, 165, 10, 255),
                Profile = new FlowLogProfile()
                    .SetPrefix("[Player]", FlowTextStyle.Bold | FlowTextStyle.Underline, "#39FF00")
                    .SetMessageStyle(FlowTextStyle.Italic)
                    .SetMessageColor("#DDDDDD")
                    .SetPostfix("!", FlowTextStyle.None, "#9E2FDD")
            });

            StringAssert.Contains("public static readonly FlowLogProfile PlayerModuleProfile = new FlowLogProfile()", content);
            StringAssert.Contains(".SetPrefix(\"[Player]\", FlowTextStyle.Bold | FlowTextStyle.Underline, \"#39FF00\")", content);
            StringAssert.Contains(".SetMessageStyle(FlowTextStyle.Italic)", content);
            StringAssert.Contains(".SetMessageColor(\"#DDDDDD\")", content);
            StringAssert.Contains(".SetPostfix(\"!\", FlowTextStyle.None, \"#9E2FDD\");", content);
        }

        [Test]
        public void A_prefix_with_a_quote_in_it_is_escaped()
        {
            string content = FlowLogTypeGenerator.GeneratePartContent(new ChannelPartEVO
            {
                Name = "PlayerModule",
                Color = new Color32(1, 2, 3, 255),
                Profile = new FlowLogProfile().SetPrefix("say \"hi\"", FlowTextStyle.None, "#FFFFFF")
            });

            StringAssert.Contains(".SetPrefix(\"say \\\"hi\\\"\", FlowTextStyle.None, \"#FFFFFF\");", content);
        }

        /// <summary>
        /// The same card writes the same file, byte for byte, or every load would rewrite every
        /// part and recompile the project for nothing.
        /// </summary>
        [Test]
        public void The_same_channel_writes_the_same_part()
        {
            var channel = new ChannelPartEVO {Name = "PlayerModule", Color = new Color32(229, 165, 10, 255)};

            Assert.AreEqual(
                FlowLogTypeGenerator.GeneratePartContent(channel),
                FlowLogTypeGenerator.GeneratePartContent(channel));
        }

        /// <summary>
        /// The card decides the colour when it says one; the palette decides when it does not.
        /// A card line the reader cannot parse is treated as silence rather than as black.
        /// </summary>
        [Test]
        public void The_card_overrides_the_palette_and_a_bad_colour_falls_back_to_it()
        {
            var reader = new ChannelPartReader();
            Color32 pick = new FlowChannelPalette().Pick("PlayerModule");

            ChannelPartEVO fromCard = reader.From("PlayerModule", new ModuleCardChannelLinesEVO {Colour = "#E5A50A"});
            ChannelPartEVO fromPalette = reader.From("PlayerModule", new ModuleCardChannelLinesEVO());
            ChannelPartEVO fromNonsense = reader.From("PlayerModule", new ModuleCardChannelLinesEVO {Colour = "amber"});

            Assert.AreEqual((Color) new Color32(0xE5, 0xA5, 0x0A, 255), (Color) fromCard.Color);
            Assert.AreEqual((Color) pick, (Color) fromPalette.Color);
            Assert.AreEqual((Color) pick, (Color) fromNonsense.Color);
            Assert.IsNull(fromPalette.Profile);
        }

        [Test]
        public void The_cards_profile_line_becomes_the_parts_profile()
        {
            ChannelPartEVO part = new ChannelPartReader().From("PlayerModule",
                new ModuleCardChannelLinesEVO {Profile = "prefix=\"[Player]\" prefix-style=bold"});

            Assert.AreEqual("[Player]", part.Profile.Prefix);
            Assert.AreEqual(FlowTextStyle.Bold, part.Profile.PrefixStyle);
        }
    }
}
