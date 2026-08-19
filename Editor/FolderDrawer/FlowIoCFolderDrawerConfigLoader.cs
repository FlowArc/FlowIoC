#if UNITY_EDITOR

using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FlowIoC.FolderDrawer
{
    /// <summary>
    /// Reads the project local drawer config, and creates it with a usable set of default
    /// rules the first time a project asks for it.
    /// </summary>
    internal class FlowIoCFolderDrawerConfigLoader
    {
        /// <summary>
        /// Under Assets rather than in the package, so the colors travel with the project
        /// and not with FlowIoC.
        /// </summary>
        public string ConfigPath { get; } = "Assets/Editor/FlowIoC/FolderDrawer/FlowIoCFolderDrawerConfig.asset";

        public FlowIoCFolderDrawerConfig Load()
        {
            return AssetDatabase.LoadAssetAtPath<FlowIoCFolderDrawerConfig>(ConfigPath);
        }

        public FlowIoCFolderDrawerConfig EnsureConfig()
        {
            FlowIoCFolderDrawerConfig config = Load();
            if (config != null) return config;

            EnsureDirectory();

            config = ScriptableObject.CreateInstance<FlowIoCFolderDrawerConfig>();
            config.Enabled = true;
            config.FolderRules = new List<FolderDrawerFolderRule>();
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

        private List<FolderDrawerPathRule> CreateDefaultPathRules()
        {
            return new List<FolderDrawerPathRule>
            {
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

        private FolderDrawerPathRule CreatePathRule(string value, FolderDrawerPathCheckType type, Color startColor, Color endColor)
        {
            return new FolderDrawerPathRule
            {
                PathRule = new FolderDrawerPathMatch { Value = value, Type = type },
                Visual = new FolderDrawerVisual
                {
                    ColorInfo = new FolderDrawerColor { StartColor = startColor, EndColor = endColor }
                }
            };
        }
    }
}

#endif
