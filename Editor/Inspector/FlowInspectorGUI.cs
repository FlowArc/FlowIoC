#if UNITY_EDITOR

using System;
using FlowIoC.BaseModule.Attributes;
using UnityEditor;
using UnityEngine;

namespace FlowIoC.Editor.Inspector
{
    /// <summary>
    /// A field, with the gutter that carries its help button. The gutter is reserved on every row
    /// of a component that documents anything, so labels stay in one column - a row that shifts
    /// left because its field has no comment is harder to read than an empty gutter.
    /// </summary>
    internal class FlowInspectorGUI
    {
        private const float Gutter = 16f;
        private const float ButtonSize = 13f;

        private readonly FlowPalette _palette;
        private readonly FlowRoleResolver _roles;
        private readonly FlowHelpSource _help;
        private readonly FlowHelpState _state;

        private GUIStyle _question;
        private GUIStyle _card;
        private GUIStyle _cardFlush;
        private GUIStyle _cardEntry;
        private GUIStyle _entryAction;
        private GUIStyle _action;

        public FlowInspectorGUI(FlowPalette palette, FlowRoleResolver roles, FlowHelpSource help, FlowHelpState state)
        {
            _palette = palette;
            _roles = roles;
            _help = help;
            _state = state;
        }

        /// <summary>
        /// The inset Unity puts around an IMGUI inspector. The header bar escapes it by drawing
        /// into a rect of its own; a layout group cannot, because the inset is the parent group's
        /// padding and a child's margin does not reach it. Negative spacing on either side of the
        /// card is what takes it back, so the card lines up with the bar instead of floating
        /// inside it.
        /// </summary>
        private const float InspectorInsetLeft = 18f;

        private const float InspectorInsetRight = 5f;

        /// <summary>A group of rows under one title, reaching the same edges the bar does.</summary>
        public GUIStyle Card => _card ??= new GUIStyle(EditorStyles.helpBox)
        {
            margin = new RectOffset(0, 0, 2, 4),
            padding = new RectOffset(10, 6, 6, 6)
        };

        /// <summary>
        /// A card whose rows carry their own background - a list of entries rather than a column
        /// of fields. It has no padding on either side, so an entry's background reaches the
        /// card's edges instead of floating inside it; the title indents itself instead.
        /// </summary>
        public GUIStyle CardFlush => _cardFlush ??= new GUIStyle(EditorStyles.helpBox)
        {
            margin = new RectOffset(0, 0, 2, 4),
            padding = new RectOffset(0, 0, 6, 6)
        };

        /// <summary>
        /// One entry inside a flush card. Tight on every side: a Root listing several sub-contexts
        /// should read as a list, and the default box leaves enough air between entries that they
        /// read as separate panels instead.
        /// </summary>
        public GUIStyle CardEntry => _cardEntry ??= new GUIStyle(GUI.skin.box)
        {
            margin = new RectOffset(0, 0, 0, 2),
            padding = new RectOffset(17, 6, 2, 2)
        };

        /// <summary>
        /// The button that takes an entry off a list. Shorter than its row and lifted off the
        /// bottom of it, so it reads as sitting on the entry rather than as part of its edge.
        /// </summary>
        public GUIStyle EntryAction => _entryAction ??= new GUIStyle(GUI.skin.button)
        {
            fixedHeight = 16f,
            margin = new RectOffset(0, 0, 1, 3),
            padding = new RectOffset(0, 0, 0, 0)
        };

        public void BeginCard(string title, bool flushSides = false)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(-InspectorInsetLeft);

            EditorGUILayout.BeginVertical(flushSides ? CardFlush : Card);

            if (flushSides)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(10f);
                EditorGUILayout.LabelField(title, EditorStyles.miniBoldLabel);
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.LabelField(title, EditorStyles.miniBoldLabel);
            }
        }

        public void EndCard()
        {
            EditorGUILayout.EndVertical();

            GUILayout.Space(-InspectorInsetRight);
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// What a card's action leaves on either side. The cards run edge to edge; a button that
        /// did the same would not read as a button at all, so this one keeps a little air.
        /// </summary>
        private const float ActionInset = 6f;

        /// <summary>
        /// A card's own action, wearing a washed out version of the role's colour - enough to say
        /// it belongs to this component, not enough to compete with the bar above it.
        /// </summary>
        public bool AccentButton(FlowRole role, string label)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(-(InspectorInsetLeft - ActionInset));

            Color accent = _palette.Accent(role, EditorGUIUtility.isProSkin);
            Color previous = GUI.backgroundColor;

            GUI.backgroundColor = new Color(accent.r, accent.g, accent.b, 0.45f);

            bool pressed = GUILayout.Button(label, ActionStyle);

            GUI.backgroundColor = previous;

            GUILayout.Space(ActionInset - InspectorInsetRight);
            EditorGUILayout.EndHorizontal();

            return pressed;
        }

        private GUIStyle ActionStyle => _action ??= new GUIStyle(GUI.skin.button)
        {
            margin = new RectOffset(0, 0, 2, 4),
            fixedHeight = 36f
        };

        public bool Toggle(Type owner, string member, string label, bool value)
        {
            Rect content = Row(owner, member);
            bool result = EditorGUI.ToggleLeft(content, label, value);

            HelpBox(owner, member);

            return result;
        }

        public int IntField(Type owner, string member, string label, int value)
        {
            Rect content = Row(owner, member);
            int result = EditorGUI.IntField(content, label, value);

            HelpBox(owner, member);

            return result;
        }

        /// <summary>
        /// One phase of a Root's life: the toggle that says whether it happens by itself, and - in
        /// play mode only - what has happened and a way to make it happen now. Out of play mode a
        /// badge saying "not yet" and a button that can never be pressed are noise.
        /// </summary>
        public bool Phase(Type owner, string member, string label, bool value, bool done, bool runnable,
            out bool run)
        {
            const float badgeWidth = 62f;
            const float runWidth = 24f;

            Rect content = Row(owner, member);
            var togglePart = new Rect(content.x, content.y, content.width - badgeWidth - runWidth - 8f, content.height);

            bool result = EditorGUI.ToggleLeft(togglePart, label, value);

            run = false;

            if (Application.isPlaying)
            {
                var badge = new Rect(content.xMax - badgeWidth - runWidth - 4f, content.y, badgeWidth, content.height);

                Color previous = GUI.color;
                GUI.color = done ? new Color(0.44f, 0.82f, 0.50f) : new Color(0.62f, 0.62f, 0.62f);
                GUI.Label(badge, done ? "● done" : "○ waiting", EditorStyles.miniLabel);
                GUI.color = previous;

                using (new EditorGUI.DisabledScope(!runnable))
                {
                    var button = new Rect(content.xMax - runWidth, content.y, runWidth, content.height);
                    run = GUI.Button(button, "▶", EditorStyles.miniButton);
                }
            }

            HelpBox(owner, member);

            return result;
        }

        public void Property(Type owner, SerializedProperty property)
        {
            float height = EditorGUI.GetPropertyHeight(property, true);
            Rect row = EditorGUILayout.GetControlRect(true, height);
            var content = new Rect(row.x + Gutter, row.y, row.width - Gutter, row.height);

            QuestionButton(owner, property.name, new Rect(row.x, row.y, Gutter, EditorGUIUtility.singleLineHeight));
            EditorGUI.PropertyField(content, property, true);

            HelpBox(owner, property.name);
        }

        /// <summary>
        /// The open help under a row. Public because Odin draws the row itself and calls back here
        /// for the box alone.
        /// </summary>
        public void HelpBox(Type owner, string member)
        {
            if (!_state.IsOpen(owner, member))
                return;

            string text = _help.For(owner, member);

            if (string.IsNullOrEmpty(text))
                return;

            Color accent = Accent(owner);
            var content = new GUIContent(text);
            float width = EditorGUIUtility.currentViewWidth - Gutter - 40f;
            float height = EditorStyles.wordWrappedMiniLabel.CalcHeight(content, width) + 8f;

            Rect rect = EditorGUILayout.GetControlRect(false, height);
            rect = new Rect(rect.x + Gutter, rect.y, rect.width - Gutter, rect.height);

            EditorGUI.DrawRect(rect, new Color(accent.r, accent.g, accent.b, EditorGUIUtility.isProSkin ? 0.12f : 0.10f));
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, 3f, rect.height), accent);

            var textRect = new Rect(rect.x + 9f, rect.y + 4f, rect.width - 15f, rect.height - 8f);
            GUI.Label(textRect, content, EditorStyles.wordWrappedMiniLabel);
        }

        /// <summary>
        /// Lays out one single-line row and draws its help button, answering with what is left for
        /// the field itself.
        /// </summary>
        private Rect Row(Type owner, string member)
        {
            Rect row = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight);

            QuestionButton(owner, member, new Rect(row.x, row.y, Gutter, row.height));

            return new Rect(row.x + Gutter, row.y, row.width - Gutter, row.height);
        }

        /// <summary>
        /// The button is only drawn where there is something to say; the gutter is left in place
        /// either way, so every label in the component starts at the same x.
        /// </summary>
        public void QuestionButton(Type owner, string member, Rect gutter)
        {
            if (string.IsNullOrEmpty(_help.For(owner, member)))
                return;

            bool open = _state.IsOpen(owner, member);
            var button = new Rect(gutter.x, gutter.y + 2f, ButtonSize, ButtonSize);

            Color previous = GUI.color;
            GUI.color = open ? Accent(owner) : new Color(1f, 1f, 1f, 0.45f);

            if (GUI.Button(button, new GUIContent("?", "What this does"), QuestionStyle()))
                _state.SetOpen(owner, member, !open);

            GUI.color = previous;
        }

        private Color Accent(Type owner)
        {
            return _roles.TryResolve(owner, out FlowRole role)
                ? _palette.Accent(role, EditorGUIUtility.isProSkin)
                : Color.grey;
        }

        private GUIStyle QuestionStyle()
        {
            return _question ??= new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 10,
                fontStyle = FontStyle.Bold
            };
        }
    }
}

#endif