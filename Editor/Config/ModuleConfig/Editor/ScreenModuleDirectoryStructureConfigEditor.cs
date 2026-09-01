#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace FlowIoC.Editor.Config.ModuleConfig.Editor
{
    [CustomEditor(typeof(ED_ScreenModuleDirectoryStructure))]
    public class ScreenModuleDirectoryStructureConfigEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            ED_ScreenModuleDirectoryStructure config = target as ED_ScreenModuleDirectoryStructure;
            if (config == null)
                return;

            Dictionary<FolderEVO.FolderType, int> usageDict = new Dictionary<FolderEVO.FolderType, int>();
            GatherFolderTypes(config.RootFolders, usageDict);

            List<FolderEVO.FolderType> duplicates = usageDict
                .Where(kv => kv.Key != FolderEVO.FolderType.Folder && kv.Value > 1)
                .Select(kv => kv.Key)
                .ToList();

            if (duplicates.Count > 0)
            {
                string dupList = string.Join(", ", duplicates);
                EditorGUILayout.HelpBox(
                    $"The following locked FolderTypes appear more than once: {dupList}. " +
                    $"Please ensure each locked type is used only once.",
                    MessageType.Error
                );
            }
        }

        private void GatherFolderTypes(List<FolderEVO> folders, Dictionary<FolderEVO.FolderType, int> usageDict)
        {
            if (folders == null)
                return;

            foreach (FolderEVO folder in folders)
            {
                FolderEVO.FolderType type = folder.Type;
                usageDict.TryAdd(type, 0);
                usageDict[type]++;

                if (folder.SubFolders != null && folder.SubFolders.Count > 0)
                {
                    GatherFolderTypes(folder.SubFolders, usageDict);
                }
            }
        }
    }
}
#endif