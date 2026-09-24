#if UNITY_EDITOR

using System;
using System.IO;
using UnityEngine;

namespace FlowIoC.Editor.ModuleInstall
{
    /// <summary>
    /// A path the Editor's file calls can reach past Windows' 260 characters. A consumer reads a
    /// module out of <c>Library/PackageCache/com.flowarc.flowioc.core@&lt;hash&gt;/Modules~/</c>, and
    /// with an ordinary project folder in front that is well over a hundred characters before the
    /// module's own path starts - a screen's test module sits past 260 on its own. The Editor's
    /// runtime reports such a file as missing even though it is on disk; the same call reaches it
    /// once the path carries the <c>\\?\</c> prefix. Anywhere else the path comes back unchanged.
    /// </summary>
    internal class LongPath
    {
        private const string PREFIX = @"\\?\";
        private const string UNC_PREFIX = @"\\?\UNC\";
        private const string UNC_START = @"\\";

        private readonly bool _windows;

        internal LongPath() : this(Application.platform == RuntimePlatform.WindowsEditor)
        {
        }

        internal LongPath(bool windows) => _windows = windows;

        /// <summary>
        /// The absolute path with the prefix in front, backslashes throughout and no trailing
        /// separator - the form every name Directory.GetFiles hands back under it shares, so a
        /// relative path is still the tail past the root's length.
        /// </summary>
        internal string Of(string path)
        {
            if (!_windows || string.IsNullOrEmpty(path) || path.StartsWith(PREFIX, StringComparison.Ordinal))
                return path;

            string full = Path.GetFullPath(path).Replace('/', '\\');

            // A drive root keeps its separator: "\\?\D:" alone names no directory.
            if (full.Length > 3)
                full = full.TrimEnd('\\');

            return full.StartsWith(UNC_START, StringComparison.Ordinal)
                ? UNC_PREFIX + full.Substring(UNC_START.Length)
                : PREFIX + full;
        }

        /// <summary>The path without the prefix, for anything that shows it or hands it to Unity's own APIs.</summary>
        internal string Strip(string path)
        {
            if (string.IsNullOrEmpty(path))
                return path;

            if (path.StartsWith(UNC_PREFIX, StringComparison.Ordinal))
                return UNC_START + path.Substring(UNC_PREFIX.Length);

            return path.StartsWith(PREFIX, StringComparison.Ordinal) ? path.Substring(PREFIX.Length) : path;
        }
    }
}

#endif
