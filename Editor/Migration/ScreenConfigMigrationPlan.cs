#if UNITY_EDITOR

using System;
using System.IO;
using FlowIoC.Editor.CodeGenerator.Screens;
using FlowIoC.ScreenModule.Data;
using FlowIoC.ScreenModule.Enums;

namespace FlowIoC.Editor.Migration
{
    /// <summary>What a CD_Screen asset said, read once so the plan needs no Unity object.</summary>
    internal class LegacyScreenConfig
    {
        public string AssetPath;
        public string ViewTypeName;
        public string MediatorTypeName;
        public int Layer;
        public ScreenTag Tag;
        public ScreenLoadType LoadType;
        public bool WasDirectPrefab;
        public string AddressableKey;
        public string ResourcePath;
        public bool HasShowAnimation;
        public bool HasHideAnimation;
    }

    internal enum ScreenConfigMigrationAction
    {
        /// <summary>Write the context file and attach it to the parent Root.</summary>
        GenerateContext,

        /// <summary>Log the Screen block and what to do with it; the owner edits their own file.</summary>
        ReportBlock,

        /// <summary>Nothing can be done; say why.</summary>
        Skip
    }

    internal class ScreenConfigMigrationStep
    {
        public ScreenConfigMigrationAction Action;
        public string ContextPath;
        public string ContextName;
        public string ViewName;
        public string MediatorName;
        public ScreenModuleSettings Settings;
        public string Reason;
    }

    /// <summary>
    /// Decides what the migrator does with one CD_Screen asset. A context is generated only when
    /// the view's script was found and no context sits beside it; a context that exists is the
    /// owner's file, so it is reported rather than rewritten. A DirectPrefab config cannot be
    /// finished by anyone but the owner, because code cannot hold the prefab reference.
    /// </summary>
    internal class ScreenConfigMigrationPlan
    {
        private const string ViewSuffix = "View";
        private const string ViewsFolder = "/ViewsMediators/";
        private const string ContextsFolder = "/RootsContexts/";

        internal ScreenConfigMigrationStep For(LegacyScreenConfig config, string viewScriptPath, bool contextExists)
        {
            ScreenConfigMigrationStep step = new ScreenConfigMigrationStep
            {
                ViewName = config.ViewTypeName,
                Settings = SettingsOf(config)
            };

            if (string.IsNullOrEmpty(config.ViewTypeName))
            {
                step.Action = ScreenConfigMigrationAction.Skip;
                step.Reason = $"'{config.AssetPath}' names no view type, so there is nothing to migrate it to.";
                return step;
            }

            string baseName = config.ViewTypeName.EndsWith(ViewSuffix, StringComparison.Ordinal)
                ? config.ViewTypeName.Substring(0, config.ViewTypeName.Length - ViewSuffix.Length)
                : config.ViewTypeName;

            step.ContextName = baseName + "Context";
            step.MediatorName = string.IsNullOrEmpty(config.MediatorTypeName) ? baseName + "Mediator" : config.MediatorTypeName;

            if (viewScriptPath == null)
            {
                step.Action = ScreenConfigMigrationAction.Skip;
                step.Reason =
                    $"'{config.AssetPath}' names {config.ViewTypeName}, but no script by that name was found, so its context was not generated.";
                return step;
            }

            step.ContextPath = ContextPathFor(viewScriptPath, step.ContextName);

            string derive = $"derive it from ScreenSubContext<{step.ViewName}, {step.MediatorName}>";

            if (config.WasDirectPrefab)
            {
                step.Action = ScreenConfigMigrationAction.ReportBlock;
                step.Reason =
                    $"'{config.AssetPath}' was set to DirectPrefab, which no longer exists. Give the screen an Addressables address or a Resources path in its Screen block, {derive}, and delete the asset.";
                return step;
            }

            if (contextExists)
            {
                step.Action = ScreenConfigMigrationAction.ReportBlock;
                step.Reason =
                    $"'{step.ContextPath}' already exists. To finish the migration, {derive}, paste the Screen block below into it, and delete '{config.AssetPath}'.";
                return step;
            }

            step.Action = ScreenConfigMigrationAction.GenerateContext;
            return step;
        }

        private static ScreenModuleSettings SettingsOf(LegacyScreenConfig config)
        {
            return new ScreenModuleSettings
            {
                ManagerId = 0,
                Layer = config.Layer,
                Tag = config.Tag,
                LoadType = config.LoadType == ScreenLoadType.Resource ? ScreenLoadType.Resource : ScreenLoadType.Addressable,
                AddressableKey = config.AddressableKey,
                ResourcePath = config.ResourcePath,
                HasShowAnimation = config.HasShowAnimation,
                HasHideAnimation = config.HasHideAnimation
            };
        }

        /// <summary>
        /// The context goes where Create Module would have put it: the RootsContexts folder beside
        /// the view's ViewsMediators folder. A view kept somewhere else gets its context next to it.
        /// </summary>
        private static string ContextPathFor(string viewScriptPath, string contextName)
        {
            string folder = Path.GetDirectoryName(viewScriptPath)?.Replace('\\', '/') + "/";
            int views = folder.LastIndexOf(ViewsFolder, StringComparison.Ordinal);

            if (views >= 0)
                folder = folder.Substring(0, views) + ContextsFolder;

            return folder + contextName + ".cs";
        }
    }
}

#endif
