#if UNITY_EDITOR

using System;
using System.Collections.Generic;

namespace FlowIoC.Editor.SetupModules
{
    /// <summary>
    /// The build list a project should have once the setup modules' scene is in it.
    ///
    /// The scene goes first because a fresh Unity project already lists SampleScene, and a reader
    /// who installs FlowIoC and presses Play should see the flow the set brought rather than an
    /// empty scene. Nothing is removed: what was there is somebody's, and pushing it down is as far
    /// as this goes.
    ///
    /// A scene already listed is left exactly where it is. Moving it to the front on every Editor
    /// launch would be a change nobody asked for.
    /// </summary>
    internal class BuildSceneList
    {
        internal string[] WithSceneFirst(string[] existingPaths, string scenePath)
        {
            string[] existing = existingPaths ?? Array.Empty<string>();

            if (string.IsNullOrEmpty(scenePath))
                return existing;

            foreach (string path in existing)
            {
                if (string.Equals(path, scenePath, StringComparison.OrdinalIgnoreCase))
                    return existing;
            }

            var result = new List<string>(existing.Length + 1) {scenePath};
            result.AddRange(existing);

            return result.ToArray();
        }
    }
}

#endif
