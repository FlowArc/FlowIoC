#if UNITY_EDITOR

using System;
using System.IO;
using Modules.LocalSaveModule.Services;

namespace Modules.LocalSaveModule.Editor
{
    /// <summary>
    /// What the panel does to the save file, without a window in the way so a test can do it
    /// too: read the facts about the file, read its text, delete it, or write it again under
    /// another password. Nothing here edits what the file says - the panel reads and resets, and
    /// a value changed from the Editor would skip the rules the Model exists to keep.
    /// </summary>
    internal class LocalSaveFileTools
    {
        private const string TEMPORARY_SUFFIX = ".tmp";

        private readonly LocalSaveFile _file;
        private readonly LocalSaveCipher _cipher = new LocalSaveCipher();

        internal LocalSaveFileTools() : this(LocalSaveService.FILE_NAME)
        {
        }

        /// <summary>Lets a test point at a file of its own instead of the developer's save.</summary>
        internal LocalSaveFileTools(string fileName)
        {
            _file = new LocalSaveFile(fileName);
        }

        internal string Path => _file.Path;

        internal string Folder => System.IO.Path.GetDirectoryName(_file.Path);

        internal bool Exists => File.Exists(_file.Path);

        internal bool HasTemporary => File.Exists(_file.Path + TEMPORARY_SUFFIX);

        internal long Length => Exists ? new FileInfo(_file.Path).Length : 0L;

        internal DateTime LastWrite => File.GetLastWriteTime(_file.Path);

        /// <summary>Whether the file opens with the magic word an encrypted save carries.</summary>
        internal bool IsEncrypted => Exists && _cipher.IsEncrypted(Header());

        /// <summary>
        /// The file as text: plain as it is, encrypted through the password. Throws for an
        /// encrypted file read with no password or the wrong one, and the panel says so.
        /// </summary>
        internal string ReadText(string password) => _file.ReadText(password) ?? string.Empty;

        /// <summary>Deletes the save and the temporary file a killed write may have left.</summary>
        internal void Reset()
        {
            if (File.Exists(_file.Path))
                File.Delete(_file.Path);

            if (HasTemporary)
                File.Delete(_file.Path + TEMPORARY_SUFFIX);
        }

        /// <summary>
        /// Writes the same contents again under another password - or none, for a plain file.
        /// This is how a developer changes the password on the Root without losing the save
        /// they were testing with.
        /// </summary>
        internal void Rewrite(string currentPassword, string newPassword)
        {
            string text = _file.ReadText(currentPassword);

            if (text == null)
                throw new FileNotFoundException("there is no save file to rewrite", _file.Path);

            _file.WriteText(text, newPassword);
        }

        private byte[] Header()
        {
            using (FileStream stream = File.OpenRead(_file.Path))
            {
                var header = new byte[8];
                int read = stream.Read(header, 0, header.Length);

                if (read == header.Length)
                    return header;

                var shorter = new byte[read];
                Buffer.BlockCopy(header, 0, shorter, 0, read);

                return shorter;
            }
        }
    }
}

#endif
