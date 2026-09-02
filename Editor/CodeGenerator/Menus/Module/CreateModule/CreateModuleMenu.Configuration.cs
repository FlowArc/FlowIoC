#if UNITY_EDITOR
using System.Collections.Generic;
using FlowIoC.Editor.CodeGenerator.Screens;
using FlowIoC.Editor.Config.ModuleConfig;
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

            // A fresh screen starts from the defaults; the previous type's inputs do not carry over.
            _screenSettings = new ScreenModuleSettings();
        }
    }
}
#endif