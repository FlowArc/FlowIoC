#if UNITY_EDITOR
using System;
using FlowIoC.Editor.Config.ModuleConfig;
using UnityEngine.Rendering;

namespace FlowIoC.Editor.Modules
{
    /// <summary>
    /// One module's cached identity. Name and kind are derived from the folder tree and are
    /// held here only so the tools do not have to walk it on every repaint; the folder GUIDs
    /// are the part that cannot be derived, because a folder renamed outside the tool keeps
    /// its GUID and loses its name.
    /// </summary>
    [Serializable]
    internal class ModuleDescriptorEVO
    {
        public string Name;
        public ModuleKind Kind;
        public string FolderGuid;

        public SerializedDictionary<FolderEVO.FolderType, string> FolderGuids =
            new SerializedDictionary<FolderEVO.FolderType, string>();

        public bool TryGetFolderGuid(FolderEVO.FolderType type, out string guid)
        {
            guid = null;
            return FolderGuids != null
                   && FolderGuids.TryGetValue(type, out guid)
                   && !string.IsNullOrEmpty(guid);
        }

        public void RecordFolderGuid(FolderEVO.FolderType type, string guid)
        {
            FolderGuids ??= new SerializedDictionary<FolderEVO.FolderType, string>();
            FolderGuids[type] = guid;
        }
    }
}

#endif
