using FlowIoC.ConsoleModule;
using FlowIoC.Editor.Console;
using NUnit.Framework;
using UnityEngine;

namespace FlowIoC.Tests
{
    public class FlowLogProfileLineTests
    {
        private readonly FlowLogProfileLine _line = new FlowLogProfileLine();

        [Test]
        public void A_full_line_parses_into_every_part_of_the_profile()
        {
            FlowLogProfile profile = _line.Parse(
                "prefix=\"[Main] \" prefix-style=bold,underline prefix-colour=#39FF00 "
                + "message-style=italic message-colour=#DDDDDD "
                + "postfix=\"!\" postfix-style=bold postfix-colour=#9E2FDD");

            Assert.AreEqual("[Main] ", profile.Prefix);
            Assert.AreEqual(FlowTextStyle.Bold | FlowTextStyle.Underline, profile.PrefixStyle);
            Assert.AreEqual("39FF00", ColorUtility.ToHtmlStringRGB(profile.PrefixColor));
            Assert.AreEqual(FlowTextStyle.Italic, profile.MessageStyle);
            Assert.AreEqual("DDDDDD", ColorUtility.ToHtmlStringRGB(profile.MessageColor));
            Assert.AreEqual("!", profile.Postfix);
            Assert.AreEqual(FlowTextStyle.Bold, profile.PostfixStyle);
            Assert.AreEqual("9E2FDD", ColorUtility.ToHtmlStringRGB(profile.PostfixColor));
        }

        /// <summary>
        /// Every key is optional, both spellings of colour are read, and a key nobody knows is
        /// skipped rather than refused - a card is written by hand as often as by the window.
        /// </summary>
        [Test]
        public void Keys_are_optional_and_either_spelling_of_colour_is_read()
        {
            FlowLogProfile profile = _line.Parse("prefix=\"[Hero]\" prefix-color=#FF0000 mood=cheerful");

            Assert.AreEqual("[Hero]", profile.Prefix);
            Assert.AreEqual(FlowTextStyle.None, profile.PrefixStyle);
            Assert.AreEqual("FF0000", ColorUtility.ToHtmlStringRGB(profile.PrefixColor));
            Assert.IsNull(profile.Postfix);
            Assert.AreEqual(Color.white, profile.MessageColor);
        }

        [Test]
        public void A_line_that_decorates_nothing_is_no_profile()
        {
            Assert.IsNull(_line.Parse(null));
            Assert.IsNull(_line.Parse(""));
            Assert.IsNull(_line.Parse("prefix-style=bold"));
            Assert.IsNull(_line.Parse("nonsense"));
        }

        [Test]
        public void A_quoted_text_keeps_its_escaped_quotes_and_backslashes()
        {
            FlowLogProfile profile = _line.Parse("prefix=\"say \\\"hi\\\" \\\\ now\"");

            Assert.AreEqual("say \"hi\" \\ now", profile.Prefix);
        }

        /// <summary>
        /// What Format writes is what Parse reads back, so the window and the card agree - and
        /// what is white or None is left out, so the line says only what was chosen.
        /// </summary>
        [Test]
        public void Format_and_Parse_round_trip_and_leave_out_what_is_not_set()
        {
            var profile = new FlowLogProfile()
                .SetPrefix("[Main]", FlowTextStyle.Bold, "#39FF00")
                .SetMessageStyle(FlowTextStyle.Italic)
                .SetPostfix("!", FlowTextStyle.None, "#9E2FDD");

            string line = _line.Format(profile);

            Assert.AreEqual(
                "prefix=\"[Main]\" prefix-style=bold prefix-colour=#39FF00 message-style=italic postfix=\"!\" postfix-colour=#9E2FDD",
                line);

            FlowLogProfile back = _line.Parse(line);

            Assert.AreEqual(profile.Prefix, back.Prefix);
            Assert.AreEqual(profile.PrefixStyle, back.PrefixStyle);
            Assert.AreEqual(ColorUtility.ToHtmlStringRGB(profile.PrefixColor), ColorUtility.ToHtmlStringRGB(back.PrefixColor));
            Assert.AreEqual(profile.MessageStyle, back.MessageStyle);
            Assert.AreEqual(profile.Postfix, back.Postfix);
            Assert.AreEqual(ColorUtility.ToHtmlStringRGB(profile.PostfixColor), ColorUtility.ToHtmlStringRGB(back.PostfixColor));
        }

        [Test]
        public void A_profile_that_decorates_nothing_formats_to_no_line()
        {
            Assert.IsNull(_line.Format(null));
            Assert.IsNull(_line.Format(new FlowLogProfile()));
        }

        [Test]
        public void A_colour_with_alpha_is_written_with_eight_digits_and_read_back()
        {
            Assert.AreEqual("39FF00", FlowLogProfileLine.HexOf(new Color32(0x39, 0xFF, 0x00, 255)));
            Assert.AreEqual("39FF0080", FlowLogProfileLine.HexOf(new Color32(0x39, 0xFF, 0x00, 128)));
        }
    }
}
