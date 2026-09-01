#if UNITY_EDITOR
using System.Collections.Generic;
using FlowIoC.Editor.Config.ModuleConfig;
using FlowIoC.ScreenModule.Data;
using UnityEngine;

namespace FlowIoC.Editor.CodeGenerator.Menus.Module.CreateModule
{
    internal partial class CreateModuleMenu
    {
        private void InitializeConfigMap()
        {
            ED_CodeGenerator.CreateConfig();
            _directoryConfigMap = new Dictionary<ModuleType, DirectoryStructureConfig>
            {
                {ModuleType.Main, ED_MainModuleDirectoryStructure.GetOrCreateConfig("Main")},
                {ModuleType.Screen, ED_ScreenModuleDirectoryStructure.GetOrCreateConfig("Screen")},
                {ModuleType.Test, ED_TestModuleDirectoryStructure.GetOrCreateConfig("Test")}
            };

            if (_directoryConfigMap == null || _directoryConfigMap.Count == 0)
            {
                Debug.LogError(DIRECTORY_CONFIG_ERROR);
            }
        }

        private void OnModuleTypeChanged()
        {
            // Each module type has its own config, so the Signals entry the toggle reads is a
            // different object per type and the new one starts unselected.
            SelectSignalsFolderByDefault();
            SelectSharedFolderByDefault();

            if (_selectedModuleType == ModuleType.Screen)
            {
                CreatePreviewScreenConfig();
            }
            else
            {
                DestroyPreviewScreenConfig();
            }
        }

        private void CreatePreviewScreenConfig()
        {
            DestroyPreviewScreenConfig();

            _screenConfigPreview = CreateInstance<CD_Screen>();
            _screenConfigPreview.name = "CD_Screen Preview";

            _screenConfigEditor = UnityEditor.Editor.CreateEditor(_screenConfigPreview);
        }

        private void DestroyPreviewScreenConfig()
        {
            if (_screenConfigEditor != null)
            {
                DestroyImmediate(_screenConfigEditor);
                _screenConfigEditor = null;
            }

            if (_screenConfigPreview != null)
            {
                DestroyImmediate(_screenConfigPreview);
                _screenConfigPreview = null;
            }
        }
    }
}
#endif