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
            CodeGeneratorSettings.CreateConfig();
            _directoryConfigMap = new Dictionary<ModuleType, DirectoryStructureConfig>
            {
                {ModuleType.Main, MainModuleDirectoryStructureConfig.GetOrCreateConfig("Main")},
                {ModuleType.Screen, ScreenModuleDirectoryStructureConfig.GetOrCreateConfig("Screen")},
                {ModuleType.Test, TestModuleDirectoryStructureConfig.GetOrCreateConfig("Test")}
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

            _screenConfigPreview = CreateInstance<ScreenConfig>();
            _screenConfigPreview.name = "ScreenConfig Preview";

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