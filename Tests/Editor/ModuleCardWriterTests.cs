using FlowIoC.Editor.AgentRules;
using FlowIoC.Editor.ModuleCards;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    public class ModuleCardWriterTests
    {
        private const string BODY = "**Kind** Main";

        private static string Stub() => new ModuleCardStub().For("PlayerModule");

        [Test]
        public void A_card_with_no_block_gains_one()
        {
            BlockWriteResult result = new ModuleCardWriter().Write(Stub(), BODY);

            Assert.AreEqual(BlockWriteStatus.Created, result.Status);
            StringAssert.Contains("<!-- FLOWIOC:BEGIN version=1", result.Text);
            StringAssert.Contains(BODY, result.Text);
        }

        [Test]
        public void The_authored_half_survives_the_write()
        {
            BlockWriteResult result = new ModuleCardWriter().Write(Stub(), BODY);

            StringAssert.Contains("## Purpose", result.Text);
            StringAssert.Contains(ModuleCardStub.PURPOSE_PLACEHOLDER, result.Text);
        }

        [Test]
        public void Writing_the_same_body_twice_changes_nothing()
        {
            var writer = new ModuleCardWriter();

            string once = writer.Write(Stub(), BODY).Text;

            Assert.AreEqual(BlockWriteStatus.Unchanged, writer.Write(once, BODY).Status);
        }

        [Test]
        public void A_changed_body_updates_the_block()
        {
            var writer = new ModuleCardWriter();

            string once = writer.Write(Stub(), BODY).Text;
            BlockWriteResult again = writer.Write(once, "**Kind** Screen");

            Assert.AreEqual(BlockWriteStatus.Updated, again.Status);
            StringAssert.Contains("**Kind** Screen", again.Text);
            StringAssert.DoesNotContain("**Kind** Main", again.Text);
        }

        [Test]
        public void A_card_whose_markers_are_broken_is_refused_rather_than_rewritten()
        {
            string broken = Stub() + "\n<!-- FLOWIOC:END -->\n";

            BlockWriteResult result = new ModuleCardWriter().Write(broken, BODY);

            Assert.AreEqual(BlockWriteStatus.Refused, result.Status);
            StringAssert.Contains("MODULE.md", result.Message);
        }

        [Test]
        public void A_card_with_no_block_is_stale()
        {
            Assert.IsTrue(new ModuleCardWriter().IsStale(Stub(), BODY));
        }

        [Test]
        public void A_card_carrying_the_current_body_is_not_stale()
        {
            var writer = new ModuleCardWriter();

            Assert.IsFalse(writer.IsStale(writer.Write(Stub(), BODY).Text, BODY));
        }

        /// <summary>
        /// Editing inside the block leaves the hash line describing what the block was rendered
        /// from, so a check that compared hashes would call the card current. Staleness is asked
        /// of the writer instead, which compares the block itself.
        /// </summary>
        [Test]
        public void A_block_a_reader_edited_by_hand_is_stale_although_its_hash_is_untouched()
        {
            var writer = new ModuleCardWriter();

            string tampered = writer.Write(Stub(), BODY).Text.Replace(BODY, "**Kind** Whatever");

            Assert.IsTrue(writer.IsStale(tampered, BODY));
        }

        [Test]
        public void The_same_body_hashes_the_same_in_a_CRLF_card_and_an_LF_card()
        {
            var writer = new ModuleCardWriter();

            string lf = writer.Write(Stub(), BODY).Text;
            string crlf = writer.Write(Stub().Replace("\n", "\r\n"), BODY).Text;

            Assert.IsFalse(writer.IsStale(crlf, BODY));
            Assert.IsFalse(writer.IsStale(lf, BODY));
        }
    }
}