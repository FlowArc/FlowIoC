#if UNITY_EDITOR
using System;

namespace FlowIoC.Editor.Help.WhatsNew
{
    /// <summary>
    /// Which of two package versions is the newer one, read the way the Package Manager reads
    /// them: numbers compared as numbers, so that 1.18.0 is newer than 1.9.0, and a pre-release
    /// tag older than the release it leads to, so that 1.18.0-preview.1 is behind 1.18.0.
    ///
    /// A string that is not a version is treated as no version at all, and no version is newer
    /// than nothing: the notice that asks stays quiet rather than reading garbage as an update.
    /// </summary>
    internal class VersionOrder
    {
        /// <summary>Whether <paramref name="candidate"/> is a later version than <paramref name="current"/>.</summary>
        internal bool IsNewer(string candidate, string current)
        {
            return Compare(candidate, current) > 0;
        }

        /// <summary>
        /// Positive when <paramref name="left"/> is later, negative when earlier, zero when they
        /// name the same version. Two unreadable strings are equal; one unreadable string is the
        /// earlier of the two.
        /// </summary>
        internal int Compare(string left, string right)
        {
            bool leftReads = TryRead(left, out int[] leftNumbers, out string leftTag);
            bool rightReads = TryRead(right, out int[] rightNumbers, out string rightTag);

            if (!leftReads || !rightReads)
                return leftReads.CompareTo(rightReads);

            for (var i = 0; i < 3; i++)
            {
                int byNumber = leftNumbers[i].CompareTo(rightNumbers[i]);

                if (byNumber != 0)
                    return byNumber;
            }

            // The same numbers: a release outranks any pre-release of itself, and two pre-releases
            // are ordered by their tags, which is what the Package Manager does with them too.
            if (leftTag.Length == 0 || rightTag.Length == 0)
                return rightTag.Length.CompareTo(leftTag.Length);

            return string.CompareOrdinal(leftTag, rightTag);
        }

        private static bool TryRead(string version, out int[] numbers, out string tag)
        {
            numbers = new int[3];
            tag = string.Empty;

            if (string.IsNullOrWhiteSpace(version))
                return false;

            string text = version.Trim();
            int dash = text.IndexOf('-');

            if (dash >= 0)
            {
                tag = text.Substring(dash + 1);
                text = text.Substring(0, dash);
            }

            string[] parts = text.Split('.');

            if (parts.Length == 0 || parts.Length > 3)
                return false;

            for (var i = 0; i < parts.Length; i++)
            {
                if (!int.TryParse(parts[i], out int number) || number < 0)
                    return false;

                numbers[i] = number;
            }

            return true;
        }
    }
}

#endif
