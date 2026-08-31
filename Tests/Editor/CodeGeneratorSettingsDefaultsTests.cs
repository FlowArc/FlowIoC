using System.Collections.Generic;
using FlowIoC.Editor.CodeGenerator;
using FlowIoC.Editor.Config.ModuleConfig;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace FlowIoC.Tests
{
    /// <summary>
    /// The settings asset stores its two maps through Unity's own SerializedDictionary, which
    /// writes m_Keys and m_Values where FlowIoC's own copy of the type wrote keys and values.
    /// An asset serialized by the older shape therefore arrives with both maps empty, and empty
    /// is the state these tests pin: it refills from the defaults, while a map that still holds
    /// anything is the reader's and is left alone.
    /// </summary>
    public class CodeGeneratorSettingsDefaultsTests
    {
        private CodeGeneratorSettings _settings;

        [SetUp]
        public void SetUp()
        {
            _settings = ScriptableObject.CreateInstance<CodeGeneratorSettings>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_settings);
        }

        [Test]
        public void A_fresh_asset_holds_every_folder_type_the_generators_know()
        {
            var defaults = new CodeGeneratorDefaults();

            foreach (KeyValuePair<FolderConfig.FolderType, string> entry in defaults.FolderNames)
            {
                Assert.IsTrue(_settings.DirectoryStructureConfigMap.ContainsKey(entry.Key), $"{entry.Key} is missing");
                Assert.AreEqual(entry.Value, _settings.DirectoryStructureConfigMap[entry.Key]);
            }
        }

        [Test]
        public void A_fresh_asset_holds_a_config_path_per_module_kind()
        {
            CollectionAssert.AreEquivalent(
                new[] {"Main", "Screen", "Test"},
                _settings.DirectoryStructureConfigPaths.Keys);
        }

        [Test]
        public void A_map_that_deserialized_empty_is_refilled_from_the_defaults()
        {
            _settings.DirectoryStructureConfigMap.Clear();
            _settings.DirectoryStructureConfigPaths.Clear();

            Assert.IsTrue(_settings.RestoreDefaultsIfEmpty());

            Assert.AreEqual(new CodeGeneratorDefaults().FolderNames.Count, _settings.DirectoryStructureConfigMap.Count);
            Assert.AreEqual(3, _settings.DirectoryStructureConfigPaths.Count);
        }

        [Test]
        public void A_map_that_still_holds_an_entry_is_left_alone()
        {
            _settings.DirectoryStructureConfigMap.Clear();
            _settings.DirectoryStructureConfigMap[FolderConfig.FolderType.Models] = "Brains";

            Assert.IsFalse(_settings.RestoreDefaultsIfEmpty());

            Assert.AreEqual(1, _settings.DirectoryStructureConfigMap.Count);
            Assert.AreEqual("Brains", _settings.DirectoryStructureConfigMap[FolderConfig.FolderType.Models]);
        }

        [Test]
        public void A_null_map_is_refilled_rather_than_thrown_on()
        {
            _settings.DirectoryStructureConfigMap = null;

            Assert.IsTrue(_settings.RestoreDefaultsIfEmpty());
            Assert.AreEqual(new CodeGeneratorDefaults().FolderNames.Count, _settings.DirectoryStructureConfigMap.Count);
        }

        /// <summary>
        /// The field names are the whole reason the heal exists, so they are worth pinning: a
        /// future swap back to a hand-rolled dictionary would silently empty every settings asset
        /// in every project again.
        /// </summary>
        [Test]
        public void The_maps_serialize_through_Unitys_own_key_and_value_fields()
        {
            string json = EditorJsonUtility.ToJson(_settings);

            StringAssert.Contains("m_Keys", json);
            StringAssert.Contains("m_Values", json);
        }
    }
}
