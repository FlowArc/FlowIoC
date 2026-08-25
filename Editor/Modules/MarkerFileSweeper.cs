#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace FlowIoC.Editor.Modules
{
    /// <summary>
    /// Deletes the marker files FlowIoC used to scatter through module folders. The pattern is
    /// deliberately narrow — this deletes files, and a project's own notes.txt must survive a
    /// sweep — so it matches only a leading underscore, a non-empty word, and the exact
    /// "_info.txt" tail.
    /// </summary>
    internal class MarkerFileSweeper
    {
        private static readonly Regex MarkerPattern =
            new Regex(@"^_[a-z]+_info\.txt$", RegexOptions.CultureInvariant);

        public bool IsMarkerFile(string fileName)
        {
            return !string.IsNullOrEmpty(fileName) && MarkerPattern.IsMatch(fileName);
        }

        public List<string> Sweep(string rootAbsolutePath)
        {
            var deleted = new List<string>();
            if (string.IsNullOrEmpty(rootAbsolutePath) || !Directory.Exists(rootAbsolutePath))
                return deleted;

            foreach (string file in Directory.GetFiles(rootAbsolutePath, "*.txt", SearchOption.AllDirectories))
            {
                if (!IsMarkerFile(Path.GetFileName(file))) continue;

                // The meta is only deleted once the marker itself is confirmed gone. A locked
                // marker keeps its meta rather than losing it, so the pair stays together and
                // the next sweep can retry both.
                if (Delete(file, deleted))
                    Delete(file + ".meta", deleted);
            }

            return deleted;
        }

        private bool Delete(string path, List<string> deleted)
        {
            if (!File.Exists(path)) return false;

            try
            {
                File.Delete(path);
                deleted.Add(path);
                return true;
            }
            catch (IOException)
            {
                // Left behind rather than failing the sweep; the next sweep retries it.
                return false;
            }
        }
    }
}

#endif
