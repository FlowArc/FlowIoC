#if UNITY_EDITOR

using System;
using UnityEditor;
using UnityEngine;

namespace FlowIoC.Editor.FolderDrawer
{
    // The field names below are the serialization keys of the config asset.
    // Renaming a field drops whatever the user already configured, so keep them stable.

    [Serializable]
    public class FolderDrawerPathRuleEVO
    {
        public FolderDrawerPathMatchEVO PathRule = new FolderDrawerPathMatchEVO();
        public FolderDrawerVisualEVO Visual = new FolderDrawerVisualEVO();
    }

    [Serializable]
    public class FolderDrawerFolderRuleEVO
    {
        public FolderDrawerFolderTargetEVO FolderRule = new FolderDrawerFolderTargetEVO();
        public FolderDrawerVisualEVO VisualConfig = new FolderDrawerVisualEVO();
    }

    [Serializable]
    public class FolderDrawerVisualEVO
    {
        public FolderDrawerColorEVO ColorInfo = new FolderDrawerColorEVO();
        public FolderDrawerSelectionEVO Selection = new FolderDrawerSelectionEVO();
        public FolderDrawerLabelEVO Text = new FolderDrawerLabelEVO();
        public FolderDrawerIconEVO Icon = new FolderDrawerIconEVO();
    }

    [Serializable]
    public class FolderDrawerPathMatchEVO
    {
        public string Value;
        public FolderDrawerPathCheckType Type;
    }

    public enum FolderDrawerPathCheckType
    {
        Contains,
        EndsWith,
        StartsWith
    }

    [Serializable]
    public class FolderDrawerFolderTargetEVO
    {
        public DefaultAsset Folder;

        public string Path => AssetDatabase.GetAssetPath(Folder);
    }

    [Serializable]
    public class FolderDrawerColorEVO
    {
        public Color StartColor;
        public Color EndColor;
    }

    [Serializable]
    public class FolderDrawerSelectionEVO
    {
        public bool OverrideSelectionColor;
        public Color Color;
    }

    [Serializable]
    public class FolderDrawerLabelEVO
    {
        public bool OverrideFont;
        public Color Color;
        public FontStyle Style;
        public float TextOffset = 18.5f;

        public bool OverrideLabel;
        public string Label;
    }

    [Serializable]
    public class FolderDrawerIconEVO
    {
        public bool Enable;
        public Texture Texture;
        public Vector2 Size;
        public float OffsetScaleX = 1;
        public float PixelOffsetX;
        public float PixelOffsetY;
    }
}
#endif
