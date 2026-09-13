using FlowIoC.Editor.ModuleCards;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    public class ModuleCardChannelLinesTests
    {
        private const string CARD =
            "# Player\n"
            + "\n"
            + "## Purpose\n"
            + "Holds the player.\n"
            + "\n"
            + "## Concepts\n"
            + "player, currency\n"
            + "\n"
            + "Colour: #E5A50A\n"
            + "Profile: prefix=\"[Player]\" prefix-style=bold\n"
            + "\n"
            + "<!-- FLOWIOC:BEGIN version=1 hash=abcd1234 | generated -->\n"
            + "**Kind** Main\n"
            + "Colour: #000000\n"
            + "<!-- FLOWIOC:END -->\n";

        private readonly ModuleCardChannelLines _lines = new ModuleCardChannelLines();

        [Test]
        public void The_two_lines_are_read_from_above_the_block_and_not_from_inside_it()
        {
            ModuleCardChannelLinesEVO read = _lines.Read(CARD);

            Assert.AreEqual("#E5A50A", read.Colour);
            Assert.AreEqual("prefix=\"[Player]\" prefix-style=bold", read.Profile);
        }

        [Test]
        public void Either_spelling_of_colour_is_read_and_a_missing_line_is_null()
        {
            ModuleCardChannelLinesEVO read = _lines.Read("# Player\n\ncolor: #123456\n");

            Assert.AreEqual("#123456", read.Colour);
            Assert.IsNull(read.Profile);

            Assert.IsNull(_lines.Read(null).Colour);
            Assert.IsNull(_lines.Read("# Player\n").Profile);
        }

        /// <summary>
        /// The lines go directly above the block, replacing any already there, with the rest of
        /// the card - what the author wrote, and the block - left as it was.
        /// </summary>
        [Test]
        public void Writing_replaces_the_lines_above_the_block_and_touches_nothing_else()
        {
            string written = _lines.Write(CARD, "#112233", "prefix=\"[P]\"");

            Assert.AreEqual(
                "# Player\n"
                + "\n"
                + "## Purpose\n"
                + "Holds the player.\n"
                + "\n"
                + "## Concepts\n"
                + "player, currency\n"
                + "\n"
                + "Colour: #112233\n"
                + "Profile: prefix=\"[P]\"\n"
                + "\n"
                + "<!-- FLOWIOC:BEGIN version=1 hash=abcd1234 | generated -->\n"
                + "**Kind** Main\n"
                + "Colour: #000000\n"
                + "<!-- FLOWIOC:END -->\n",
                written);
        }

        /// <summary>
        /// Null drops a line: a module put back on the palette loses its Colour line rather than
        /// keeping a stale one, and a profile emptied out leaves no Profile line behind.
        /// </summary>
        [Test]
        public void Null_drops_a_line()
        {
            string written = _lines.Write(CARD, null, null);

            Assert.AreEqual(
                "# Player\n"
                + "\n"
                + "## Purpose\n"
                + "Holds the player.\n"
                + "\n"
                + "## Concepts\n"
                + "player, currency\n"
                + "\n"
                + "<!-- FLOWIOC:BEGIN version=1 hash=abcd1234 | generated -->\n"
                + "**Kind** Main\n"
                + "Colour: #000000\n"
                + "<!-- FLOWIOC:END -->\n",
                written);
        }

        [Test]
        public void A_card_without_a_block_gets_the_lines_at_its_end()
        {
            string written = _lines.Write("# Player\n\n## Purpose\nHolds the player.\n", "#112233", null);

            Assert.AreEqual("# Player\n\n## Purpose\nHolds the player.\n\nColour: #112233\n", written);
        }

        [Test]
        public void A_card_saved_with_CRLF_keeps_its_line_endings()
        {
            string written = _lines.Write("# Player\r\n\r\n## Purpose\r\nHolds the player.\r\n", "#112233", null);

            Assert.AreEqual("# Player\r\n\r\n## Purpose\r\nHolds the player.\r\n\r\nColour: #112233\r\n", written);
        }

        [Test]
        public void What_is_written_is_read_back()
        {
            string written = _lines.Write(CARD, "#112233", "prefix=\"[P]\"");
            ModuleCardChannelLinesEVO read = _lines.Read(written);

            Assert.AreEqual("#112233", read.Colour);
            Assert.AreEqual("prefix=\"[P]\"", read.Profile);
        }
    }
}
