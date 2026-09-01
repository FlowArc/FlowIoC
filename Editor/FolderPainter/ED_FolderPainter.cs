#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEngine;

namespace FlowIoC.Editor.FolderPainter
{
    /// <summary>
    /// Plain data. It is created and read by <see cref="FolderPainterConfigLoader"/>
    /// and lives under Assets, so every project keeps its own colors.
    /// </summary>
    class ED_FolderPainter : ScriptableObject
    {
        public bool Enabled;
        public List<FolderPainterFolderRuleEVO> FolderRules;
        public List<FolderPainterPathRuleEVO> PathRules;
    }
}

#endif
