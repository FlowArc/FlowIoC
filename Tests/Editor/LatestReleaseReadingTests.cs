using FlowIoC.Editor.Help.WhatsNew;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    /// <summary>
    /// The one field the update notice reads out of a registry's document, and what it makes of
    /// a document that is not one.
    /// </summary>
    public class LatestReleaseReadingTests
    {
        private static string Of(string document) => new LatestReleaseReading().Of(document);

        [Test]
        public void The_latest_tag_is_the_version_read()
        {
            const string document =
                "{\"name\":\"com.flowarc.flowioc.core\",\"dist-tags\":{\"latest\":\"1.18.0\"},"
                + "\"versions\":{\"1.17.1\":{},\"1.18.0\":{}}}";

            Assert.AreEqual("1.18.0", Of(document));
        }

        [Test]
        public void A_document_without_the_tag_reads_as_no_version()
        {
            Assert.AreEqual(string.Empty, Of("{\"name\":\"com.flowarc.flowioc.core\"}"));
            Assert.AreEqual(string.Empty, Of("{\"dist-tags\":{}}"));
        }

        [Test]
        public void A_reply_that_is_not_a_document_reads_as_no_version()
        {
            Assert.AreEqual(string.Empty, Of("<html>Not Found</html>"));
            Assert.AreEqual(string.Empty, Of(string.Empty));
            Assert.AreEqual(string.Empty, Of(null));
        }
    }
}
