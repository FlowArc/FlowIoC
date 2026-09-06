#if UNITY_EDITOR
using FlowIoC.BaseModule.Attributes;
using FlowIoC.Editor.Inspector;
using UnityEditor;
using UnityEngine;

namespace FlowIoC.Editor.CodeGenerator.Menus.Module
{
    /// <summary>
    /// What the Create Module window's panels are painted with. A panel's bar wears a role's
    /// colour and the rows under it a washed-out version of the same, so the panel reads as one
    /// surface rather than as a coloured bar with grey boxes beneath it - and so a panel about
    /// screens is recognisable as one before its labels are read.
    /// </summary>
    internal class ModulePanelTheme
    {
        private readonly FlowPalette _palette = new FlowPalette();

        /// <summary>
        /// The parent-module and folder-structure panels. Those two are about the window rather
        /// than about a role, so they take FlowIoC's own colour - the panels that are about a role
        /// go through RowFor and HeaderFor instead.
        /// </summary>
        public Color Row => Washed(_palette.ChromeDeep);

        public Color Header => Lifted(_palette.ChromeDeep, EditorGUIUtility.isProSkin ? 2f : 1.5f);

        /// <summary>
        /// A row's tint: the role's colour, lifted and then washed most of the way back to white.
        /// The rows are a list under a header, so they carry the colour rather than wear it.
        /// </summary>
        public Color RowFor(FlowRole role) => Washed(_palette.Deep(role));

        private Color Washed(Color deep) =>
            Color.Lerp(Color.white, Lifted(deep, 1.6f), EditorGUIUtility.isProSkin ? 0.45f : 0.3f);

        /// <summary>
        /// The bar over a panel. Lifted less than the rows beneath it, so it stays the stronger
        /// colour of the two and the panel reads as a header with a list under it.
        /// </summary>
        public Color HeaderFor(FlowRole role) =>
            Lifted(_palette.Deep(role), EditorGUIUtility.isProSkin ? 2f : 1.5f);

        /// <summary>
        /// Mixed from the deep value the inspector's own header bars are filled with rather than
        /// from the vivid accent, and scaled past 1 rather than toward white: GUI.backgroundColor
        /// multiplies the skin's box texture, so a tint under 1 darkens the row as it colours it,
        /// and mixing toward white keeps the row light by taking the colour out of it.
        /// </summary>
        private Color Lifted(Color deep, float lift) =>
            new Color(deep.r * lift, deep.g * lift, deep.b * lift);
    }
}
#endif