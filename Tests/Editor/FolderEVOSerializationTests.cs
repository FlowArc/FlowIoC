using System.Collections.Generic;
using System.IO;
using FlowIoC.Editor.Config.ModuleConfig;
using NUnit.Framework;
using UnityEditor;

namespace FlowIoC.Tests
{
    /// <summary>
    /// The failure this exists for: FolderEVO is written through [SerializeReference], which
    /// records the type's full name in the asset rather than a GUID. Renaming the class from
    /// FolderConfig to FolderEVO therefore left every project's three directory structure assets
    /// holding entries Unity could no longer resolve, so RootFolders came back as a list of nulls.
    /// ContainsFolderType then threw on the first null and Create Module could not open at all.
    ///
    /// [MovedFrom] on FolderEVO is what teaches Unity the old name. This test writes an asset in
    /// the pre-rename format and proves the entries still arrive.
    /// </summary>
    public class FolderEVOSerializationTests
    {
        private const string ScriptGuid = "a5b6f54f94fbb6f44b8b09303357efa2";
        private const string FolderName = "FlowIoCMovedFromTest";
        private const string Folder = "Assets/" + FolderName;
        private const string AssetPath = Folder + "/ED_LegacyDirectoryStructure.asset";

        [SetUp]
        public void SetUp()
        {
            if (!AssetDatabase.IsValidFolder(Folder))
                AssetDatabase.CreateFolder("Assets", FolderName);
        }

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset(Folder);
            AssetDatabase.Refresh();
        }

        [Test]
        public void A_config_written_under_the_old_FolderConfig_name_still_deserializes()
        {
            WriteLegacyAsset();

            var config = AssetDatabase.LoadAssetAtPath<ED_MainModuleDirectoryStructure>(AssetPath);
            Assert.IsNotNull(config, "the asset itself did not load");

            List<FolderEVO> folders = config.RootFolders;

            Assert.IsNotNull(folders, "RootFolders came back null");
            Assert.AreEqual(2, folders.Count);
            CollectionAssert.DoesNotContain(folders, null,
                "a null entry means Unity could not resolve the old FolderConfig type name");
        }

        [Test]
        public void The_resolved_entries_keep_the_values_the_old_asset_held()
        {
            WriteLegacyAsset();

            List<FolderEVO> folders = AssetDatabase
                .LoadAssetAtPath<ED_MainModuleDirectoryStructure>(AssetPath)
                .RootFolders;

            Assert.AreEqual("Scripts", folders[0].FolderName);
            Assert.AreEqual(FolderEVO.FolderType.Folder, folders[0].Type);
            Assert.IsTrue(folders[0].IsMandatory);

            Assert.AreEqual("Prefabs", folders[1].FolderName);
            Assert.AreEqual(FolderEVO.FolderType.Prefabs, folders[1].Type);
            Assert.IsTrue(folders[1].IsOptional);
        }

        private static void WriteLegacyAsset()
        {
            File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), AssetPath), LegacyYaml);
            AssetDatabase.ImportAsset(AssetPath, ImportAssetOptions.ForceSynchronousImport);
        }

        private const string LegacyYaml =
            "%YAML 1.1\n" +
            "%TAG !u! tag:unity3d.com,2011:\n" +
            "--- !u!114 &11400000\n" +
            "MonoBehaviour:\n" +
            "  m_ObjectHideFlags: 0\n" +
            "  m_CorrespondingSourceObject: {fileID: 0}\n" +
            "  m_PrefabInstance: {fileID: 0}\n" +
            "  m_PrefabAsset: {fileID: 0}\n" +
            "  m_GameObject: {fileID: 0}\n" +
            "  m_Enabled: 1\n" +
            "  m_EditorHideFlags: 0\n" +
            "  m_Script: {fileID: 11500000, guid: " + ScriptGuid + ", type: 3}\n" +
            "  m_Name: ED_LegacyDirectoryStructure\n" +
            "  m_EditorClassIdentifier: \n" +
            "  <RootFolders>k__BackingField:\n" +
            "  - rid: 7001\n" +
            "  - rid: 7002\n" +
            "  references:\n" +
            "    version: 2\n" +
            "    RefIds:\n" +
            "    - rid: 7001\n" +
            "      type: {class: FolderConfig, ns: FlowIoC.Editor.Config.ModuleConfig, asm: FlowIoC.Editor}\n" +
            "      data:\n" +
            "        FolderName: Scripts\n" +
            "        Type: 0\n" +
            "        IsMandatory: 1\n" +
            "        IsOptional: 0\n" +
            "        IsNamespaceProvider: 0\n" +
            "        SubFolders: []\n" +
            "    - rid: 7002\n" +
            "      type: {class: FolderConfig, ns: FlowIoC.Editor.Config.ModuleConfig, asm: FlowIoC.Editor}\n" +
            "      data:\n" +
            "        FolderName: Prefabs\n" +
            "        Type: 15\n" +
            "        IsMandatory: 0\n" +
            "        IsOptional: 1\n" +
            "        IsNamespaceProvider: 1\n" +
            "        SubFolders: []\n";
    }
}
