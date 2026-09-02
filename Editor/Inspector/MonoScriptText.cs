#if UNITY_EDITOR

using System;
using System.IO;
using UnityEditor;

namespace FlowIoC.Editor.Inspector
{
    /// <summary>
    /// Finds the .cs file a type was compiled from by asking the AssetDatabase for the MonoScript
    /// that declares it. A type from a precompiled assembly has no such asset and answers null -
    /// which is why every caller treats missing help as ordinary.
    /// </summary>
    internal class MonoScriptText : IFlowScriptText
    {
        public string Read(Type type)
        {
            string[] guids = AssetDatabase.FindAssets($"t:MonoScript {type.Name}");

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);

                if (script == null || script.GetClass() != type)
                    continue;

                string absolute = Path.GetFullPath(path);

                return File.Exists(absolute) ? File.ReadAllText(absolute) : null;
            }

            return null;
        }
    }
}

#endif
