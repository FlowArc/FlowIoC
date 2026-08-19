#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEngine;

namespace FlowIoC.Editor.FolderDrawer
{
    /// <summary>
    /// Plain data. It is created and read by <see cref="FlowIoCFolderDrawerConfigLoader"/>
    /// and lives under Assets, so every project keeps its own colors.
    /// </summary>
    class FlowIoCFolderDrawerConfig : ScriptableObject
    {
        public bool Enabled;
        public List<FolderDrawerFolderRule> FolderRules;
        public List<FolderDrawerPathRule> PathRules;
    }
}

#endif
