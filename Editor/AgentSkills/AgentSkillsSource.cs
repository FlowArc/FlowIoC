#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using FlowIoC.Editor.AgentRules;
using UnityEditor.PackageManager;

namespace FlowIoC.Editor.AgentSkills
{
    /// <summary>
    /// Finds the agent skills FlowIoC ships. The package resolves to a hashed path under
    /// Library/PackageCache for a UPM install and to Packages/FlowIoC for a submodule, so the
    /// path is asked of the Package Manager rather than assumed.
    /// </summary>
    internal class AgentSkillsSource
    {
        internal const string DocumentationFolder = "Documentation~";
        internal const string SkillsFolder = "Skills";
        internal const string ManifestFileName = "SKILL.md";

        private readonly string _packageRootPath;

        internal AgentSkillsSource()
        {
            var info = PackageInfo.FindForAssembly(typeof(AgentSkillsSource).Assembly);

            _packageRootPath = info != null
                ? info.resolvedPath
                : Path.Combine(new ProjectRoot().Resolve(), "Packages", "FlowIoC");
        }

        internal AgentSkillsSource(string packageRootPath)
        {
            _packageRootPath = packageRootPath;
        }

        internal string Root => Path.Combine(_packageRootPath, DocumentationFolder, SkillsFolder);

        /// <summary>
        /// The folders under Skills/ that actually hold a SKILL.md. A folder without one is not
        /// a skill and is skipped rather than reported, so supporting material can live there.
        /// </summary>
        internal bool TryList(out string[] skillFolders, out string error)
        {
            skillFolders = Array.Empty<string>();
            error = null;

            if (!Directory.Exists(Root))
            {
                error = $"FlowIoC could not find its skills at '{Root}'. "
                        + $"Expected {DocumentationFolder}/{SkillsFolder}/ inside the package.";
                return false;
            }

            try
            {
                var found = new List<string>();

                foreach (string folder in Directory.GetDirectories(Root))
                {
                    if (File.Exists(Path.Combine(folder, ManifestFileName)))
                        found.Add(folder);
                }

                found.Sort(StringComparer.Ordinal);
                skillFolders = found.ToArray();
                return true;
            }
            catch (Exception exception)
            {
                error = $"FlowIoC could not read '{Root}': {exception.Message}";
                return false;
            }
        }
    }
}

#endif
