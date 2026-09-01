#if UNITY_EDITOR

using System;
using UnityEditor;
using UnityEngine;

namespace FlowIoC.Editor.FolderPainter
{
    // The field names below are the serialization keys of the config asset.
    // Renaming a field drops whatever the user already configured, so keep them stable.

    [Serializable]
    public class FolderPainterPathRuleEVO
    {
        public FolderPainterPathMatchEVO PathRule = new FolderPainterPathMatchEVO();
        public FolderPainterVisualEVO Visual = new FolderPainterVisualEVO();
    }

    [Serializable]
    public class FolderPainterFolderRuleEVO
    {
        public FolderPainterFolderTargetEVO FolderRule = new FolderPainterFolderTargetEVO();
        public FolderPainterVisualEVO VisualConfig = new FolderPainterVisualEVO();
    }

    [Serializable]
    public class FolderPainterVisualEVO
    {
        public FolderPainterColorEVO ColorInfo = new FolderPainterColorEVO();
        public FolderPainterSelectionEVO Selection = new FolderPainterSelectionEVO();
        public FolderPainterLabelEVO Text = new FolderPainterLabelEVO();
        public FolderPainterIconEVO Icon = new FolderPainterIconEVO();
    }

    [Serializable]
    public class FolderPainterPathMatchEVO
    {
        public string Value;
        public FolderPainterPathCheckType Type;
    }

    public enum FolderPainterPathCheckType
    {
        Contains,
        EndsWith,
        StartsWith
    }

    [Serializable]
    public class FolderPainterFolderTargetEVO
    {
        public DefaultAsset Folder;

        public string Path => AssetDatabase.GetAssetPath(Folder);
    }

    [Serializable]
    public class FolderPainterColorEVO
    {
        public Color StartColor;
        public Color EndColor;
    }

    [Serializable]
    public class FolderPainterSelectionEVO
    {
        public bool OverrideSelectionColor;
        public Color Color;
    }

    [Serializable]
    public class FolderPainterLabelEVO
    {
        public bool OverrideFont;
        public Color Color;
        public FontStyle Style;
        public float TextOffset = 18.5f;

        public bool OverrideLabel;
        public string Label;
    }

    [Serializable]
    public class FolderPainterIconEVO
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
