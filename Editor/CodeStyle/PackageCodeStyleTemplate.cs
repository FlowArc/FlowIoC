#if UNITY_EDITOR

using System.IO;
using FlowIoC.Editor.AgentRules;
using UnityEditor.PackageManager;

namespace FlowIoC.Editor.CodeStyle
{
    /// <summary>
    /// Locates the code style FlowIoC ships. The package resolves to a hashed path under
    /// Library/PackageCache for a UPM install and to Packages/FlowIoC for a submodule, so the
    /// path is asked of the Package Manager rather than assumed.
    /// </summary>
    internal class PackageCodeStyleTemplate
    {
        internal const string Folder = "CodeStyle~";
        internal const string FileName = "SolutionCodeStyle.DotSettings";

        internal string Resolve()
        {
            var info = PackageInfo.FindForAssembly(typeof(PackageCodeStyleTemplate).Assembly);

            string packageRoot = info != null
                ? info.resolvedPath
                : Path.Combine(new ProjectRoot().Resolve(), "Packages", "FlowIoC");

            return Path.Combine(packageRoot, Folder, FileName);
        }
    }
}

#endif
