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
    internal class FlowPalette
    {
        private readonly Dictionary<FlowRole, Color> _deep = new Dictionary<FlowRole, Color>();
        private readonly Dictionary<FlowRole, Color> _vivid = new Dictionary<FlowRole, Color>();

        public FlowPalette()
        {
            Add(FlowRole.Root, "#6C3FD1", "#9966FF");
            Add(FlowRole.Service, "#2A6FC4", "#3C8CE7");
            Add(FlowRole.System, "#136E69", "#2FA8A0");
            Add(FlowRole.View, "#9E4E0B", "#F0873C");
            Add(FlowRole.Mediator, "#9C4614", "#D9622B");
            Add(FlowRole.Screen, "#836218", "#D4A017");
            Add(FlowRole.Connector, "#2E7D3A", "#4FB55E");
            Add(FlowRole.Adapter, "#4A5364", "#7A8290");
            Add(FlowRole.Test, "#5A5A5A", "#8A8A8A");
        }

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
        public Color Strip(FlowRole role)
        {
            Color deep = Deep(role);

            return new Color(deep.r, deep.g, deep.b, 0.22f);
        }

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
