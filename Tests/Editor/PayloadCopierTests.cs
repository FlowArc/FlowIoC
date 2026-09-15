using System.IO;
using FlowIoC.Editor.ModuleInstall;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    public class PayloadCopierTests
    {
        private const string FolderMeta =
            "fileFormatVersion: 2\nguid: 0123456789abcdef0123456789abcdef\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n";

        private const string FileMeta =
            "fileFormatVersion: 2\nguid: fedcba9876543210fedcba9876543210\nMonoImporter:\n  externalObjects: {}\n";

        private string _root;
        private string _source;
        private string _target;

        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), "FlowIoCPayloadCopier_" + Path.GetRandomFileName());
            _source = Path.Combine(_root, "source");
            _target = Path.Combine(_root, "target");
            Directory.CreateDirectory(_source);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_root))
                Directory.Delete(_root, true);
        }

        /// <summary>
        /// Git stores no empty folder, so a module's skeleton travels as the folder's meta alone.
        /// The copy puts the folder back, before Unity would with a warning.
        /// </summary>
        [Test]
        public void A_folder_meta_without_its_folder_gets_the_folder_back()
        {
            File.WriteAllText(Path.Combine(_source, "Entities.meta"), FolderMeta);

            new PayloadCopier().CopyTree(_source, _target);

            Assert.IsTrue(Directory.Exists(Path.Combine(_target, "Entities")), "the folder was not created");
            Assert.IsTrue(File.Exists(Path.Combine(_target, "Entities.meta")), "the meta was not copied");
        }

        [Test]
        public void A_files_meta_creates_no_folder()
        {
            File.WriteAllText(Path.Combine(_source, "Thing.cs"), "class Thing {}");
            File.WriteAllText(Path.Combine(_source, "Thing.cs.meta"), FileMeta);

            new PayloadCopier().CopyTree(_source, _target);

            Assert.IsFalse(Directory.Exists(Path.Combine(_target, "Thing.cs")), "a folder was created for a file's meta");
            Assert.IsTrue(File.Exists(Path.Combine(_target, "Thing.cs")));
        }

        /// <summary>A folder that does travel - it has a file in it - is copied as it always was.</summary>
        [Test]
        public void A_folder_with_files_is_copied_whole()
        {
            Directory.CreateDirectory(Path.Combine(_source, "Models"));
            File.WriteAllText(Path.Combine(_source, "Models.meta"), FolderMeta);
            File.WriteAllText(Path.Combine(_source, "Models", "Model.cs"), "class Model {}");

            new PayloadCopier().CopyTree(_source, _target);

            Assert.IsTrue(File.Exists(Path.Combine(_target, "Models", "Model.cs")));
            Assert.IsTrue(File.Exists(Path.Combine(_target, "Models.meta")));
        }
    }
}
