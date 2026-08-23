#if UNITY_EDITOR

using System;
using System.IO;
using UnityEditor.PackageManager;
using UnityEngine;

namespace FlowIoC.Editor.AgentRules
{
    /// <summary>
    /// Reads the shipped rule text out of the installed package. The package resolves to a
    /// hashed path under Library/PackageCache for a UPM install and to Packages/FlowIoC for a
    /// submodule, so the path is asked of the Package Manager rather than assumed.
    /// </summary>
    internal class AgentRulesSource
    {
        internal const string DocumentationFolder = "Documentation~";
        internal const string FileName = "AgentRules.md";
        internal const string VersionPlaceholder = "{VERSION}";

        private readonly string _packageRootPath;

        internal string Version { get; }

        internal AgentRulesSource()
        {
            var info = PackageInfo.FindForAssembly(typeof(AgentRulesSource).Assembly);

            if (info != null)
            {
                _packageRootPath = info.resolvedPath;
                Version = info.version;
                return;
            }

            // Embedded or otherwise unresolvable: fall back to the conventional submodule location.
            _packageRootPath = Path.Combine(new ProjectRoot().Resolve(), "Packages", "FlowIoC");
            Version = ReadVersionFromPackageJson(_packageRootPath);
        }

        internal AgentRulesSource(string packageRootPath, string version)
        {
            _packageRootPath = packageRootPath;
            Version = version;
        }

        internal bool TryRead(out string body, out string error)
        {
            body = null;
            error = null;

            string path = Path.Combine(_packageRootPath, DocumentationFolder, FileName);

            if (!File.Exists(path))
            {
                error = $"FlowIoC could not find its rule text at '{path}'. "
                        + $"Expected {DocumentationFolder}/{FileName} inside the package.";
                return false;
            }

            try
            {
                body = File.ReadAllText(path).Replace(VersionPlaceholder, Version);
                return true;
            }
            catch (Exception exception)
            {
                error = $"FlowIoC could not read '{path}': {exception.Message}";
                return false;
            }
        }

        private string ReadVersionFromPackageJson(string packageRoot)
        {
            string path = Path.Combine(packageRoot, "package.json");
            if (!File.Exists(path))
                return "master";

            foreach (string line in File.ReadAllLines(path))
            {
                int key = line.IndexOf("\"version\"", StringComparison.Ordinal);
                if (key < 0)
                    continue;

                int colon = line.IndexOf(':', key);
                if (colon < 0)
                    continue;

                int open = line.IndexOf('"', colon + 1);
                int close = open < 0 ? -1 : line.IndexOf('"', open + 1);

                if (open > 0 && close > open)
                    return line.Substring(open + 1, close - open - 1);
            }

            return "master";
        }
    }

    /// <summary>
    /// The folder that holds Assets/, Packages/ and ProjectSettings/. Every file this feature
    /// writes is relative to it, and more than one type needs it, so it lives in one place.
    /// </summary>
    internal class ProjectRoot
    {
        internal string Resolve()
        {
            var parent = Directory.GetParent(Application.dataPath);
            return parent != null ? parent.FullName : Application.dataPath;
        }
    }
}

#endif
