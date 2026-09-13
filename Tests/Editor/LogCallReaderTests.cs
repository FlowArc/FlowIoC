using System.Collections.Generic;
using System.Linq;
using FlowIoC.Editor.ModuleScanner;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    public class LogCallReaderTests
    {
        private readonly LogCallReader _reader = new LogCallReader();

        private static string[] ArgumentsOf(LogCallEVO call) => call.Arguments.Select(argument => argument.Text).ToArray();

        [Test]
        public void A_plain_call_is_read_with_its_method_and_arguments()
        {
            IReadOnlyList<LogCallEVO> calls = _reader.Read("FlowLogger.LogError(FlowModule.PlayerModule, \"Currency went negative.\");");

            Assert.AreEqual(1, calls.Count);
            Assert.AreEqual("LogError", calls[0].Method);
            CollectionAssert.AreEqual(new[] {"FlowModule.PlayerModule", "\"Currency went negative.\""}, ArgumentsOf(calls[0]));
        }

        [Test]
        public void The_methods_are_told_apart_and_other_members_are_not_calls()
        {
            IReadOnlyList<LogCallEVO> calls = _reader.Read(
                "FlowLogger.Log(\"a\"); FlowLogger.LogWarning(\"b\"); FlowLogger.LogLong(\"c\"); "
                + "FlowLogger.ClearLogs(); FlowLogger.Logs.Clear(); MyFlowLogger.Log(\"d\");");

            CollectionAssert.AreEqual(new[] {"Log", "LogWarning", "LogLong"}, calls.Select(call => call.Method).ToArray());
        }

        /// <summary>
        /// The commas and the closing parenthesis inside a message are the message's own. An
        /// interpolation hole may hold a string of its own and a nested call; a verbatim string
        /// doubles its quotes.
        /// </summary>
        [Test]
        public void Commas_quotes_and_brackets_inside_a_message_do_not_split_it()
        {
            IReadOnlyList<LogCallEVO> calls = _reader.Read(
                "FlowLogger.Log(FlowModule.ShopModule, $\"Bought '{item}' for {(cheap ? \"1, 2\" : Price(\")\"))}\", ctx);\n"
                + "FlowLogger.Log(@\"say \"\"hi, there)\"\" \", \"x\");");

            Assert.AreEqual(2, calls.Count);
            CollectionAssert.AreEqual(
                new[] {"FlowModule.ShopModule", "$\"Bought '{item}' for {(cheap ? \"1, 2\" : Price(\")\"))}\"", "ctx"},
                ArgumentsOf(calls[0]));
            CollectionAssert.AreEqual(new[] {"@\"say \"\"hi, there)\"\" \"", "\"x\""}, ArgumentsOf(calls[1]));
        }

        [Test]
        public void A_call_inside_a_comment_or_a_string_is_not_a_call()
        {
            IReadOnlyList<LogCallEVO> calls = _reader.Read(
                "// FlowLogger.Log(\"a\", \"b\");\n"
                + "/* FlowLogger.Log(\"c\", \"d\"); */\n"
                + "var s = \"FlowLogger.Log(\\\"e\\\", \\\"f\\\")\";\n"
                + "FlowLogger.Log(\"real\");");

            Assert.AreEqual(1, calls.Count);
            CollectionAssert.AreEqual(new[] {"\"real\""}, ArgumentsOf(calls[0]));
        }

        [Test]
        public void A_call_spread_over_lines_keeps_the_line_it_starts_on_and_its_spans()
        {
            const string text = "class A\n{\n    void M()\n    {\n        FlowLogger.LogError(FlowModule.MainScreenModule,\n            \"OpenMainScreenCommand - the screen did not open.\");\n    }\n}";

            IReadOnlyList<LogCallEVO> calls = _reader.Read(text);

            Assert.AreEqual(1, calls.Count);
            Assert.AreEqual(5, calls[0].Line);
            Assert.AreEqual("FlowLogger", text.Substring(calls[0].Start, "FlowLogger".Length));
            Assert.AreEqual(')', text[calls[0].End - 1]);
            Assert.AreEqual("FlowModule.MainScreenModule", text.Substring(calls[0].Arguments[0].Start, calls[0].Arguments[0].End - calls[0].Arguments[0].Start));
        }

        [Test]
        public void A_call_with_no_arguments_has_none_and_an_unfinished_one_is_skipped()
        {
            Assert.AreEqual(0, _reader.Read("FlowLogger.Log();")[0].Arguments.Count);
            Assert.AreEqual(0, _reader.Read("FlowLogger.Log(\"never closed\";").Count);
        }

        [Test]
        public void A_character_literal_holding_a_quote_does_not_open_a_string()
        {
            IReadOnlyList<LogCallEVO> calls = _reader.Read("char q = '\"'; FlowLogger.Log(\"after\", \"it\");");

            Assert.AreEqual(1, calls.Count);
            CollectionAssert.AreEqual(new[] {"\"after\"", "\"it\""}, ArgumentsOf(calls[0]));
        }
    }
}
