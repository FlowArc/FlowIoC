#if UNITY_EDITOR && !ODIN_INSPECTOR
using System;
using FlowIoC.BaseModule.Attributes;
using UnityEditor;
using UnityEngine;

namespace FlowIoC.Editor.Inspector
{
    /// <summary>
    /// What gives an ordinary component the FlowIoC bar without asking it to derive from
    /// anything. Unity binds an inspector to a concrete type, so the only way to reach a View a
    /// game wrote is to claim MonoBehaviour itself - which this does, and then draws nothing of
    /// its own for a type that is not FlowIoC's.
    ///
    /// It is compiled only where Odin is absent. Odin claims every component that has no editor
    /// of its own, and a custom editor written for MonoBehaviour loses that contest, so with Odin
    /// installed the bar is injected into Odin's own property list by FlowHeaderOdinProcessor
    /// instead. A component with a custom editor of its own is more specific than this one and
    /// wins either way, so nothing already drawn deliberately changes shape.
    /// </summary>
    [CustomEditor(typeof(MonoBehaviour), true)]
    [CanEditMultipleObjects]
    public class FlowCatchAllEditor : UnityEditor.Editor
    {
        private FlowPalette _palette;
        private FlowRoleResolver _roles;
        private FlowHelpSource _help;
        private FlowHelpState _helpState;
        private FlowHeaderBar _bar;
        private FlowInspectorGUI _gui;

        private void OnEnable()
        {
            _palette = new FlowPalette();
            _roles = new FlowRoleResolver();
            _help = new FlowHelpSource(new MonoScriptText());
            _helpState = new FlowHelpState();
            _bar = new FlowHeaderBar(_palette, new FlowHelpPageMap());
            _gui = new FlowInspectorGUI(_palette, _roles, _help, _helpState);
        }

        public override void OnInspectorGUI()
        {
            bool decorated = DrawsBar(out Type type, out FlowRole role);

            if (decorated)
                DrawBar(type, role);

            DrawBody(decorated ? type : null);
        }

        /// <summary>
        /// Whether this selection is one FlowIoC has anything to say about. A mixed selection is
        /// left alone: a bar naming one of the two roles would be lying about the other.
        /// </summary>
        private bool DrawsBar(out Type type, out FlowRole role)
        {
            type = target != null ? target.GetType() : null;
            role = default;

            if (type == null)
                return false;

            foreach (UnityEngine.Object each in targets)
            {
                if (each == null || each.GetType() != type)
                    return false;
            }

            return _roles.TryResolve(type, out role);
        }

        private void DrawBar(Type type, FlowRole role)
        {
            bool open = _helpState.IsOpen(type, FlowHelpParser.TypeKey);

            _bar.Draw(role, _roles.TitleFor(type), type.Assembly.GetName().Name, _roles.LabelFor(type, role),
                _help.Summary(type), open,
                () => _helpState.SetOpen(type, FlowHelpParser.TypeKey, !open));
        }

        /// <summary>
        /// The properties are walked here rather than handed to DrawDefaultInspector, so the
        /// gutter that carries each field's help button can be laid out beside it.
        /// </summary>
        private void DrawBody(Type type)
        {
            serializedObject.Update();

            SerializedProperty property = serializedObject.GetIterator();
            bool first = true;

            while (property.NextVisible(first))
            {
                first = false;

                if (property.propertyPath == "m_Script")
                {
                    // Where the bar is drawn it already names the type and its module, so the
                    // Script row would only say it a second time. Everything else keeps the row
                    // it has always had.
                    if (type != null)
                        continue;

                    using (new EditorGUI.DisabledScope(true))
                        EditorGUILayout.PropertyField(property, true);

                    continue;
                }

                if (type == null)
                    EditorGUILayout.PropertyField(property, true);
                else
                    _gui.Property(type, property);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}

#endif