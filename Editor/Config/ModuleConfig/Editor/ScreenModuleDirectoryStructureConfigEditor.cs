#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace FlowIoC.Editor.Config.ModuleConfig.Editor
{
    [CustomEditor(typeof(ScreenModuleDirectoryStructureConfig))]
    public class ScreenModuleDirectoryStructureConfigEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            ScreenModuleDirectoryStructureConfig config = target as ScreenModuleDirectoryStructureConfig;
            if (config == null)
                return;

            Dictionary<FolderConfig.FolderType, int> usageDict = new Dictionary<FolderConfig.FolderType, int>();
            GatherFolderTypes(config.RootFolders, usageDict);

            List<FolderConfig.FolderType> duplicates = usageDict
                .Where(kv => kv.Key != FolderConfig.FolderType.Folder && kv.Value > 1)
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

        private void GatherFolderTypes(List<FolderConfig> folders, Dictionary<FolderConfig.FolderType, int> usageDict)
        {
            if (folders == null)
                return;

            foreach (FolderConfig folder in folders)
            {
                FolderConfig.FolderType type = folder.Type;
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