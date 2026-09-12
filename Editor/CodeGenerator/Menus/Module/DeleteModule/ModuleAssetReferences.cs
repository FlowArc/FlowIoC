#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;

namespace FlowIoC.Editor.CodeGenerator.Menus.Module.DeleteModule
{
    /// <summary>
    /// Which scenes and prefabs outside a module point into it. Delete Module says this before it
    /// deletes anything.
    ///
    /// It is what the sub-context script reference bought. A Root used to list its sub-contexts by
    /// name, so nothing in the project depended on the context and there was nothing to ask: the
    /// name simply stopped resolving after the module went, and the first report came at play time.
    /// The entry holds the script now, which makes the scene a dependent asset the engine already
    /// tracks.
    ///
    /// The report is the whole of the answer. Nothing is opened and nothing is saved, because
    /// editing somebody's scene is not a tool's decision - what the reader gets is the list of files
    /// to look at once the module is gone.
    ///
    /// Dependencies are read one asset at a time, which is a load per scene and prefab in the
    /// project. That is a cost Delete Module can pay, being a deliberate press with a confirmation
    /// behind it, and exactly the cost Module Scanner could not - it runs on every window focus.
    /// </summary>
    internal class ModuleAssetReferences
    {
        private const string SCENE_AND_PREFAB = "t:Scene t:Prefab";

        private readonly Func<IReadOnlyList<string>> _candidates;
        private readonly Func<string, IReadOnlyList<string>> _dependenciesOf;

        internal ModuleAssetReferences() : this(
            () => AssetDatabase.FindAssets(SCENE_AND_PREFAB)
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => !string.IsNullOrEmpty(path))
                .Distinct()
                .ToList(),
            // Not recursive: what is wanted is the assets this one names itself. A scene that
            // instances a Root prefab of the module names the prefab, which is the thing to report,
            // rather than everything the prefab in turn reaches.
            path => AssetDatabase.GetDependencies(path, false))
        {
        }

        internal ModuleAssetReferences(
            Func<IReadOnlyList<string>> candidates,
            Func<string, IReadOnlyList<string>> dependenciesOf)
        {
            _candidates = candidates;
            _dependenciesOf = dependenciesOf;
        }

        /// <summary>
        /// One line per asset-and-file pair, or an empty list when nothing outside the module points
        /// into it.
        /// </summary>
        internal IReadOnlyList<string> Find(string moduleAssetPath)
        {
            var found = new List<string>();

            string module = Folder(moduleAssetPath);

            if (string.IsNullOrEmpty(module)) return found;

            foreach (string candidate in _candidates())
            {
                // The module's own scenes and prefabs are going with it, so naming them would be a
                // list of things the reader cannot act on.
                if (IsInside(candidate, module)) continue;

                foreach (string dependency in _dependenciesOf(candidate))
                {
                    if (!IsInside(dependency, module)) continue;

                    found.Add($"{candidate} names {Path.GetFileNameWithoutExtension(dependency)}");
                }
            }

            return found;
        }

        /// <summary>
        /// The same answer as a list of assets rather than of lines, each named once.
        ///
        /// This is what narrows the unwiring: taking a sub-context out of a Root means opening the
        /// scene or prefab that holds it, and opening every one in the project to find out which
        /// ones matter would be the expensive half of the job. The dependency query answers that
        /// from the index, so only these are opened.
        /// </summary>
        internal IReadOnlyList<string> AssetsPointingInto(string moduleAssetPath)
        {
            var found = new List<string>();

            string module = Folder(moduleAssetPath);

            if (string.IsNullOrEmpty(module)) return found;

            foreach (string candidate in _candidates())
            {
                if (IsInside(candidate, module)) continue;

                foreach (string dependency in _dependenciesOf(candidate))
                {
                    if (!IsInside(dependency, module)) continue;

                    // Named once however many of the module's files it points at: this list is
                    // opened, and opening the same scene twice would do the work twice.
                    found.Add(candidate);

                    break;
                }
            }

            return found;
        }

        /// <summary>
        /// The same list with the module's own scenes and prefabs in it. Delete Module leaves those
        /// out because they go with the folder; Rename Module needs them, because a test scene
        /// inside the module lists the module's own screen context and has to follow the name.
        /// </summary>
        internal IReadOnlyList<string> AssetsPointingIntoOrInside(string moduleAssetPath)
        {
            var found = new List<string>();

            string module = Folder(moduleAssetPath);

            if (string.IsNullOrEmpty(module)) return found;

            foreach (string candidate in _candidates())
            {
                foreach (string dependency in _dependenciesOf(candidate))
                {
                    if (!IsInside(dependency, module)) continue;

                    found.Add(candidate);

                    break;
                }
            }

            return found;
        }

        /// <summary>
        /// The module folder as a path that can only match inside it. The trailing slash is what
        /// keeps PlayerHudModule out of PlayerModule's answer.
        /// </summary>
        private string Folder(string moduleAssetPath)
        {
            if (string.IsNullOrEmpty(moduleAssetPath)) return null;

            return moduleAssetPath.Replace('\\', '/').TrimEnd('/') + "/";
        }

        private bool IsInside(string assetPath, string moduleFolder)
        {
            return !string.IsNullOrEmpty(assetPath)
                   && assetPath.Replace('\\', '/')
                       .StartsWith(moduleFolder, StringComparison.OrdinalIgnoreCase);
        }
    }
}

#endif