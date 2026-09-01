#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEngine;

namespace FlowIoC.Editor.FolderDrawer
{
    /// <summary>
    /// Plain data. It is created and read by <see cref="FolderDrawerConfigLoader"/>
    /// and lives under Assets, so every project keeps its own colors.
    /// </summary>
    class ED_FolderDrawer : ScriptableObject
    {
        public bool Enabled;
        public List<FolderDrawerFolderRuleEVO> FolderRules;
        public List<FolderDrawerPathRuleEVO> PathRules;
    }
}

#endif
