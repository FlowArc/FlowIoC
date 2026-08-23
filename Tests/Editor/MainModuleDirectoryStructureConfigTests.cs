using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FlowIoC.Editor.Config.ModuleConfig;
using NUnit.Framework;
using UnityEngine;

namespace FlowIoC.Tests
{
    public class MainModuleDirectoryStructureConfigTests
    {
        private MainModuleDirectoryStructureConfig _config;

        [SetUp]
        public void SetUp() => _config = ScriptableObject.CreateInstance<MainModuleDirectoryStructureConfig>();

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_config);

        /// <summary>
        /// The list a brand new config actually gets. GetOrCreateConfig calls this after
        /// CreateInstance, so this path - not the field initializer - is what a consumer sees.
        /// </summary>
        [Test]
        public void The_generated_default_carries_a_Systems_folder()
        {
            InitializeDefaults();

            Assert.IsTrue(Contains(_config.RootFolders, "Systems"));
        }

        [Test]
        public void The_generated_default_still_carries_a_Services_folder()
        {
            InitializeDefaults();

            Assert.IsTrue(Contains(_config.RootFolders, "Services"));
        }

        /// <summary>
        /// The folder list is written out twice - once as the field initializer, once in
        /// InitializeDefaultFolderStructure - so the two are easy to let drift apart.
        /// </summary>
        [Test]
        public void The_field_initializer_carries_a_Systems_folder_too()
        {
            Assert.IsTrue(Contains(_config.RootFolders, "Systems"));
        }

        [Test]
        public void Systems_sits_next_to_Services_under_Scripts_Runtime()
        {
            InitializeDefaults();

            var runtime = Find(_config.RootFolders, "Runtime");
            Assert.IsNotNull(runtime, "No Scripts/Runtime folder in the default structure.");

            var names = runtime.SubFolders.Select(f => f.FolderName).ToList();
            CollectionAssert.Contains(names, "Systems");
            CollectionAssert.Contains(names, "Services");
        }

        private void InitializeDefaults()
        {
            typeof(MainModuleDirectoryStructureConfig)
                .GetMethod("InitializeDefaultFolderStructure", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(_config, null);
        }

        private static bool Contains(IEnumerable<FolderConfig> folders, string name) =>
            Find(folders, name) != null;

        private static FolderConfig Find(IEnumerable<FolderConfig> folders, string name)
        {
            if (folders == null)
                return null;

            foreach (var folder in folders)
            {
                if (folder.FolderName == name)
                    return folder;

                var nested = Find(folder.SubFolders, name);
                if (nested != null)
                    return nested;
            }

            return null;
        }
    }
}
