using System;
using System.Text.RegularExpressions;
using FlowIoC.Editor.AgentRules;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    public class ManagedBlockWriterTests
    {
        private const string Body = "Rule one.\nRule two.";
        private const string Version = "1.0.1";
        private const string EndMarker = "<!-- FLOWIOC:END -->";

        [Test]
        public void Write_creates_the_block_when_the_file_is_empty()
        {
            var result = new ManagedBlockWriter().Write(string.Empty, Body, Version);

            Assert.AreEqual(BlockWriteStatus.Created, result.Status);
            StringAssert.Contains("FLOWIOC:BEGIN", result.Text);
            StringAssert.Contains("FLOWIOC:END", result.Text);
            StringAssert.Contains("Rule one.", result.Text);
        }

        [Test]
        public void Write_appends_the_block_and_keeps_existing_content()
        {
            const string existing = "# My rules\n\nAlways run the linter.\n";

            var result = new ManagedBlockWriter().Write(existing, Body, Version);

            Assert.AreEqual(BlockWriteStatus.Created, result.Status);
            StringAssert.StartsWith(existing, result.Text);
            StringAssert.Contains("Rule one.", result.Text);
        }

        [Test]
        public void Write_replaces_only_the_block_and_leaves_the_surroundings_intact()
        {
            var writer = new ManagedBlockWriter();
            const string before = "# Before\n\n";
            const string after = "\n# After\n";
            string seeded = before + ExtractBlock(writer.Write(string.Empty, "OLD BODY", Version).Text) + after;

            var result = writer.Write(seeded, Body, Version);

            Assert.AreEqual(BlockWriteStatus.Updated, result.Status);
            StringAssert.StartsWith(before, result.Text);
            StringAssert.EndsWith(after, result.Text);
            StringAssert.DoesNotContain("OLD BODY", result.Text);
            StringAssert.Contains("Rule one.", result.Text);
        }

        [Test]
        public void Write_is_idempotent()
        {
            var writer = new ManagedBlockWriter();
            string first = writer.Write("# Mine\n", Body, Version).Text;

            var second = writer.Write(first, Body, Version);

            Assert.AreEqual(BlockWriteStatus.Unchanged, second.Status);
            Assert.AreEqual(first, second.Text);
        }

        [Test]
        public void Write_refuses_a_begin_marker_without_an_end_marker()
        {
            const string broken = "<!-- FLOWIOC:BEGIN version=1.0.0 hash=deadbeef -->\nstuff\n";

            var result = new ManagedBlockWriter().Write(broken, Body, Version);

            Assert.AreEqual(BlockWriteStatus.Refused, result.Status);
            Assert.AreEqual(broken, result.Text);
            Assert.IsNotEmpty(result.Message);
        }

        [Test]
        public void Write_refuses_an_end_marker_that_precedes_its_begin_marker()
        {
            const string broken = EndMarker + "\n<!-- FLOWIOC:BEGIN version=1.0.0 hash=deadbeef -->\n";

            var result = new ManagedBlockWriter().Write(broken, Body, Version);

            Assert.AreEqual(BlockWriteStatus.Refused, result.Status);
            Assert.AreEqual(broken, result.Text);
        }

        [Test]
        public void Write_refuses_two_begin_markers_as_ambiguous()
        {
            var writer = new ManagedBlockWriter();
            string one = writer.Write(string.Empty, Body, Version).Text;
            string two = one + one;

            var result = writer.Write(two, Body, Version);

            Assert.AreEqual(BlockWriteStatus.Refused, result.Status);
            Assert.AreEqual(two, result.Text);
        }

        [Test]
        public void Write_preserves_crlf_line_endings()
        {
            const string existing = "# Mine\r\n\r\nA rule.\r\n";

            string text = new ManagedBlockWriter().Write(existing, Body, Version).Text;

            Assert.IsFalse(Regex.IsMatch(text, "(?<!\r)\n"),
                "A lone LF was written into a CRLF file.");
        }

        [Test]
        public void Write_uses_lf_for_a_file_it_creates()
        {
            string text = new ManagedBlockWriter().Write(string.Empty, Body, Version).Text;

            Assert.IsFalse(text.Contains("\r\n"));
        }

        [Test]
        public void Remove_deletes_the_block_and_keeps_the_rest()
        {
            var writer = new ManagedBlockWriter();
            const string before = "# Before\n";
            string seeded = writer.Write(before, Body, Version).Text;

            string result = writer.Remove(seeded);

            StringAssert.DoesNotContain("FLOWIOC:BEGIN", result);
            StringAssert.DoesNotContain("Rule one.", result);
            StringAssert.Contains("# Before", result);
        }

        [Test]
        public void Remove_leaves_a_file_without_a_block_untouched()
        {
            const string existing = "# Mine\n";

            Assert.AreEqual(existing, new ManagedBlockWriter().Remove(existing));
        }

        [Test]
        public void ReadHash_returns_the_hash_from_the_marker()
        {
            var writer = new ManagedBlockWriter();
            string text = writer.Write(string.Empty, Body, Version).Text;

            Assert.AreEqual(writer.ComputeHash(Body), writer.ReadHash(text));
        }

        [Test]
        public void ReadHash_returns_null_when_there_is_no_block()
        {
            Assert.IsNull(new ManagedBlockWriter().ReadHash("# Mine\n"));
        }

        [Test]
        public void ComputeHash_is_stable_and_body_sensitive()
        {
            var writer = new ManagedBlockWriter();

            Assert.AreEqual(writer.ComputeHash(Body), writer.ComputeHash(Body));
            Assert.AreNotEqual(writer.ComputeHash(Body), writer.ComputeHash(Body + "!"));
            Assert.AreEqual(8, writer.ComputeHash(Body).Length);
        }

        private static string ExtractBlock(string text)
        {
            int begin = text.IndexOf("<!-- FLOWIOC:BEGIN", StringComparison.Ordinal);
            int end = text.IndexOf(EndMarker, StringComparison.Ordinal);
            return text.Substring(begin, end - begin + EndMarker.Length);
        }
    }
}
