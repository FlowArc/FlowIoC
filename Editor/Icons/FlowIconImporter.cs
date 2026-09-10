#if UNITY_EDITOR

using UnityEditor;

namespace FlowIoC.Editor.Icons
{
    /// <summary>
    /// Imports every picture under Editor/Icons the way an icon has to be imported: as a GUI
    /// texture, uncompressed, without a mip chain. Unity's defaults for a new PNG are a
    /// compressed, mipmapped sprite-less texture, and an icon imported that way is blurred by
    /// the very resampling the set exists to avoid. The settings are applied here rather than
    /// written by hand into each meta, so a drawing dropped into the folder is right on its first
    /// import and stays right when the folder is re-imported.
    ///
    /// Only the package's own folder is touched. A project may well have an Editor/Icons of its
    /// own, and what it imports there is its business.
    /// </summary>
    internal class FlowIconImporter : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(FlowIcons.Folder() + "/") || !assetPath.EndsWith(".png"))
                return;

            var importer = (TextureImporter) assetImporter;

            importer.textureType = TextureImporterType.GUI;
            importer.mipmapEnabled = false;
            importer.sRGBTexture = true;
            importer.alphaIsTransparency = true;
            importer.filterMode = UnityEngine.FilterMode.Bilinear;
            importer.wrapMode = UnityEngine.TextureWrapMode.Clamp;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
        }
    }
}

#endif
