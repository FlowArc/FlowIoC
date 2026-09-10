using System.IO;
using FlowIoC.Editor.CodeGenerator;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    /// <summary>
    /// What Create Command shows beside the command it writes: the binding the Context needs,
    /// spelled with the holder field that Context actually declares, ready to be pasted where the
    /// flow reads right. The window no longer writes the binding itself - where a command sits
    /// in a sequence is a decision about the flow, and a window cannot take it.
    /// </summary>
    public class CommandBindingSnippetTests
    {
        private string _folder;

        [SetUp]
        public void SetUp()
        {
            _folder = Path.Combine(Path.GetTempPath(), "FlowIoC-" + Path.GetRandomFileName());
            Directory.CreateDirectory(_folder);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_folder)) Directory.Delete(_folder, true);
        }

        private string Context(params string[] lines)
        {
            string path = Path.Combine(_folder, "PlayerContext.cs");
            File.WriteAllLines(path, lines);

            return path;
        }

        [Test]
        public void The_snippet_binds_the_incoming_signal_named_after_the_command_to_a_sequence()
        {
            Assert.AreEqual(
                "CommandBinder.Bind(_signals.Incoming.DecreaseCurrency)\n"
                + "    .ToSequence<DecreaseCurrencyCommand>();",
                new CommandBindingSnippet().For("DecreaseCurrency", "_signals"));
        }

        /// <summary>
        /// The holder field is whatever the Context called it, read off the declaration - the
        /// snippet is only worth pasting if it compiles against that Context as it stands.
        /// </summary>
        [Test]
        public void The_holder_field_is_read_off_the_context_that_declares_it()
        {
            string context = Context(
                "public class PlayerContext : Context",
                "{",
                "    private PlayerSignals _playerSignals;",
                "}");

            Assert.AreEqual("_playerSignals", new CommandBindingSnippet().HolderFieldIn(context));
        }

        /// <summary>
        /// A module declares two holders, and only the public one has an Incoming. The internal
        /// holder is skipped even when it is declared first.
        /// </summary>
        [Test]
        public void The_internal_holder_is_not_the_field_the_binding_uses()
        {
            string context = Context(
                "    private PlayerInternalSignals _internal;",
                "    private PlayerSignals _signals;");

            Assert.AreEqual("_signals", new CommandBindingSnippet().HolderFieldIn(context));
        }

        [Test]
        public void A_context_that_cannot_be_read_falls_back_to_the_conventional_name()
        {
            Assert.AreEqual("_signals", new CommandBindingSnippet().HolderFieldIn(Path.Combine(_folder, "Missing.cs")));
            Assert.AreEqual("_signals", new CommandBindingSnippet().HolderFieldIn(null));
        }
    }
}
