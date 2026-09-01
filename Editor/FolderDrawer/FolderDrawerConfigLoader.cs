#if UNITY_EDITOR

using System.Collections.Generic;
using System.IO;
using FlowIoC.BaseModule.ProjectPaths;
using FlowIoC.Editor.Migration;
using UnityEditor;
using UnityEngine;

namespace FlowIoC.Editor.FolderDrawer
{
    /// <summary>
    /// Reads the project local drawer config, and creates it with a usable set of default
    /// rules the first time a project asks for it.
    /// </summary>
    internal class FolderDrawerConfigLoader
    {
        /// <summary>
        /// Under Assets rather than in the package, so the colors travel with the project
        /// and not with FlowIoC.
        /// </summary>
        public string ConfigPath { get; } = new FlowIoCProjectPaths().FolderDrawerConfig;

        public ED_FolderDrawer Load()
        {
            return AssetDatabase.LoadAssetAtPath<ED_FolderDrawer>(ConfigPath);
        }

        public ED_FolderDrawer EnsureConfig()
        {
            // Before Load, so a project that has not migrated yet finds its existing config at the
            // new path instead of getting a fresh default and losing its colors.
            new FlowIoCPathMigrator().MigrateIfNeeded();

            ED_FolderDrawer config = Load();
            if (config != null) return config;

            EnsureDirectory();

            config = ScriptableObject.CreateInstance<ED_FolderDrawer>();
            config.Enabled = true;
            config.FolderRules = new List<FolderDrawerFolderRuleEVO>();
            config.PathRules = CreateDefaultPathRules();

            AssetDatabase.CreateAsset(config, ConfigPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"FlowIoCFolderDrawerConfig asset created at: {ConfigPath}");
            return config;
        }

        private void EnsureDirectory()
        {
            string directory = Path.GetDirectoryName(ConfigPath);
            if (string.IsNullOrEmpty(directory) || Directory.Exists(directory)) return;

            Directory.CreateDirectory(directory);
            AssetDatabase.Refresh();
        }

        private List<FolderDrawerPathRuleEVO> CreateDefaultPathRules()
        {
            return new List<FolderDrawerPathRuleEVO>
            {
                // The package rows are matched by package name and not by folder name: an embedded
                // package sits under Packages/<name from package.json>, so "Packages/FlowIoC" on
                // disk is "Packages/com.flowarc.flowioc.core" to the Project window. These come
                // first so the whole package reads as one block instead of picking up the generic
                // Module, Test and Art colors below.
                CreatePathRule("/com.flowarc.flowioc.core", FolderDrawerPathCheckType.EndsWith,
                    new Color(0.7114389f, 0.5019608f, 1f, 0.101960786f),
                    new Color(0.32527715f, 0.2999733f, 0.5943396f, 0.50980395f)),

                CreatePathRule("/com.flowarc.flowioc.core", FolderDrawerPathCheckType.Contains,
                    new Color(0.101960786f, 0.101960786f, 0.40784314f, 0.101960786f),
                    new Color(0.14901961f, 0.101960786f, 0.40784314f, 0.20392157f)),

                // Harmless in a project without the addons package: the rule simply never matches.
                CreatePathRule("/com.flowarc.flowioc.addons", FolderDrawerPathCheckType.EndsWith,
                    new Color(0.5019608f, 0.88521314f, 1f, 0.101960786f),
                    new Color(0.3254902f, 0.29803923f, 0.59607846f, 0.50980395f)),

                CreatePathRule("/com.flowarc.flowioc.addons", FolderDrawerPathCheckType.Contains,
                    new Color(0.101960786f, 0.34739718f, 0.40784314f, 0.101960786f),
                    new Color(0.14901961f, 0.101960786f, 0.40784314f, 0.20392157f)),

                CreatePathRule("/Modules", FolderDrawerPathCheckType.EndsWith,
                    new Color(0.5019608f, 0.5019608f, 1f, 0.2f),
                    new Color(0.29803923f, 0.2f, 0.6f, 0.4f)),

                CreatePathRule("Debug", FolderDrawerPathCheckType.Contains,
                    new Color(0.4f, 0.13930716f, 0.10196078f, 0.101960786f),
                    new Color(0.29803923f, 0.10196077f, 0.105761915f, 0.14901961f)),

                CreatePathRule("Test", FolderDrawerPathCheckType.Contains,
                    new Color(0.4f, 0.101960786f, 0.29803923f, 0.101960786f),
                    new Color(0.29803923f, 0.101960786f, 0.2f, 0.14901961f)),

                CreatePathRule("Module", FolderDrawerPathCheckType.EndsWith,
                    new Color(0.49019608f, 0.49019608f, 1f, 0.101960786f),
                    new Color(0.30588236f, 0.20392157f, 0.6117647f, 0.20392157f)),

                CreatePathRule("Art", FolderDrawerPathCheckType.EndsWith,
                    new Color(0.12922749f, 0.8301887f, 0.8301887f, 0.101960786f),
                    new Color(0.23633854f, 0.3071741f, 0.4433962f, 0.20392157f)),

                CreatePathRule("Art", FolderDrawerPathCheckType.Contains,
                    new Color(0.101960786f, 0.4f, 0.4f, 0.101960786f),
                    new Color(0.14901961f, 0.2f, 0.29803923f, 0.20392157f)),

                CreatePathRule("UI", FolderDrawerPathCheckType.Contains,
                    new Color(0.101960786f, 0.4f, 0.4f, 0.101960786f),
                    new Color(0.14901961f, 0.2f, 0.29803923f, 0.20392157f)),

                CreatePathRule("Module", FolderDrawerPathCheckType.Contains,
                    new Color(0.101960786f, 0.101960786f, 0.40784314f, 0.101960786f),
                    new Color(0.14901961f, 0.101960786f, 0.40784314f, 0.20392157f))
            };
        }

        private FolderDrawerPathRuleEVO CreatePathRule(string value, FolderDrawerPathCheckType type, Color startColor, Color endColor)
        {
            return new FolderDrawerPathRuleEVO
            {
                PathRule = new FolderDrawerPathMatchEVO {Value = value, Type = type},
                Visual = new FolderDrawerVisualEVO
                {
                    ColorInfo = new FolderDrawerColorEVO {StartColor = startColor, EndColor = endColor}
                }
            };
        }
    }
}

#endif