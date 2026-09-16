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

        /// <summary>The name the package is published under, for when the Package Manager has none to give.</summary>
        internal const string PACKAGE_NAME = "com.flowarc.flowioc.core";

        private readonly string _packageRootPath;
        private readonly string _version;
        private readonly PackageSource _source;
        private readonly string _registryUrl;
        private readonly string _packageName;

        internal WhatsNewSource()
        {
            var info = PackageInfo.FindForAssembly(typeof(WhatsNewSource).Assembly);

            _packageRootPath = info != null
                ? info.resolvedPath
                : Path.Combine(new ProjectRoot().Resolve(), "Packages", "FlowIoC");

            _version = info == null ? string.Empty : info.version;
            _source = info == null ? PackageSource.Unknown : info.source;
            // The Package Manager names a registry for every package, Unity's own for one that
            // never came from a registry at all - an embedded copy reads packages.unity.com. Only
            // a package that was actually installed from a registry has one worth asking.
            _registryUrl = info != null && info.source == PackageSource.Registry && info.registry != null
                ? info.registry.url
                : string.Empty;
            _packageName = info == null || string.IsNullOrEmpty(info.name) ? PACKAGE_NAME : info.name;
        }

        internal WhatsNewSource(string packageRootPath, string version)
            : this(packageRootPath, version, PackageSource.Unknown, string.Empty)
        {
        }

        internal WhatsNewSource(string packageRootPath, string version, PackageSource source, string registryUrl)
        {
            _packageRootPath = packageRootPath;
            _version = version;
            _source = source;
            _registryUrl = registryUrl ?? string.Empty;
            _packageName = PACKAGE_NAME;
        }

        /// <summary>The installed version, or empty when the package is not resolved through UPM.</summary>
        internal string Version => _version;

        /// <summary>
        /// How the package got into the project - a registry, a Git URL, an embedded folder - which
        /// is what decides whether the Package Manager will ever offer an update for it.
        /// </summary>
        internal PackageSource Source => _source;

        /// <summary>The registry the package was installed from, or empty when it came another way.</summary>
        internal string RegistryUrl => _registryUrl;

        internal string PackageName => _packageName;

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