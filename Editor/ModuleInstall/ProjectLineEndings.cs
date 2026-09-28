#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace FlowIoC.Editor.ModuleInstall
{
    /// <summary>
    /// Writes a copied module's text files with the line endings the game's checkout uses. The
    /// package ships LF. A project whose git has core.autocrlf on keeps CRLF in its working tree,
    /// and there every LF file an install or an update wrote made git warn "LF will be replaced by
    /// CRLF" - once per file, on every module update. Such a project gets CRLF; any other gets the
    /// bytes as shipped.
    ///
    /// The update's hashes drop every CR before an LF, so a converted file still reads as the
    /// package's own and never as a game's edit.
    ///
    /// Only files Unity leaves alone are converted. A scene, a prefab or an asset is rewritten by
    /// the Editor in its own line endings the next time it is saved, and converting it here would
    /// only make that save a diff.
    /// </summary>
    internal class ProjectLineEndings
    {
        private const byte CR = (byte) '\r';
        private const byte LF = (byte) '\n';

        private readonly HashSet<string> _textExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".cs", ".md", ".json", ".asmdef", ".asmref", ".txt", ".xml", ".uss", ".uxml", ".tss", ".meta"
        };

        private readonly Lazy<bool> _crlf;

        internal ProjectLineEndings() : this(ReadAutoCrlf)
        {
        }

        /// <summary>For a test: whether the project wants CRLF, without asking git.</summary>
        internal ProjectLineEndings(Func<bool> wantsCrlf) => _crlf = new Lazy<bool>(wantsCrlf);

        /// <summary>Converts the file in place when the project wants CRLF and the file is text.</summary>
        internal void Apply(string path)
        {
            if (!_crlf.Value || !IsText(path))
                return;

            byte[] converted = ToCrlf(File.ReadAllBytes(path));

            if (converted != null)
                File.WriteAllBytes(path, converted);
        }

        internal bool IsText(string path) => _textExtensions.Contains(Path.GetExtension(path));

        /// <summary>
        /// Every LF not already after a CR, turned into CRLF; null when there was nothing to turn,
        /// so an untouched file is not written again.
        /// </summary>
        internal byte[] ToCrlf(byte[] bytes)
        {
            int bare = 0;

            for (int i = 0; i < bytes.Length; i++)
            {
                if (bytes[i] == LF && (i == 0 || bytes[i - 1] != CR))
                    bare++;
            }

            if (bare == 0)
                return null;

            var result = new byte[bytes.Length + bare];
            int at = 0;

            for (int i = 0; i < bytes.Length; i++)
            {
                if (bytes[i] == LF && (i == 0 || bytes[i - 1] != CR))
                    result[at++] = CR;

                result[at++] = bytes[i];
            }

            return result;
        }

        /// <summary>
        /// What git says for this project, system and global settings included - Git for Windows
        /// turns autocrlf on in its system config, so the project's own .git/config alone would
        /// miss it. No git, or no answer, means the bytes stay as shipped.
        /// </summary>
        private static bool ReadAutoCrlf()
        {
            try
            {
                var start = new ProcessStartInfo("git", "config --get core.autocrlf")
                {
                    WorkingDirectory = Directory.GetCurrentDirectory(),
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (Process git = Process.Start(start))
                {
                    if (git == null)
                        return false;

                    string answer = git.StandardOutput.ReadToEnd();

                    if (!git.WaitForExit(5000))
                        return false;

                    return string.Equals(answer.Trim(), "true", StringComparison.OrdinalIgnoreCase);
                }
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}

#endif
