#if UNITY_EDITOR
using System.Text.RegularExpressions;

namespace FlowIoC.Editor.CodeGenerator.Menus.Module.RenameModule
{
    /// <summary>
    /// One replacement of the rename pass: what to find, what to write, and the pattern that
    /// finds it as a whole word. The pattern is built once here rather than at every file, and
    /// the replacement has its dollars escaped so a name is written as it is, never read as a
    /// group reference.
    /// </summary>
    internal class TextRule
    {
        internal string Old { get; private set; }
        internal string New { get; private set; }
        internal Regex Pattern { get; private set; }
        internal string Replacement { get; private set; }

        /// <summary>A namespace, a log constant or a class name: replaced where it stands as a whole word.</summary>
        internal static TextRule Token(string old, string @new)
        {
            return new TextRule
            {
                Old = old,
                New = @new,
                Pattern = new Regex(@"\b" + Regex.Escape(old) + @"\b"),
                Replacement = Escaped(@new)
            };
        }

        /// <summary>
        /// The address a screen context declares, in either of the two shapes the generator writes -
        /// <c>ScreenLoadCVO.Addressable("Name")</c> and <c>ScreenLoadCVO.Resource("Folder/Name")</c>.
        /// Only the last segment of a resource path is the prefab's name, so only that is replaced.
        /// </summary>
        internal static TextRule ScreenAddress(string old, string @new)
        {
            return new TextRule
            {
                Old = old,
                New = @new,
                Pattern = new Regex(@"(ScreenLoadCVO\.(?:Addressable|Resource)\(""(?:[^""]*/)?)" + Regex.Escape(old) + @"(""\))"),
                Replacement = "${1}" + Escaped(@new) + "${2}"
            };
        }

        private static string Escaped(string replacement) => replacement.Replace("$", "$$");
    }
}
#endif
