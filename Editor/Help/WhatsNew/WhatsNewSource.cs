#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using FlowIoC.Editor.AgentRules;
using UnityEditor.PackageManager;

namespace FlowIoC.Editor.Help.WhatsNew
{
    /// <summary>
    /// The changelog FlowIoC ships, and the version of FlowIoC that is installed.
    ///
    /// The package resolves to a hashed path under Library/PackageCache for a UPM install and to
    /// Packages/FlowIoC for a submodule, so both are asked of the Package Manager rather than
    /// assumed. A missing or unreadable file reads as no releases: a tab with nothing in it is a
    /// better answer than an editor that throws while drawing help.
    /// </summary>
    internal class WhatsNewSource
    {
        internal const string FileName = "CHANGELOG.md";

        private readonly string _packageRootPath;
        private readonly string _version;

        internal WhatsNewSource()
        {
            var info = PackageInfo.FindForAssembly(typeof(WhatsNewSource).Assembly);

            _packageRootPath = info != null
                ? info.resolvedPath
                : Path.Combine(new ProjectRoot().Resolve(), "Packages", "FlowIoC");

            _version = info == null ? string.Empty : info.version;
        }

        internal WhatsNewSource(string packageRootPath, string version)
        {
            _packageRootPath = packageRootPath;
            _version = version;
        }

        /// <summary>The installed version, or empty when the package is not resolved through UPM.</summary>
        internal string Version => _version;

        internal string ChangelogPath => Path.Combine(_packageRootPath, FileName);

        internal IReadOnlyList<WhatsNewVersionEVO> Releases()
        {
            return new WhatsNewReading().Of(Text());
        }

        private string Text()
        {
            try
            {
                return File.Exists(ChangelogPath) ? File.ReadAllText(ChangelogPath) : string.Empty;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }
    }
}

#endif
