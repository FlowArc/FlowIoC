using FlowIoC.Editor.CodeGenerator;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    /// <summary>
    /// What Create Command writes into a context's CommandBindings. A binding says how a command
    /// runs in the name it calls it by - ToSequence or ToParallel - so nothing follows the last
    /// command, and a block that ends in the InSequence/InParallel of an older API is rewritten
    /// into what compiles today.
    /// </summary>
    public class CommandBindingBlockTests
    {
        private CommandBindingBlock _block;

        [SetUp]
        public void SetUp() => _block = new CommandBindingBlock();

        [Test]
        public void A_new_block_binds_the_signal_to_one_command()
        {
            string block = _block.Create("_signals.Incoming.AddCurrency", "AddCurrencyCommand", true);

            Assert.AreEqual(
                "            CommandBinder.Bind(_signals.Incoming.AddCurrency)\r\n"
                + "                .ToSequence<AddCurrencyCommand>();",
                block);
        }

        [Test]
        public void A_parallel_command_is_named_by_the_call_that_runs_it_that_way()
        {
            string block = _block.Create("_signals.Incoming.Tick", "TickCommand", false);

            StringAssert.Contains(".ToParallel<TickCommand>();", block);
            StringAssert.DoesNotContain(".InParallel", block);
        }

        [Test]
        public void A_second_command_is_appended_after_the_one_already_there()
        {
            string existing =
                "            CommandBinder.Bind(_signals.Incoming.AddCurrency)\r\n"
                + "                .ToSequence<AddCurrencyCommand>();";

            string block = _block.Merge(existing, "_signals.Incoming.AddCurrency", "SavePlayerCommand", true);

            Assert.AreEqual(
                "            CommandBinder.Bind(_signals.Incoming.AddCurrency)\r\n"
                + "                .ToSequence<AddCurrencyCommand>()\r\n"
                + "                .ToSequence<SavePlayerCommand>();",
                block);
        }

        [Test]
        public void A_command_the_block_already_runs_is_not_added_twice()
        {
            string existing =
                "            CommandBinder.Bind(_signals.Incoming.AddCurrency)\r\n"
                + "                .ToSequence<AddCurrencyCommand>();";

            string block = _block.Merge(existing, "_signals.Incoming.AddCurrency", "AddCurrencyCommand", true);

            Assert.AreEqual(existing, block);
        }

        /// <summary>
        /// The block belongs to another signal, so it is handed back exactly as it was - the
        /// context holds one block per signal and the others are none of this one's business.
        /// </summary>
        [Test]
        public void A_block_for_another_signal_is_left_alone()
        {
            string existing =
                "            CommandBinder.Bind(_signals.Incoming.Tick)\r\n"
                + "                .ToParallel<TickCommand>();";

            Assert.AreEqual(existing,
                _block.Merge(existing, "_signals.Incoming.AddCurrency", "AddCurrencyCommand", true));
        }

        /// <summary>
        /// Blocks written by an older generator said To&lt;T&gt; and closed with InParallel, which
        /// no longer compiles. Reading one keeps its commands and its mode, and writes them the
        /// way the binder is called now.
        /// </summary>
        [Test]
        public void A_block_from_the_old_api_is_rewritten_into_the_current_one()
        {
            string legacy =
                "CommandBinder.Bind(_signals.Incoming.AddCurrency)\r\n"
                + "    .To<AddCurrencyCommand>()\r\n"
                + "    .InParallel();";

            string block = _block.Merge(legacy, "_signals.Incoming.AddCurrency", "SavePlayerCommand", false);

            Assert.AreEqual(
                "            CommandBinder.Bind(_signals.Incoming.AddCurrency)\r\n"
                + "                .ToParallel<AddCurrencyCommand>()\r\n"
                + "                .ToParallel<SavePlayerCommand>();",
                block);
        }

        /// <summary>
        /// Create Command can be run with the command field empty; the signal is still bound, so
        /// the context says the signal exists and the commands come later.
        /// </summary>
        [Test]
        public void A_block_with_no_command_binds_the_signal_alone()
        {
            Assert.AreEqual("            CommandBinder.Bind(_signals.Incoming.Tick);",
                _block.Create("_signals.Incoming.Tick", string.Empty, true));
        }
    }
}
