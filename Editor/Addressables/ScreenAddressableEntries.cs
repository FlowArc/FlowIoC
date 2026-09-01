#if UNITY_EDITOR

using System;

namespace FlowIoC.Editor.Addressables
{
    /// <summary>One asset, and where it belongs in the Addressables groups.</summary>
    internal class ScreenAddressableEntry
    {
        internal string AssetPath;
        internal string Address;
        internal string GroupName;

        /// <summary>The label to set, or null when the entry carries none.</summary>
        internal string Label;
    }

    /// <summary>
    /// Which Addressables entries a screen wants, worked out from its name alone. This is the part
    /// of registration that can be read and tested without an Editor; ScreenAddressables is the
    /// thin piece that talks to Unity.
    ///
    /// AssetPath is left for the caller to fill, because the generator knows where it just wrote
    /// the files and the installer has to go and find them.
    /// </summary>
    internal class ScreenAddressableEntries
    {
        internal const string ConfigGroup = "Local_Screen-Configs";

        // A project-side Addressables label, not a type name: it is written into the consuming
        // project's Addressables settings, so it stays what it has always been even though the
        // asset it labels is now CD_Screen. Renaming it would orphan every entry already labelled.
        internal const string ConfigLabel = "ScreenConfig";
        internal const string PrefabLabel = "ScreenPrefab";
        private const string GroupPrefix = "Local_Screen-";
        private const string ScreenSuffix = "Screen";

        internal ScreenAddressableEntry[] For(string screenName)
        {
            return new[]
            {
                new ScreenAddressableEntry
                {
                    Address = screenName,
                    GroupName = GroupPrefix + WithoutScreenSuffix(screenName),
                    Label = PrefabLabel
                },
                new ScreenAddressableEntry
                {
                    Address = "CD_" + screenName,
                    GroupName = ConfigGroup,
                    Label = ConfigLabel
                }
            };
        }

        /// <summary>
        /// MainScreen groups under Local_Screen-Main, not Local_Screen-MainScreen: the prefix
        /// already says what these are. A name that does not end in Screen is left alone rather
        /// than trimmed to nothing.
        /// </summary>
        private static string WithoutScreenSuffix(string screenName)
        {
            if (string.IsNullOrEmpty(screenName))
                return screenName;

            if (!screenName.EndsWith(ScreenSuffix, StringComparison.Ordinal))
                return screenName;

            string trimmed = screenName.Substring(0, screenName.Length - ScreenSuffix.Length);

            return trimmed.Length == 0 ? screenName : trimmed;
        }
    }
}

#endif
