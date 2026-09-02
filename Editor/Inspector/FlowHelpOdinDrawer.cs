#if UNITY_EDITOR && ODIN_INSPECTOR

using System;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace FlowIoC.Editor.Inspector
{
    /// <summary>
    /// The help button, inside Odin's own drawing. Odin lays a component's body out itself, so the
    /// only way a field gets a "?" beside it is to join the chain: this draws the gutter and then
    /// calls the next drawer, which is the one that would have drawn the field anyway. Every Odin
    /// attribute keeps working, because none of them is replaced.
    ///
    /// It is offered every property in the project and answers "not mine" for anything FlowIoC
    /// cannot document, so nothing outside FlowIoC changes shape. The role is checked before the
    /// help text is, because resolving a type is cheap and reading its source file is not.
    /// </summary>
    [DrawerPriority(DrawerPriorityLevel.WrapperPriority)]
    public class FlowHelpOdinDrawer<T> : OdinValueDrawer<T>
    {
        private const float Gutter = 16f;

        private FlowRoleResolver _roles;
        private FlowHelpSource _help;
        private FlowHelpState _state;
        private FlowInspectorSettings _settings;
        private FlowInspectorGUI _gui;

        /// <summary>
        /// Odin asks a drawer whether it can draw a property on an instance it built without
        /// running any constructor, so nothing may be set up in a field initializer or in
        /// Initialize. Everything is built on first use instead.
        /// </summary>
        private void EnsureBuilt()
        {
            if (_gui != null)
                return;

            var palette = new FlowPalette();

            _roles = new FlowRoleResolver();
            _help = new FlowHelpSource(new MonoScriptText());
            _state = new FlowHelpState();
            _settings = new FlowInspectorSettings();
            _gui = new FlowInspectorGUI(palette, _roles, _help, _state);
        }

        protected override bool CanDrawValueProperty(InspectorProperty property)
        {
            EnsureBuilt();

            if (!_settings.Enabled)
                return false;

            Type owner = property.ParentType;

            if (owner == null || !_roles.TryResolve(owner, out _))
                return false;

            return !string.IsNullOrEmpty(_help.For(owner, property.Name));
        }

        protected override void DrawPropertyLayout(GUIContent label)
        {
            EnsureBuilt();

            Type owner = Property.ParentType;

            GUILayout.BeginHorizontal();

            Rect gutter = GUILayoutUtility.GetRect(Gutter, EditorGUIUtility.singleLineHeight,
                GUILayout.Width(Gutter), GUILayout.ExpandWidth(false));

            _gui.QuestionButton(owner, Property.Name, gutter);

            GUILayout.BeginVertical();
            CallNextDrawer(label);
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();

            _gui.HelpBox(owner, Property.Name);
        }
    }
}

#endif