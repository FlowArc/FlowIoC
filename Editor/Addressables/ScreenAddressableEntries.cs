#if UNITY_EDITOR

using System;
using System.IO;

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
    /// Which Addressables entry a screen's prefab wants, worked out from its name alone. This is the
    /// part of registration that can be read and tested without an Editor; ScreenAddressables is
    /// the thin piece that talks to Unity.
    ///
    /// AssetPath is left for the caller to fill, because the generator knows where it just wrote
    /// the prefab and the installer has to go and find it.
    /// </summary>
    internal class ScreenAddressableEntries
    {
        internal const string PrefabLabel = "ScreenPrefab";
        private const string GroupPrefix = "Local_Screen-";
        private const string ScreenSuffix = "Screen";

        private const string ArtFolder = "Art";
        private const string ModuleSuffix = "Module";
        private const string ResourcesSegment = "/Resources/";

        internal ScreenAddressableEntry For(string screenName)
        {
            return new ScreenAddressableEntry
            {
                Address = screenName,
                GroupName = GroupPrefix + WithoutScreenSuffix(screenName),
                Label = PrefabLabel
            };
        }

        /// <summary>
        /// Whether a screen prefab at this path is loaded by address. One under a Resources folder
        /// is loaded by path - ScreenLoadCVO.Resource - and marking it addressable as well would
        /// ship it twice, once in resources.assets and once in a bundle nothing asks for.
        /// </summary>
        internal bool IsAddressable(string prefabPath)
        {
            return string.IsNullOrEmpty(prefabPath)
                   || !prefabPath.Replace('\\', '/').Contains(ResourcesSegment, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// A file in a screen module's Art folder: addressed by its own name, in the screen's group,
        /// with no label - the label marks the prefabs the screen service loads, and this is what
        /// one of those screens loads for itself, the way the loading screen brings in the art
        /// behind its bar once Addressables is up.
        /// </summary>
        internal ScreenAddressableEntry ForArt(string screenName, string artPath)
        {
            return new ScreenAddressableEntry
            {
                AssetPath = artPath,
                Address = Path.GetFileNameWithoutExtension(artPath),
                GroupName = GroupPrefix + WithoutScreenSuffix(screenName),
                Label = null
            };
        }

        /// <summary>
        /// The screen an Art folder belongs to, read off the module folder directly above it:
        /// LoadingScreenModule/Art is LoadingScreen's. A folder that is not called Art, or one under
        /// a module that is not a screen module, has no screen group to go to and answers null.
        /// </summary>
        internal string ScreenOfArtFolder(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath)) return null;

            string normalized = folderPath.Replace('\\', '/').TrimEnd('/');

            if (!string.Equals(Path.GetFileName(normalized), ArtFolder, StringComparison.Ordinal)) return null;

            string module = Path.GetFileName(Path.GetDirectoryName(normalized) ?? string.Empty);

            if (!module.EndsWith(ScreenSuffix + ModuleSuffix, StringComparison.Ordinal)) return null;

            return module.Substring(0, module.Length - ModuleSuffix.Length);
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
