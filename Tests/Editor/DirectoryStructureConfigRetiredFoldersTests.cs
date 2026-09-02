using System.Collections.Generic;
using FlowIoC.Editor.Config.ModuleConfig;
using NUnit.Framework;
using UnityEngine;

namespace FlowIoC.Tests
{
    /// <summary>
    /// A directory structure config is a serialized asset in every project, so retiring a folder
    /// in code does not reach the assets already on disk. GetOrCreateConfig heals them, and this is
    /// the healing step's contract.
    /// </summary>
    public class DirectoryStructureConfigRetiredFoldersTests
    {
        private ED_MainModuleDirectoryStructure _config;

        [SetUp]
        public void SetUp() => _config = ScriptableObject.CreateInstance<ED_MainModuleDirectoryStructure>();

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_config);

        private static bool Contains(IEnumerable<FolderEVO> folders, FolderEVO.FolderType type)
        {
            foreach (FolderEVO folder in folders)
            {
                if (folder.Type == type) return true;
                if (folder.SubFolders != null && Contains(folder.SubFolders, type)) return true;
            }

            return false;
        }

        [Test]
        public void The_defaults_no_longer_lay_down_a_screen_configs_folder()
        {
            Assert.IsFalse(Contains(_config.RootFolders, FolderEVO.FolderType.ScreenConfigs));
        }

        [Test]
        public void A_retired_folder_is_removed_wherever_it_sits()
        {
            FolderEVO scriptables = _config.RootFolders.Find(folder => folder.FolderName == "Scriptables");
            scriptables.SubFolders.Add(new FolderEVO { FolderName = "ScreenConfigs", Type = FolderEVO.FolderType.ScreenConfigs });

            bool removed = _config.RemoveFolderType(FolderEVO.FolderType.ScreenConfigs);

            Assert.IsTrue(removed);
            Assert.IsFalse(Contains(_config.RootFolders, FolderEVO.FolderType.ScreenConfigs));
            Assert.IsNotNull(_config.RootFolders.Find(folder => folder.FolderName == "Scriptables"));
        }

        [Test]
        public void Removing_a_folder_that_is_not_there_reports_nothing_changed()
        {
            Assert.IsFalse(_config.RemoveFolderType(FolderEVO.FolderType.ScreenConfigs));
        }
    }
}
