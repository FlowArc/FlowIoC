using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace Modules.LocalSaveModule.Services
{
    /// <summary>
    /// The save file on disk, under the persistent data path. It holds text and nothing else -
    /// what the text means is the service's business - and it writes the way a save has to be
    /// written: to a temporary file first, moved over the old one only when it is whole, so a
    /// player killed mid-write is left with the previous save rather than half of a new one.
    ///
    /// With a password the bytes go through <see cref="LocalSaveCipher"/>. Reading decides by
    /// the file, not by the password: a plain file is read plain whatever the password says,
    /// which is what lets a game turn encryption on later and keep every save it already has -
    /// the next write is the encrypted one.
    /// </summary>
    internal class LocalSaveFile
    {
        private const string TEMPORARY_SUFFIX = ".tmp";

        private readonly LocalSaveCipher _cipher = new LocalSaveCipher();

        internal LocalSaveFile(string fileName)
        {
            Path = System.IO.Path.Combine(Application.persistentDataPath, fileName);
        }

        internal string Path { get; }

        /// <summary>
        /// The text the file holds, or null when there is no file - which is what a first run
        /// looks like, and not a failure. An encrypted file read with no password, or the wrong
        /// one, throws: the caller decides what an unreadable save means.
        /// </summary>
        internal string ReadText(string password)
        {
            if (!File.Exists(Path))
                return null;

            byte[] bytes = File.ReadAllBytes(Path);

            if (!_cipher.IsEncrypted(bytes))
                return Encoding.UTF8.GetString(bytes);

            if (string.IsNullOrEmpty(password))
                throw new InvalidOperationException("the save file is encrypted and no password is set");

            return _cipher.Decrypt(bytes, password);
        }

        internal void WriteText(string text, string password)
        {
            byte[] bytes = string.IsNullOrEmpty(password)
                ? Encoding.UTF8.GetBytes(text)
                : _cipher.Encrypt(text, password);

            string temporary = Path + TEMPORARY_SUFFIX;

            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path));
            File.WriteAllBytes(temporary, bytes);

            if (File.Exists(Path))
                File.Delete(Path);

            File.Move(temporary, Path);
        }
    }
}
