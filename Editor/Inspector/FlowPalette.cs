#if UNITY_EDITOR

using System.Collections.Generic;
using FlowIoC.BaseModule.Attributes;
using UnityEngine;

namespace FlowIoC.Editor.Inspector
{
    /// <summary>
    /// The colour a role wears. Every role carries two values: a deep one the header bar is
    /// filled with, dark enough that white title text clears 4.5:1, and a vivid one used only in
    /// small places - the stripe down the left of the bar, an open help button, the edge of a
    /// help box. The accent swaps between them by skin, because the vivid value dissolves on a
    /// light background.
    /// </summary>
    public class FlowPalette
    {
        private readonly Dictionary<FlowRole, Color> _deep = new Dictionary<FlowRole, Color>();
        private readonly Dictionary<FlowRole, Color> _vivid = new Dictionary<FlowRole, Color>();

        public FlowPalette()
        {
            // Indigo, and deliberately not the violet the tools wear: the two sit together in
            // Create Module, where the window is chrome and the Root preview inside it is the role,
            // so the deep values are far enough apart to be told at a glance.
            Add(FlowRole.Core, "#37307E", "#8B82EA");

            // Nothing in a finished project wears this. Every Root is a Core, a System, a Service,
            // a Connector, an Adapter or a Test, so the plain Root is what a Root that has not said
            // which of those it is falls back to - and it is coloured to be noticed rather than to
            // blend in.
            Add(FlowRole.Root, "#A8324A", "#E86D8C");
            Add(FlowRole.Service, "#2A6FC4", "#3C8CE7");
            Add(FlowRole.System, "#136E69", "#2FA8A0");
            Add(FlowRole.View, "#9E4E0B", "#F0873C");
            Add(FlowRole.Mediator, "#9C4614", "#D9622B");
            Add(FlowRole.Screen, "#836218", "#D4A017");
            Add(FlowRole.Connector, "#2E7D3A", "#4FB55E");
            Add(FlowRole.Adapter, "#4A5364", "#7A8290");
            Add(FlowRole.Test, "#5A5A5A", "#8A8A8A");
        }

        /// <summary>
        /// FlowIoC's own colour, owned by no role. The generator windows and the Help banner are
        /// tools rather than things in the project, so they wear this instead of borrowing a role's
        /// fill - which is what they used to do, and what made them change colour the day the plain
        /// Root's did.
        ///
        /// It is the violet the tools have always been. What moved is Core, which had taken it for
        /// a while: the two sit together in Create Module, where the window is chrome and the Root
        /// preview inside it is the role, so they are the one pair that must not be the same fill.
        /// </summary>
        public Color ChromeDeep => Parse("#6C3FD1");

        public Color ChromeVivid => Parse("#9966FF");

        public Color Chrome(bool proSkin) => proSkin ? ChromeVivid : ChromeDeep;

        /// <summary>The bar's fill. White title text is legible on every one of these.</summary>
        public Color Deep(FlowRole role) => _deep[role];

        /// <summary>The stripe and the help accents. Never carries text.</summary>
        public Color Vivid(FlowRole role) => _vivid[role];

        /// <summary>
        /// What a small accent uses. The vivid value has too little contrast against the light
        /// skin's own grey, so there the deep value does the accenting instead.
        /// </summary>
        public Color Accent(FlowRole role, bool proSkin) => proSkin ? Vivid(role) : Deep(role);

        /// <summary>The strip under the bar: the fill, thinned until the module name reads on it.</summary>
        public Color Strip(FlowRole role) => Strip(Deep(role));

        /// <summary>
        /// The same strip for a fill no role owns. A window may wear a colour of its own - Module
        /// Scanner takes the green its own rows are drawn in - and still needs the strip beneath it.
        /// </summary>
        public Color Strip(Color deep) => new Color(deep.r, deep.g, deep.b, 0.22f);

        /// <summary>
        /// What a button that takes something away is tinted with - a list row's minus, Delete
        /// Module's Delete.
        ///
        /// The red channel is past 1 on purpose. <c>GUI.backgroundColor</c> multiplies the skin's
        /// button texture, so every component under 1 darkens the button as it colours it: plain
        /// <c>Color.red</c> is (1, 0, 0) and comes back as a near-black hole with a red cast, and
        /// a gentler (0.95, 0.45, 0.42) only turns the grey muddy. Scaled past 1 the button lights
        /// up instead, which is the same trick <c>ModulePanelTheme.Lifted</c> plays on a panel bar,
        /// and green and blue stay far enough down to keep it red rather than pink.
        /// </summary>
        public Color Danger => new Color(1.7f, 0.42f, 0.38f);

        public Color Title => Color.white;

        private void Add(FlowRole role, string deep, string vivid)
        {
            _deep[role] = Parse(deep);
            _vivid[role] = Parse(vivid);
        }

        private Color Parse(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out Color color);

            return color;
        }
    }
}

#endif