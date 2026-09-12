using System.Collections.Generic;
using FlowIoC.Editor.CodeGenerator.Menus.Module.RenameModule;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    /// <summary>
    /// A rename is a text pass with word boundaries: Modules.CounterModule is replaced where it is
    /// the whole token and left where it is the front of a longer one. The rules run in the order
    /// they are given, which is what lets a nested namespace go before the one it starts with.
    /// </summary>
    public class SourceTextRewriterTests
    {
        private readonly SourceTextRewriter _rewriter = new SourceTextRewriter();

        [Test]
        public void A_token_is_replaced_only_where_it_is_the_whole_word()
        {
            var rules = new List<TextRule> {TextRule.Token("Modules.CounterModule", "Modules.TimerModule")};

            string result = _rewriter.Rewrite(
                "using Modules.CounterModule.Models;\nnamespace Modules.CounterModuleX.A {}\nvar x = Modules.CounterModule;",
                rules, out bool changed);

            Assert.IsTrue(changed);
            Assert.AreEqual("using Modules.TimerModule.Models;\nnamespace Modules.CounterModuleX.A {}\nvar x = Modules.TimerModule;", result);
        }

        [Test]
        public void Rules_run_in_order_so_the_deeper_namespace_goes_first()
        {
            var rules = new List<TextRule>
            {
                TextRule.Token("Modules.CounterModule.CounterTestModule", "Modules.TimerModule.TimerTestModule"),
                TextRule.Token("Modules.CounterModule", "Modules.TimerModule")
            };

            string result = _rewriter.Rewrite(
                "using Modules.CounterModule.CounterTestModule.Signals;\nusing Modules.CounterModule.Signals;",
                rules, out _);

            Assert.AreEqual("using Modules.TimerModule.TimerTestModule.Signals;\nusing Modules.TimerModule.Signals;", result);
        }

        [Test]
        public void A_log_constant_and_an_identifier_are_tokens_too()
        {
            var rules = new List<TextRule>
            {
                TextRule.Token("FlowLogType.CounterModule", "FlowLogType.TimerModule"),
                TextRule.Token("CounterServiceSignalsIncoming", "TimerServiceSignalsIncoming"),
                TextRule.Token("CounterServiceSignals", "TimerServiceSignals")
            };

            string result = _rewriter.Rewrite(
                "FlowLogger.Log(FlowLogType.CounterModule, \"Execute - CounterServiceSignals\");\n"
                + "public CounterServiceSignalsIncoming Incoming;\nvar a = GetInstance<CounterServiceSignals>();",
                rules, out _);

            Assert.AreEqual(
                "FlowLogger.Log(FlowLogType.TimerModule, \"Execute - TimerServiceSignals\");\n"
                + "public TimerServiceSignalsIncoming Incoming;\nvar a = GetInstance<TimerServiceSignals>();",
                result);
        }

        [Test]
        public void Unchanged_text_says_so_and_comes_back_as_it_was()
        {
            string text = "namespace Modules.OtherModule.Models {}";

            string result = _rewriter.Rewrite(text, new List<TextRule> {TextRule.Token("Modules.CounterModule", "Modules.TimerModule")}, out bool changed);

            Assert.IsFalse(changed);
            Assert.AreEqual(text, result);
        }

        [Test]
        public void The_screen_address_rule_reaches_the_literal_in_both_load_shapes_and_nothing_else()
        {
            var rules = new List<TextRule> {TextRule.ScreenAddress("GameplayScreen", "HudScreen")};

            string result = _rewriter.Rewrite(
                "Load = ScreenLoadCVO.Addressable(\"GameplayScreen\"),\n"
                + "Load = ScreenLoadCVO.Resource(\"Screens/GameplayScreen\"),\n"
                + "var name = \"GameplayScreen\";",
                rules, out _);

            Assert.AreEqual(
                "Load = ScreenLoadCVO.Addressable(\"HudScreen\"),\n"
                + "Load = ScreenLoadCVO.Resource(\"Screens/HudScreen\"),\n"
                + "var name = \"GameplayScreen\";",
                result);
        }

        [Test]
        public void A_dollar_in_the_new_name_is_written_as_it_is()
        {
            string result = _rewriter.Rewrite("A.B", new List<TextRule> {TextRule.Token("A.B", "A$1.C")}, out _);

            Assert.AreEqual("A$1.C", result);
        }
    }
}
