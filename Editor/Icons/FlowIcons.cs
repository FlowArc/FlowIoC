#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace FlowIoC.Editor.Icons
{
    /// <summary>
    /// Draws a <see cref="FlowIcon"/> at the size it was rasterised for. The drawings are white on
    /// transparent and take their colour at draw time, so one file serves both skins and the
    /// selected row alike: the icon is tinted with the colour of the words beside it.
    ///
    /// Every icon ships at 16, 24, 32 and 48 pixels - 16 and 24 points at 1x and at 2x - and the
    /// file for a draw is the one whose pixels match the rect, so nothing is resampled. Unity's own
    /// icons are what this replaces: they come as 16 pixel point-filtered bitmaps and as 256 pixel
    /// mip chains, and either drawn into a 22 pixel box is a smeared or a jagged one.
    ///
    /// A window owns one of these and keeps it for as long as it is open: a texture is loaded on
    /// the first draw that asks for it and kept from then on.
    /// </summary>
    internal class FlowIcons
    {
        /// <summary>The pixel sizes every icon ships at, smallest first.</summary>
        internal static readonly int[] ShippedSizes = {16, 24, 32, 48};

        private const string FALLBACK_ROOT = "Packages/com.flowarc.flowioc.core";

        private readonly Dictionary<(FlowIcon, int), Texture2D> _loaded =
            new Dictionary<(FlowIcon, int), Texture2D>();

        /// <summary>
        /// Draws <paramref name="icon"/> filling <paramref name="rect"/>, in <paramref name="tint"/>.
        /// The rect is in points; the file chosen is the one whose pixels match it on this screen.
        /// On a scale no shipped size matches - 125%, 175% - the next size up is drawn scaled down,
        /// which is the same softening every other picture in the Editor gets at that scale.
        /// </summary>
        public void Draw(Rect rect, FlowIcon icon, Color tint)
        {
            if (icon == FlowIcon.None)
                return;

            Texture2D texture = Get(icon, PixelsFor(rect.width));

            if (texture == null)
                return;

            GUI.DrawTexture(rect, texture, ScaleMode.StretchToFill, true, 0f, tint, 0f, 0f);
        }

        /// <summary>
        /// The file name an icon has at a pixel size: Book-16.png. One place spells the shape, so
        /// the test that checks every icon ships every size and the loader that reads them agree.
        /// </summary>
        internal static string FileName(FlowIcon icon, int pixels) => $"{icon}-{pixels}.png";

        /// <summary>
        /// The shipped size a draw of <paramref name="points"/> wide needs on this screen: the
        /// exact pixel count when it ships, else the smallest shipped size above it.
        /// </summary>
        internal static int PixelsFor(float points)
        {
            int wanted = Mathf.RoundToInt(points * EditorGUIUtility.pixelsPerPoint);

            foreach (int size in ShippedSizes)
            {
                if (size >= wanted)
                    return size;
            }

            return ShippedSizes[ShippedSizes.Length - 1];
        }

        private Texture2D Get(FlowIcon icon, int pixels)
        {
            if (_loaded.TryGetValue((icon, pixels), out Texture2D cached) && cached != null)
                return cached;

            string path = $"{Folder()}/{FileName(icon, pixels)}";
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);

            if (texture == null)
            {
                Debug.LogError($"<color=cyan>[FlowIoC]</color> No icon at {path}. Every FlowIcon "
                               + "ships at 16, 24, 32 and 48 pixels under Editor/Icons.");
            }

            _loaded[(icon, pixels)] = texture;

            return texture;
        }

        /// <summary>
        /// The folder the icons live in, under the package root read off this assembly rather
        /// than hardcoded, so the icons are found however the package was installed - embedded,
        /// from a Git URL, or from a registry.
        /// </summary>
        internal static string Folder()
        {
            UnityEditor.PackageManager.PackageInfo package =
                UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(FlowIcons).Assembly);

            return (package != null ? package.assetPath : FALLBACK_ROOT) + "/Editor/Icons";
        }
    }
}

#endif
