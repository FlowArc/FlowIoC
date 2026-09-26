#if UNITY_EDITOR
using System;
using FlowIoC.BaseModule.Attributes;
using FlowIoC.Editor.Inspector;
using UnityEditor;
using UnityEngine;

namespace FlowIoC.Editor.Root
{
    /// <summary>
    /// The word a sub-context wears beside its name, and its colour: SCREEN for a screen context,
    /// CONNECTOR for a connector, and for a context a service ships to be configured on the Root
    /// that uses it - the pool's - the name of what it configures in the service colour. The Root's
    /// list of sub-contexts and the Add Sub Context window both ask here, so the two never disagree.
    /// </summary>
    internal class SubContextBadge
    {
        private readonly ScreenSubContextDeclarations _declarations;
        private readonly FlowRoleResolver _roles;
        private readonly FlowPalette _palette;
        private readonly SubContextSettingsTypes _settingsTypes;

        internal SubContextBadge(ScreenSubContextDeclarations declarations, FlowRoleResolver roles, FlowPalette palette,
            SubContextSettingsTypes settingsTypes)
        {
            _declarations = declarations;
            _roles = roles;
            _palette = palette;
            _settingsTypes = settingsTypes;
        }

        /// <summary>False, with no badge, for a context that is none of the three.</summary>
        internal bool TryGet(Type contextType, out string badge, out Color color)
        {
            bool proSkin = EditorGUIUtility.isProSkin;

            if (_declarations.IsScreenContext(contextType))
            {
                badge = "SCREEN";
                color = _palette.Accent(FlowRole.Screen, proSkin);
                return true;
            }

            if (_roles.IsConnector(contextType))
            {
                badge = "CONNECTOR";
                color = _palette.Accent(FlowRole.Connector, proSkin);
                return true;
            }

            if (_settingsTypes.For(contextType) is { } settingsType)
            {
                badge = _settingsTypes.Title(settingsType).ToUpperInvariant();
                color = _palette.Accent(FlowRole.Service, proSkin);
                return true;
            }

            badge = null;
            color = Color.clear;
            return false;
        }
    }
}
#endif
