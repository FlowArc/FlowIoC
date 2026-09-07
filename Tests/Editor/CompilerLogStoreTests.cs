using System.Collections.Generic;
using FlowIoC.Editor.Console;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    public class CompilerLogStoreTests
    {
        private readonly CompilerLogStore _store = new CompilerLogStore();

        [Test]
        public void A_message_survives_the_round_trip()
        {
            var written = new List<CompilerLogRecord>
            {
                new CompilerLogRecord
                {
                    Message = "Assets/A.cs(12,5): error CS0103: The name 'x' does not exist",
                    File = "Assets/A.cs",
                    Line = 12,
                    IsError = true
                }
            };

            var read = _store.Deserialize(_store.Serialize(written));

            Assert.AreEqual(1, read.Count);
            Assert.AreEqual(written[0].Message, read[0].Message);
            Assert.AreEqual("Assets/A.cs", read[0].File);
            Assert.AreEqual(12, read[0].Line);
            Assert.IsTrue(read[0].IsError);
        }

        [Test]
        public void Several_messages_keep_their_order()
        {
            var written = new List<CompilerLogRecord>
            {
                new CompilerLogRecord {Message = "first", File = "A.cs", Line = 1, IsError = true},
                new CompilerLogRecord {Message = "second", File = "B.cs", Line = 2, IsError = false}
            };

            var read = _store.Deserialize(_store.Serialize(written));

            Assert.AreEqual("first", read[0].Message);
            Assert.AreEqual("second", read[1].Message);
        }

        [Test]
        public void Nothing_stored_reads_back_as_nothing()
        {
            Assert.AreEqual(0, _store.Deserialize(null).Count);
            Assert.AreEqual(0, _store.Deserialize("").Count);
        }

        /// <summary>
        /// SessionState is shared with everything else in the editor, so the string can come
        /// back as something this store did not write. An unreadable value is no compiler
        /// messages, never an exception during a domain reload.
        /// </summary>
        [Test]
        public void An_unreadable_value_reads_back_as_nothing()
        {
            Assert.AreEqual(0, _store.Deserialize("not json at all").Count);
        }
    }
}
