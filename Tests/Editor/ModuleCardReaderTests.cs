using FlowIoC.Editor.ModuleCards;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    public class ModuleCardReaderTests
    {
        private const string CARD =
            "# PlayerModule\n\n"
            + "## Purpose\n"
            + "Owns the player's currency and the rules that keep it valid.\n\n"
            + "## Concepts\n"
            + "currency, wallet, balance\n\n"
            + "## Decisions\n"
            + "- Currency is a double.\n\n"
            + "<!-- FLOWIOC:BEGIN version=1 hash=abcd1234 | generated -->\n"
            + "**Kind** Main\n"
            + "<!-- FLOWIOC:END -->\n";

        [Test]
        public void The_purpose_is_the_first_line_under_its_heading()
        {
            ModuleCardAuthoredEVO authored = new ModuleCardReader().Read(CARD);

            Assert.AreEqual("Owns the player's currency and the rules that keep it valid.", authored.Purpose);
        }

        [Test]
        public void The_concepts_are_the_first_line_under_their_heading()
        {
            ModuleCardAuthoredEVO authored = new ModuleCardReader().Read(CARD);

            Assert.AreEqual("currency, wallet, balance", authored.Concepts);
        }

        [Test]
        public void A_written_purpose_is_not_a_placeholder()
        {
            ModuleCardAuthoredEVO authored = new ModuleCardReader().Read(CARD);

            Assert.IsFalse(authored.PurposeIsPlaceholder);
            Assert.IsFalse(authored.ConceptsIsPlaceholder);
        }

        [Test]
        public void The_stub_reads_back_as_two_placeholders()
        {
            ModuleCardAuthoredEVO authored = new ModuleCardReader().Read(new ModuleCardStub().For("PlayerModule"));

            Assert.IsTrue(authored.PurposeIsPlaceholder);
            Assert.IsTrue(authored.ConceptsIsPlaceholder);
        }

        /// <summary>
        /// Create Module offers both lines while the author still has the answer in mind, and
        /// what it collects goes straight into the stub.
        /// </summary>
        [Test]
        public void A_stub_written_from_a_draft_carries_what_the_author_typed()
        {
            var draft = new ModuleCardDraftEVO {Purpose = "Owns the money.", Concepts = "currency, wallet"};

            ModuleCardAuthoredEVO authored = new ModuleCardReader().Read(new ModuleCardStub().For("PlayerModule", draft));

            Assert.AreEqual("Owns the money.", authored.Purpose);
            Assert.AreEqual("currency, wallet", authored.Concepts);
            Assert.IsFalse(authored.PurposeIsPlaceholder);
        }

        [Test]
        public void A_line_the_author_left_empty_keeps_its_placeholder()
        {
            var draft = new ModuleCardDraftEVO {Purpose = "Owns the money.", Concepts = "   "};

            ModuleCardAuthoredEVO authored = new ModuleCardReader().Read(new ModuleCardStub().For("PlayerModule", draft));

            Assert.IsFalse(authored.PurposeIsPlaceholder);
            Assert.IsTrue(authored.ConceptsIsPlaceholder);
        }

        [Test]
        public void A_missing_heading_reads_as_a_placeholder_rather_than_throwing()
        {
            ModuleCardAuthoredEVO authored = new ModuleCardReader().Read("# PlayerModule\n");

            Assert.IsNull(authored.Purpose);
            Assert.IsTrue(authored.PurposeIsPlaceholder);
        }

        [Test]
        public void A_heading_followed_only_by_the_generated_block_reads_as_a_placeholder()
        {
            string card = "# PlayerModule\n\n## Purpose\n\n<!-- FLOWIOC:BEGIN version=1 -->\nx\n<!-- FLOWIOC:END -->\n";

            Assert.IsTrue(new ModuleCardReader().Read(card).PurposeIsPlaceholder);
        }

        [Test]
        public void A_card_written_with_CRLF_reads_the_same_as_one_written_with_LF()
        {
            ModuleCardAuthoredEVO authored = new ModuleCardReader().Read(CARD.Replace("\n", "\r\n"));

            Assert.AreEqual("currency, wallet, balance", authored.Concepts);
        }
    }
}