using System.IO;
using System.Text;
using FlowIoC.Editor.CodeGenerator.Menus.Module.RenameModule;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    /// <summary>A source file rewritten by the rename keeps its byte order mark - or its lack of one - and its line endings.</summary>
    public class TextFileTests
    {
        private string _path;

        [SetUp]
        public void Fresh() => _path = Path.Combine(Path.GetTempPath(), "flowioc-textfile-" + Path.GetRandomFileName() + ".cs");

        [TearDown]
        public void Gone()
        {
            if (File.Exists(_path)) File.Delete(_path);
        }

        [Test]
        public void A_file_with_a_bom_keeps_it_and_one_without_stays_without()
        {
            File.WriteAllText(_path, "a\r\nb", new UTF8Encoding(true));
            Assert.IsTrue(new TextFile().Rewrite(_path, text => text.Replace("a", "x")));
            byte[] withBom = File.ReadAllBytes(_path);
            Assert.AreEqual(0xEF, withBom[0]);
            Assert.AreEqual("x\r\nb", File.ReadAllText(_path));

            File.WriteAllText(_path, "a\nb", new UTF8Encoding(false));
            Assert.IsTrue(new TextFile().Rewrite(_path, text => text.Replace("a", "y")));
            byte[] withoutBom = File.ReadAllBytes(_path);
            Assert.AreEqual((byte) 'y', withoutBom[0]);
            Assert.AreEqual("y\nb", File.ReadAllText(_path));
        }

        [Test]
        public void An_unchanged_file_is_not_written()
        {
            File.WriteAllText(_path, "same");
            System.DateTime before = File.GetLastWriteTimeUtc(_path);

            Assert.IsFalse(new TextFile().Rewrite(_path, text => text));

            Assert.AreEqual(before, File.GetLastWriteTimeUtc(_path));
        }
    }
}
