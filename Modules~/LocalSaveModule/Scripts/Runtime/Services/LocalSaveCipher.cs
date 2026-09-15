using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Modules.LocalSaveModule.Services
{
    /// <summary>
    /// What the save file looks like when a password is set: AES-256 in CBC mode, the key
    /// derived from the password with PBKDF2 over a salt drawn fresh for every write, the IV
    /// drawn fresh as well. Both sit in the file ahead of the ciphertext, behind a magic word
    /// that says which kind of file this is, so a reader can tell an encrypted file from a plain
    /// one without being told.
    ///
    /// What this protects against is a player opening the file in a text editor and changing a
    /// number. It is not a secret from a determined one: the password is a string in the build.
    /// </summary>
    internal class LocalSaveCipher
    {
        private const byte VERSION = 1;
        private const int SALT_LENGTH = 16;
        private const int IV_LENGTH = 16;
        private const int KEY_LENGTH = 32;
        private const int ITERATIONS = 10000;

        private static readonly byte[] Magic = Encoding.ASCII.GetBytes("FLOWSAVE");

        private static int HeaderLength => Magic.Length + 1 + SALT_LENGTH + IV_LENGTH;

        /// <summary>Whether the bytes open with the magic word an encrypted file carries.</summary>
        internal bool IsEncrypted(byte[] bytes)
        {
            if (bytes == null || bytes.Length < Magic.Length)
                return false;

            for (int index = 0; index < Magic.Length; index++)
            {
                if (bytes[index] != Magic[index])
                    return false;
            }

            return true;
        }

        internal byte[] Encrypt(string plainText, string password)
        {
            byte[] salt = Random(SALT_LENGTH);
            byte[] iv = Random(IV_LENGTH);
            byte[] plain = Encoding.UTF8.GetBytes(plainText);

            using (Aes aes = Create(password, salt, iv))
            using (ICryptoTransform encryptor = aes.CreateEncryptor())
            {
                byte[] body = encryptor.TransformFinalBlock(plain, 0, plain.Length);

                using (var stream = new MemoryStream(HeaderLength + body.Length))
                {
                    stream.Write(Magic, 0, Magic.Length);
                    stream.WriteByte(VERSION);
                    stream.Write(salt, 0, salt.Length);
                    stream.Write(iv, 0, iv.Length);
                    stream.Write(body, 0, body.Length);

                    return stream.ToArray();
                }
            }
        }

        /// <summary>
        /// The text the bytes hold, or an exception when the password is not the one they were
        /// written with - a wrong key leaves the padding wrong, and the block cipher says so.
        /// </summary>
        internal string Decrypt(byte[] bytes, string password)
        {
            if (!IsEncrypted(bytes) || bytes.Length < HeaderLength)
                throw new InvalidDataException("the file is not an encrypted save");

            if (bytes[Magic.Length] != VERSION)
                throw new InvalidDataException("the file was written by a newer save format");

            var salt = new byte[SALT_LENGTH];
            var iv = new byte[IV_LENGTH];
            Buffer.BlockCopy(bytes, Magic.Length + 1, salt, 0, SALT_LENGTH);
            Buffer.BlockCopy(bytes, Magic.Length + 1 + SALT_LENGTH, iv, 0, IV_LENGTH);

            using (Aes aes = Create(password, salt, iv))
            using (ICryptoTransform decryptor = aes.CreateDecryptor())
            {
                byte[] plain = decryptor.TransformFinalBlock(bytes, HeaderLength, bytes.Length - HeaderLength);

                return Encoding.UTF8.GetString(plain);
            }
        }

        private static Aes Create(string password, byte[] salt, byte[] iv)
        {
            Aes aes = Aes.Create();
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.IV = iv;

            using (var derive = new Rfc2898DeriveBytes(password, salt, ITERATIONS, HashAlgorithmName.SHA256))
                aes.Key = derive.GetBytes(KEY_LENGTH);

            return aes;
        }

        private static byte[] Random(int length)
        {
            var bytes = new byte[length];

            using (RandomNumberGenerator generator = RandomNumberGenerator.Create())
                generator.GetBytes(bytes);

            return bytes;
        }
    }
}
