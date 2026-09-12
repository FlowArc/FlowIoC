#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;

namespace FlowIoC.Editor.CodeGenerator.Menus.Module.RenameModule
{
    /// <summary>
    /// Rewrites one text file in place and keeps what a plain ReadAllText/WriteAllText would
    /// quietly change: the byte order mark. Unity writes its templates with one and most editors
    /// write without, and a rename that flipped every file it touched would show up as a diff on
    /// lines it never changed. Line endings survive on their own - the rules never touch them.
    /// </summary>
    internal class TextFile
    {
        /// <summary>Whether the file was written - it is not when <paramref name="rewrite"/> hands the text back as it was.</summary>
        internal bool Rewrite(string path, Func<string, string> rewrite)
        {
            byte[] bytes = File.ReadAllBytes(path);
            bool bom = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;

            string text = File.ReadAllText(path);
            string updated = rewrite(text);

            if (updated == text) return false;

            File.WriteAllText(path, updated, new UTF8Encoding(bom));

            return true;
        }
    }
}
#endif
