#if UNITY_EDITOR && ODIN_INSPECTOR

using System;
using System.Collections.Generic;
using FlowIoC.BaseModule.Attributes;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEngine;

namespace FlowIoC.Editor.Inspector
{
    /// <summary>
    /// The header bar, for a project that has Odin. Odin claims the inspector of every component
    /// that has no editor of its own - a custom editor written for MonoBehaviour loses to it - so
    /// the bar is injected into Odin's own property list instead of drawn by an editor of ours.
    /// Everything Odin does with the component's real members is untouched.
    ///
    /// A Root does not come through here: it has its own editor, which Odin leaves alone.
    /// </summary>
    public class FlowHeaderOdinProcessor<T> : OdinPropertyProcessor<T> where T : MonoBehaviour
    {
        private FlowPalette _palette;
        private FlowRoleResolver _roles;
        private FlowHelpSource _help;
        private FlowHelpState _state;
        private FlowHeaderBar _bar;

        /// <summary>
        /// Odin can ask a processor whether it applies on an instance it built without running a
        /// constructor, so nothing is set up in a field initializer.
        /// </summary>
        private void EnsureBuilt()
        {
            if (_bar != null)
                return;

            _palette = new FlowPalette();
            _roles = new FlowRoleResolver();
            _help = new FlowHelpSource(new MonoScriptText());
            _state = new FlowHelpState();
            _bar = new FlowHeaderBar(_palette, new FlowHelpPageMap());
        }

        public override bool CanProcessForProperty(InspectorProperty property)
        {
            EnsureBuilt();

            if (!property.IsTreeRoot)
                return false;

            return _roles.TryResolve(typeof(T), out _);
        }

        public override void ProcessMemberProperties(List<InspectorPropertyInfo> infos)
        {
            EnsureBuilt();

            infos.Insert(0, InspectorPropertyInfo.CreateForDelegate("__flowHeader", -10000f, typeof(T),
                (Action) DrawHeader, new OnInspectorGUIAttribute()));
        }

        private void DrawHeader()
        {
            EnsureBuilt();

            Type type = typeof(T);

            if (!_roles.TryResolve(type, out FlowRole role))
                return;

            bool open = _state.IsOpen(type, FlowHelpParser.TypeKey);

            _bar.Draw(role, _roles.TitleFor(type), type.Assembly.GetName().Name, _roles.LabelFor(type, role),
                _help.Summary(type), open,
                () => _state.SetOpen(type, FlowHelpParser.TypeKey, !open));
        }
    }
}

#endif