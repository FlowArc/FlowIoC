#if UNITY_EDITOR

using System;
using UnityEditor;
using UnityEngine;

namespace FlowIoC.Editor.FolderDrawer
{
    // The field names below are the serialization keys of the config asset.
    // Renaming a field drops whatever the user already configured, so keep them stable.

    [Serializable]
    public class FolderDrawerPathRule
    {
        public FolderDrawerPathMatch PathRule = new FolderDrawerPathMatch();
        public FolderDrawerVisual Visual = new FolderDrawerVisual();
    }

    [Serializable]
    public class FolderDrawerFolderRule
    {
        public FolderDrawerFolderTarget FolderRule = new FolderDrawerFolderTarget();
        public FolderDrawerVisual VisualConfig = new FolderDrawerVisual();
    }

    [Serializable]
    public class FolderDrawerVisual
    {
        public FolderDrawerColor ColorInfo = new FolderDrawerColor();
        public FolderDrawerSelection Selection = new FolderDrawerSelection();
        public FolderDrawerLabel Text = new FolderDrawerLabel();
        public FolderDrawerIcon Icon = new FolderDrawerIcon();
    }

    [Serializable]
    public class FolderDrawerPathMatch
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
    public class FolderDrawerFolderTarget
    {
        public DefaultAsset Folder;

        public string Path => AssetDatabase.GetAssetPath(Folder);
    }

    [Serializable]
    public class FolderDrawerColor
    {
        public Color StartColor;
        public Color EndColor;
    }

    [Serializable]
    public class FolderDrawerSelection
    {
        public bool OverrideSelectionColor;
        public Color Color;
    }

    [Serializable]
    public class FolderDrawerLabel
    {
        public bool OverrideFont;
        public Color Color;
        public FontStyle Style;
        public float TextOffset = 18.5f;

        public bool OverrideLabel;
        public string Label;
    }

    [Serializable]
    public class FolderDrawerIcon
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
