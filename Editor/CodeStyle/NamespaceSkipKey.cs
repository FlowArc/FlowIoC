#if UNITY_EDITOR

using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace FlowIoC.Editor.CodeStyle
{
    /// <summary>
    /// The settings key under which Rider remembers that a folder is not a namespace provider.
    ///
    /// Rider spells the folder's path relative to the project, lowercased, with every character
    /// outside a-z and 0-9 written as its code point - <c>_005C</c> for the backslash, <c>_002E</c>
    /// for a dot, <c>_0040</c> for the at sign a package's cache folder carries. The lowercasing
    /// follows the machine's culture, and on a Turkish one a capital I comes down as a dotless ı,
    /// so <c>FlowIoC</c> is <c>flow_0131oc</c> there and <c>flowioc</c> everywhere else. A file
    /// read on both kinds of machine has to carry both spellings, which is why one folder answers
    /// with more than one key.
    /// </summary>
    internal class NamespaceSkipKey
    {
        internal const string Prefix = "/Default/CodeInspection/NamespaceProvider/NamespaceFoldersToSkip/=";
        internal const string Suffix = "/@EntryIndexedValue";

        private static readonly CultureInfo Turkish = new CultureInfo("tr-TR");

        /// <summary>
        /// Every key Rider might look the folder up under: the invariant spelling, the machine's
        /// own, and the Turkish one, with duplicates dropped.
        /// </summary>
        internal IReadOnlyList<string> For(string relativeFolderPath)
        {
            var keys = new List<string>();

            foreach (string encoded in Spellings(relativeFolderPath))
                keys.Add(Prefix + encoded + Suffix);

            return keys;
        }

        /// <summary>The encoded folder paths alone, for a caller matching keys rather than writing them.</summary>
        internal IReadOnlyList<string> Spellings(string relativeFolderPath)
        {
            string path = relativeFolderPath.Replace('/', '\\').Trim('\\');

            var spellings = new List<string>();

            foreach (CultureInfo culture in new[] {CultureInfo.InvariantCulture, CultureInfo.CurrentCulture, Turkish})
            {
                string encoded = Encode(path.ToLower(culture));

                if (!spellings.Contains(encoded))
                    spellings.Add(encoded);
            }

            return spellings;
        }

        private static string Encode(string loweredPath)
        {
            var builder = new StringBuilder(loweredPath.Length * 2);

            foreach (char c in loweredPath)
            {
                if (c >= 'a' && c <= 'z' || c >= '0' && c <= '9')
                    builder.Append(c);
                else
                    builder.Append('_').Append(((int) c).ToString("X4"));
            }

            return builder.ToString();
        }
    }
}

#endif
