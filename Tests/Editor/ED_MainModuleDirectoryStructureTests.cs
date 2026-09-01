using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using FlowIoC.Editor.CodeGenerator;
using FlowIoC.Editor.Config.ModuleConfig;
using NUnit.Framework;
using UnityEngine;

namespace FlowIoC.Tests
{
    public class ED_MainModuleDirectoryStructureTests
    {
        private ED_MainModuleDirectoryStructure _config;

        [SetUp]
        public void SetUp() => _config = ScriptableObject.CreateInstance<ED_MainModuleDirectoryStructure>();

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

        /// <summary>
        /// Shared is a sibling of Runtime, not a folder inside it: it becomes its own assembly so
        /// that a screen or sub module can read the data a module publishes without referencing
        /// the assembly that holds its Models and Commands.
        /// </summary>
        [Test]
        public void The_generated_default_carries_a_Shared_folder_next_to_Runtime()
        {
            InitializeDefaults();

            FolderEVO scripts = Find(_config.RootFolders, "Scripts");
            Assert.IsNotNull(scripts, "No Scripts folder in the default structure.");

            List<string> names = scripts.SubFolders.Select(f => f.FolderName).ToList();
            CollectionAssert.Contains(names, "Runtime");
            CollectionAssert.Contains(names, "Shared");
        }

        [Test]
        public void The_field_initializer_carries_a_Shared_folder_too()
        {
            FolderEVO scripts = Find(_config.RootFolders, "Scripts");
            Assert.IsNotNull(scripts, "No Scripts folder in the field initializer.");

            CollectionAssert.Contains(scripts.SubFolders.Select(f => f.FolderName).ToList(), "Shared");
        }

        /// <summary>
        /// Most modules publish nothing, so Shared is offered as a tick in the folder preview
        /// rather than laid down for every module - and its own subfolders are mandatory, so
        /// ticking it once brings all of them.
        /// </summary>
        [Test]
        public void Shared_is_optional_and_its_subfolders_come_with_it()
        {
            InitializeDefaults();

            FolderEVO shared = Find(_config.RootFolders, "Shared");
            Assert.IsNotNull(shared);
            Assert.IsTrue(shared.IsOptional, "Shared should be offered as an optional folder.");
            Assert.IsFalse(shared.IsMandatory);

            foreach (FolderEVO child in shared.SubFolders)
            {
                Assert.IsTrue(child.IsMandatory, $"'{child.FolderName}' should come with Shared rather than need its own tick.");
            }
        }

        /// <summary>
        /// A FolderType resolves to one path and one recorded GUID per module, so the Shared
        /// data folders cannot reuse the types the Runtime ones carry.
        /// </summary>
        [Test]
        public void Shared_data_folders_carry_types_of_their_own()
        {
            InitializeDefaults();

            Assert.AreEqual(FolderEVO.FolderType.SharedUnityObjects, FolderTypeAt("Shared", "UnityObjects"));
            Assert.AreEqual(FolderEVO.FolderType.SharedValueObjects, FolderTypeAt("Shared", "ValueObjects"));
            Assert.AreEqual(FolderEVO.FolderType.SharedEnums, FolderTypeAt("Shared", "Enums"));
            Assert.AreEqual(FolderEVO.FolderType.SharedConstants, FolderTypeAt("Shared", "Constants"));
        }

        [Test]
        public void A_Shared_folder_type_resolves_under_Scripts_Shared()
        {
            InitializeDefaults();

            string path = _config.FindFullFolderPathByID(FolderEVO.FolderType.SharedUnityObjects, "base");

            Assert.AreEqual(Path.Combine("base", "Scripts", "Shared", "Data", "UnityObjects"), path);
        }

        /// <summary>
        /// The Shared subfolders are mandatory within Shared but Shared itself is not, so a module
        /// created without it has none of them - which the caller that warns about missing folders
        /// has to be able to tell apart from a folder that really went missing.
        /// </summary>
        [Test]
        public void A_folder_inside_an_optional_parent_reports_as_optional()
        {
            InitializeDefaults();

            _config.FindFullFolderPathByID(FolderEVO.FolderType.SharedUnityObjects, "base", out bool isOptional);

            Assert.IsTrue(isOptional);
        }

        [Test]
        public void A_folder_inside_a_mandatory_parent_still_reports_as_mandatory()
        {
            InitializeDefaults();

            _config.FindFullFolderPathByID(FolderEVO.FolderType.UnityObjects, "base", out bool isOptional);

            Assert.IsFalse(isOptional);
        }

        /// <summary>
        /// Every project that already ran the code generator has its own serialized config, which
        /// GetOrCreateConfig loads untouched - so the branch has to be added to it rather than
        /// only stamped onto brand new assets.
        /// </summary>
        [Test]
        public void EnsureSharedBranch_adds_the_branch_to_a_config_that_predates_it()
        {
            InitializeDefaults();
            RemoveSharedBranch();

            var settings = ScriptableObject.CreateInstance<ED_CodeGenerator>();
            try
            {
                Assert.IsTrue(_config.EnsureSharedBranch(settings));

                FolderEVO scripts = Find(_config.RootFolders, "Scripts");
                CollectionAssert.Contains(scripts.SubFolders.Select(f => f.FolderName).ToList(), "Shared");
                Assert.AreEqual(FolderEVO.FolderType.SharedValueObjects, FolderTypeAt("Shared", "ValueObjects"));
            }
            finally
            {
                Object.DestroyImmediate(settings);
            }
        }

        /// <summary>
        /// A folder is only rename-tracked while its type is in the settings map - that map is
        /// what ModuleIndexRegistrar records GUIDs from - so a settings asset written before these
        /// types existed has to gain them alongside the branch itself.
        /// </summary>
        [Test]
        public void EnsureSharedBranch_registers_the_Shared_folder_names_it_needs()
        {
            InitializeDefaults();
            RemoveSharedBranch();

            var settings = ScriptableObject.CreateInstance<ED_CodeGenerator>();
            try
            {
                settings.DirectoryStructureConfigMap.Remove(FolderEVO.FolderType.Shared);
                settings.DirectoryStructureConfigMap.Remove(FolderEVO.FolderType.SharedUnityObjects);
                settings.DirectoryStructureConfigMap.Remove(FolderEVO.FolderType.SharedValueObjects);
                settings.DirectoryStructureConfigMap.Remove(FolderEVO.FolderType.SharedEnums);
                settings.DirectoryStructureConfigMap.Remove(FolderEVO.FolderType.SharedConstants);

                _config.EnsureSharedBranch(settings);

                Assert.AreEqual("Shared", settings.DirectoryStructureConfigMap[FolderEVO.FolderType.Shared]);
                Assert.AreEqual("UnityObjects", settings.DirectoryStructureConfigMap[FolderEVO.FolderType.SharedUnityObjects]);
                Assert.AreEqual("ValueObjects", settings.DirectoryStructureConfigMap[FolderEVO.FolderType.SharedValueObjects]);
                Assert.AreEqual("Enums", settings.DirectoryStructureConfigMap[FolderEVO.FolderType.SharedEnums]);
                Assert.AreEqual("Constants", settings.DirectoryStructureConfigMap[FolderEVO.FolderType.SharedConstants]);
            }
            finally
            {
                Object.DestroyImmediate(settings);
            }
        }

        [Test]
        public void EnsureSharedBranch_leaves_a_config_that_already_has_one_alone()
        {
            InitializeDefaults();

            var settings = ScriptableObject.CreateInstance<ED_CodeGenerator>();
            try
            {
                Assert.IsFalse(_config.EnsureSharedBranch(settings));

                FolderEVO scripts = Find(_config.RootFolders, "Scripts");
                Assert.AreEqual(1, scripts.SubFolders.Count(f => f.FolderName == "Shared"));
            }
            finally
            {
                Object.DestroyImmediate(settings);
            }
        }

        private void RemoveSharedBranch()
        {
            FolderEVO scripts = Find(_config.RootFolders, "Scripts");
            scripts.SubFolders.RemoveAll(f => f.Type == FolderEVO.FolderType.Shared);
        }

        private FolderEVO.FolderType FolderTypeAt(string branchName, string folderName)
        {
            FolderEVO branch = Find(_config.RootFolders, branchName);
            Assert.IsNotNull(branch, $"No '{branchName}' folder in the structure.");

            FolderEVO folder = Find(branch.SubFolders, folderName);
            Assert.IsNotNull(folder, $"No '{folderName}' folder under '{branchName}'.");

            return folder.Type;
        }

        private void InitializeDefaults()
        {
            typeof(ED_MainModuleDirectoryStructure)
                .GetMethod("InitializeDefaultFolderStructure", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(_config, null);
        }

        private static bool Contains(IEnumerable<FolderEVO> folders, string name) =>
            Find(folders, name) != null;

        private static FolderEVO Find(IEnumerable<FolderEVO> folders, string name)
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