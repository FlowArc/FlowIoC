using FlowIoC.Editor.CodeGenerator.Menus.Module;
using FlowIoC.Editor.Config.ModuleConfig;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    /// <summary>
    /// A module has a Signals folder under Scripts and another under Scripts/Runtime, and read as
    /// names they are indistinguishable. They share the name on purpose - the namespace segment is
    /// the folder name, and sharing it is what lets one using reach both holders - so the answer is
    /// not to rename either one but to say on the row which is which.
    /// </summary>
    public class FolderPreviewHintsTests
    {
        private readonly FolderPreviewHints _hints = new FolderPreviewHints();

        private static FolderEVO Folder(FolderEVO.FolderType type) => new FolderEVO {Type = type};

        [Test]
        public void The_public_Signals_folder_says_it_is_the_public_surface()
        {
            StringAssert.Contains("public", _hints.For(Folder(FolderEVO.FolderType.PublicSignals)));
        }

        [Test]
        public void The_Runtime_Signals_folder_says_it_is_the_modules_own_traffic()
        {
            StringAssert.Contains("itself", _hints.For(Folder(FolderEVO.FolderType.Signals)));
        }

        [Test]
        public void The_two_do_not_read_the_same()
        {
            Assert.AreNotEqual(
                _hints.For(Folder(FolderEVO.FolderType.PublicSignals)),
                _hints.For(Folder(FolderEVO.FolderType.Signals)));
        }

        /// <summary>
        /// A row that explains itself needs no hint, and a preview where every line carries prose
        /// is a preview nobody reads.
        /// </summary>
        [TestCase(FolderEVO.FolderType.Shared)]
        [TestCase(FolderEVO.FolderType.Models)]
        [TestCase(FolderEVO.FolderType.Editor)]
        [TestCase(FolderEVO.FolderType.Folder)]
        public void A_folder_whose_name_is_answer_enough_gets_none(FolderEVO.FolderType type)
        {
            Assert.IsNull(_hints.For(Folder(type)));
        }

        [Test]
        public void No_folder_at_all_gets_none()
        {
            Assert.IsNull(_hints.For(null));
        }
    }
}
