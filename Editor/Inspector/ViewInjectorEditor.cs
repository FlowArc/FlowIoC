#if UNITY_EDITOR
using System;
using FlowIoC.BaseModule.Attributes;
using FlowIoC.BaseModule.Contexts;
using FlowIoC.BaseModule.Injectable.Components;
using FlowIoC.BaseModule.ViewsMediators.Utils;
using FlowIoC.BaseModule.ViewsMediators.View;
using FlowIoC.BaseModule.ViewsMediators.View.Data;
using FlowIoC.BaseModule.ViewsMediators.View.Enums;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FlowIoC.Editor.Inspector
{
    /// <summary>
    /// The injector's inspector: one entry per view on the object, saying which Context that view
    /// belongs to and - while the game runs - whether it has reached it.
    ///
    /// Everything is drawn through the serialized object rather than by writing to the component,
    /// so an edit is undoable and lands on whatever holds the object, prefab asset or scene.
    /// </summary>
    [CustomEditor(typeof(ViewInjector))]
    public class ViewInjectorEditor : UnityEditor.Editor
    {
        /// <summary>Where the help for the fields of an entry is read from.</summary>
        private static readonly Type EntryType = typeof(ViewInjectorData);

        /// <summary>The rows the bar's help button opens and closes together.</summary>
        private static readonly string[] EntryMembers =
        {
            nameof(ViewInjectorData.AutoRegister),
            nameof(ViewInjectorData.InjectableView),
            nameof(ViewInjectorData.ContextSource),
            nameof(ViewInjectorData.SelectedRoot),
            nameof(ViewInjectorData.RootName),
            nameof(ViewInjectorData.IsRegistered)
        };

        private ViewInjector _injector;

        private FlowPalette _palette;
        private FlowRoleResolver _roles;
        private FlowHelpSource _help;
        private FlowHelpState _helpState;
        private FlowHeaderBar _bar;
        private FlowInspectorGUI _gui;

        private ViewInjectorEntries _entries;
        private ViewInjectorFoldouts _foldouts;

        private void OnEnable()
        {
            _injector = target as ViewInjector;

            _palette = new FlowPalette();
            _roles = new FlowRoleResolver();
            _help = new FlowHelpSource(new MonoScriptText());
            _helpState = new FlowHelpState();
            _bar = new FlowHeaderBar(_palette, new FlowHelpPageMap());
            _gui = new FlowInspectorGUI(_palette, _roles, _help, _helpState);

            _entries = new ViewInjectorEntries();
            _foldouts = new ViewInjectorFoldouts();
        }

        /// <summary>
        /// Only while the game runs: registration happens without the inspector being touched, and
        /// a repaint is the only way the badge can say so.
        /// </summary>
        public override bool RequiresConstantRepaint() => Application.isPlaying;

        public override void OnInspectorGUI()
        {
            if (_injector == null)
                return;

            DrawFlowHeader();

            serializedObject.Update();

            SerializedProperty entries = serializedObject.FindProperty(nameof(ViewInjector.viewDataList));

            _entries.Sync(_injector, entries);

            if (entries.arraySize == 0)
            {
                EditorGUILayout.HelpBox(
                    "Nothing to inject. This object carries no IView, so the injector has no view to hand to a Mediator.",
                    MessageType.Info);

                serializedObject.ApplyModifiedProperties();

                return;
            }

            Action pending = DrawEntries(entries);

            serializedObject.ApplyModifiedProperties();

            // Run after the properties are written, or registering would be undone by the values
            // the inspector was still holding.
            pending?.Invoke();
        }

        private void DrawFlowHeader()
        {
            Type type = _injector.GetType();

            if (!_roles.TryResolve(type, out FlowRole role))
                return;

            bool open = _helpState.IsOpen(type, FlowHelpParser.TypeKey);

            _bar.Draw(role, _roles.TitleFor(type), type.Assembly.GetName().Name, _roles.LabelFor(type, role),
                _help.Summary(type), open,
                () =>
                {
                    bool next = !open;
                    _helpState.SetOpen(type, FlowHelpParser.TypeKey, next);
                    _helpState.SetAll(EntryType, EntryMembers, next);
                });
        }

        /// <summary>
        /// The views, one entry each. An action comes back rather than being run here, because
        /// registering a view while the inspector is mid-layout would change what is being drawn.
        /// </summary>
        private Action DrawEntries(SerializedProperty entries)
        {
            Action pending = null;

            _gui.BeginCard("VIEWS", true);

            for (int i = 0; i < entries.arraySize; i++)
            {
                Action action = DrawEntry(entries.GetArrayElementAtIndex(i));

                pending ??= action;
            }

            _gui.EndCard();

            return pending;
        }

        private Action DrawEntry(SerializedProperty entry)
        {
            var view = entry.FindPropertyRelative(nameof(ViewInjectorData.View)).objectReferenceValue as IView;

            if (view == null)
                return null;

            EditorGUILayout.BeginVertical(_gui.CardEntry);

            string viewName = view.GetType().Name;
            int injectorId = _injector.GetInstanceID();

            bool wasExpanded = _foldouts.IsExpanded(injectorId, viewName);

            EditorGUILayout.BeginHorizontal();

            bool expanded = EditorGUILayout.Foldout(wasExpanded, new GUIContent(viewName), true);

            if (expanded != wasExpanded)
                _foldouts.SetExpanded(injectorId, viewName, expanded);

            DrawSourceBadge(entry);

            EditorGUILayout.EndHorizontal();

            Action pending = null;

            if (expanded)
            {
                EditorGUI.indentLevel++;

                DrawContextSource(entry);
                pending = DrawRegistration(entry, view);

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();

            return pending;
        }

        /// <summary>
        /// Where this view gets its Context, said on the folded entry as well as inside it. It
        /// wears the Root colour, because all three answers name a Root.
        /// </summary>
        private void DrawSourceBadge(SerializedProperty entry)
        {
            var source = (ViewContextSource) entry.FindPropertyRelative(nameof(ViewInjectorData.ContextSource))
                .enumValueIndex;

            Color previous = GUI.color;
            GUI.color = _palette.Accent(FlowRole.Root, EditorGUIUtility.isProSkin);

            EditorGUILayout.LabelField(BadgeFor(source), EditorStyles.miniBoldLabel, GUILayout.Width(92));

            GUI.color = previous;
        }

        private string BadgeFor(ViewContextSource source)
        {
            switch (source)
            {
                case ViewContextSource.SelectedRoot:
                    return "SELECTED ROOT";

                case ViewContextSource.RootName:
                    return "ROOT NAME";

                default:
                    return "BUBBLE UP";
            }
        }

        private void DrawContextSource(SerializedProperty entry)
        {
            _gui.Property(EntryType, entry.FindPropertyRelative(nameof(ViewInjectorData.AutoRegister)));
            _gui.Property(EntryType, entry.FindPropertyRelative(nameof(ViewInjectorData.InjectableView)));
            _gui.Property(EntryType, entry.FindPropertyRelative(nameof(ViewInjectorData.ContextSource)));

            var source = (ViewContextSource) entry.FindPropertyRelative(nameof(ViewInjectorData.ContextSource))
                .enumValueIndex;

            switch (source)
            {
                case ViewContextSource.SelectedRoot:
                    _gui.Property(EntryType, entry.FindPropertyRelative(nameof(ViewInjectorData.SelectedRoot)));
                    WarnIfPrefab();
                    break;

                case ViewContextSource.RootName:
                    _gui.Property(EntryType, entry.FindPropertyRelative(nameof(ViewInjectorData.RootName)));
                    break;
            }
        }

        /// <summary>
        /// A prefab cannot hold a reference to a Root in the scene - Unity drops it the moment the
        /// asset is saved. The trap is silent, so the inspector says it where the choice is made.
        /// </summary>
        private void WarnIfPrefab()
        {
            bool inPrefab = PrefabUtility.IsPartOfPrefabAsset(_injector.gameObject)
                            || PrefabStageUtility.GetPrefabStage(_injector.gameObject) != null;

            if (!inPrefab)
                return;

            EditorGUILayout.HelpBox(
                "A prefab cannot hold a reference to a Root in the scene. Use Root Name here, which is "
                + "resolved when the object starts.",
                MessageType.Warning);
        }

        /// <summary>
        /// What has become of the view while the game runs: the Context it reached, whether it is
        /// registered, and the one action that changes that. Out of play mode none of it has
        /// happened yet, so none of it is drawn.
        /// </summary>
        private Action DrawRegistration(SerializedProperty entry, IView view)
        {
            if (!Application.isPlaying)
                return null;

            IContext context = _injector.GetContextOfView(view);

            _gui.ReadOnlyField(EntryType, nameof(ViewInjectorData.View), "Context",
                context == null ? "not resolved" : context.GetType().Name);

            SerializedProperty registered = entry.FindPropertyRelative(nameof(ViewInjectorData.IsRegistered));

            bool pressed = _gui.Status(EntryType, nameof(ViewInjectorData.IsRegistered), "Registration",
                registered.boolValue, "● registered", "○ waiting", registered.boolValue ? "■" : "▶", true);

            if (!pressed)
                return null;

            return registered.boolValue
                ? view.UnRegister
                : () => view.Register();
        }
    }
}
#endif