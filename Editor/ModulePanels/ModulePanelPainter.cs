#if UNITY_EDITOR

using FlowIoC.Editor.Inspector;
using UnityEditor;
using UnityEngine;

namespace FlowIoC.Editor.ModulePanels
{
    /// <summary>
    /// The marks a module panel is drawn with, over the row painter every FlowIoC window uses:
    /// a heading, a labelled value, a note, a warning, a field to type in, a block of text to
    /// read, and a row of buttons. A panel says what it is made of in these and never touches
    /// a rect, so a panel from any package lines up with the bar over it the way the framework's
    /// own windows do.
    ///
    /// A row that only reads does not light up under the pointer: nothing happens when it is
    /// clicked, and a highlight that promises a click would be a lie. The buttons are the only
    /// things here that respond.
    /// </summary>
    public class ModulePanelPainter
    {
        private const float LABEL_WIDTH = 150f;
        private const float ACTION_HEIGHT = 26f;
        private const float BUTTON_HEIGHT = 18f;
        private const float BUTTON_PADDING = 16f;
        private const float BUTTON_GAP = 4f;
        private const float RIGHT_MARGIN = 8f;
        private const float TEXT_PADDING = 4f;

        private readonly FlowRowPainter _rows;
        private readonly FlowPalette _palette;
        private readonly Color _accent;

        internal ModulePanelPainter(FlowRowPainter rows, FlowPalette palette, Color accent)
        {
            _rows = rows;
            _palette = palette;
            _accent = accent;
        }

        /// <summary>A section's name, on a row tinted in the panel's colour.</summary>
        public void Heading(string text)
        {
            Rect rect = _rows.Row();
            _rows.Paint(rect, _accent, FlowRowPainter.HEADING_ALPHA);

            GUI.Label(Content(rect), text, _rows.Strong(false));
        }

        /// <summary>A value the reader looks at: its name on the left, the value beside it.</summary>
        public void Field(string label, string value)
        {
            Rect rect = _rows.Row();
            _rows.Paint(rect, _accent, FlowRowPainter.QUIET_ALPHA);

            Rect content = Content(rect);
            GUI.Label(new Rect(content.x, content.y, LABEL_WIDTH, content.height), label, _rows.Name(false));
            GUI.Label(ValueRect(content), value, _rows.Cell(false));
        }

        /// <summary>Something the reader should know, in the quiet grey a note is read in.</summary>
        public void Note(string text)
        {
            GUIStyle style = _rows.MutedWrapped();
            Rect rect = _rows.Row(HeightOf(style, text));
            _rows.PaintShaded(rect, _rows.Guide);

            GUI.Label(Content(rect), text, style);
        }

        /// <summary>Something that stops the reader - the buttons are off while the game plays, say.</summary>
        public void Warning(string text)
        {
            GUIStyle style = _rows.NameWrapped(false);
            Rect rect = _rows.Row(HeightOf(style, text));
            _rows.Paint(rect, _rows.Warn);

            GUI.Label(Content(rect), text, style);
        }

        /// <summary>
        /// A value the reader types: a password, a name. Returns what the field holds after this
        /// repaint, the way every IMGUI field does.
        /// </summary>
        public string TextField(string label, string value, bool secret = false)
        {
            Rect rect = _rows.Row(ACTION_HEIGHT);
            _rows.Paint(rect, _accent, FlowRowPainter.QUIET_ALPHA);

            Rect content = Content(rect);
            GUI.Label(new Rect(content.x, content.y, LABEL_WIDTH, content.height), label, _rows.Name(false));

            Rect field = ValueRect(content);
            field.y += (content.height - EditorGUIUtility.singleLineHeight) / 2f;
            field.height = EditorGUIUtility.singleLineHeight;

            return secret ? EditorGUI.PasswordField(field, value) : EditorGUI.TextField(field, value);
        }

        /// <summary>
        /// A block of text to read and copy from, never to edit - the save file as it is on disk.
        /// Selectable, so a line can be copied out; not a text area, so nothing typed here goes
        /// anywhere.
        /// </summary>
        public void Text(string text)
        {
            GUIStyle style = _rows.NameWrapped(false);
            Rect rect = _rows.Row(HeightOf(style, text) + TEXT_PADDING * 2f);
            _rows.Paint(rect, _accent, FlowRowPainter.QUIET_ALPHA);

            Rect content = Content(rect);
            content.y += TEXT_PADDING;
            content.height -= TEXT_PADDING * 2f;

            EditorGUI.SelectableLabel(content, text, style);
        }

        /// <summary>
        /// A row of buttons, laid out from the left in the order they are given. A disabled one
        /// stays in its place greyed rather than disappearing, so the panel does not rearrange
        /// itself as the state changes; a destructive one is tinted.
        /// </summary>
        public void Actions(params ModulePanelAction[] actions)
        {
            Rect rect = _rows.Row(ACTION_HEIGHT);
            _rows.Paint(rect, _accent, FlowRowPainter.QUIET_ALPHA);

            float x = rect.x + _rows.ContentX;
            float y = rect.y + (rect.height - BUTTON_HEIGHT) / 2f;

            foreach (ModulePanelAction action in actions)
            {
                var content = new GUIContent(action.Label);
                float width = EditorStyles.miniButton.CalcSize(content).x + BUTTON_PADDING;
                var button = new Rect(x, y, width, BUTTON_HEIGHT);

                Color previous = GUI.backgroundColor;

                if (action.Destructive)
                    GUI.backgroundColor = _palette.ActionRemove;

                using (new EditorGUI.DisabledScope(!action.Enabled || action.OnClick == null))
                {
                    if (GUI.Button(button, content, EditorStyles.miniButton))
                        action.OnClick();
                }

                GUI.backgroundColor = previous;
                x += width + BUTTON_GAP;
            }
        }

        /// <summary>
        /// One serialized field to edit, drawn the way the Inspector draws it - a slider for a
        /// [Range], an object picker for a reference - so undo, dirtying and saving are Unity's.
        /// The label is the property's own unless the panel says otherwise. This is the mark a
        /// panel that authors an asset is made of: it edits what the Inspector would edit, not a
        /// Model's values.
        /// </summary>
        public void Property(SerializedProperty property, string label = null)
        {
            Properties(label ?? property.displayName, property);
        }

        /// <summary>
        /// Several fields on one row under one label, sharing the value column - an override's
        /// original and its variant side by side. Each field draws without a label of its own.
        /// </summary>
        public void Properties(string label, params SerializedProperty[] properties)
        {
            Rect rect = _rows.Row(ACTION_HEIGHT);
            _rows.Paint(rect, _accent, FlowRowPainter.QUIET_ALPHA);

            Rect content = Content(rect);
            GUI.Label(new Rect(content.x, content.y, LABEL_WIDTH, content.height), label, _rows.Name(false));

            Rect value = ValueRect(content);
            value.y += (content.height - EditorGUIUtility.singleLineHeight) / 2f;
            value.height = EditorGUIUtility.singleLineHeight;

            float width = (value.width - BUTTON_GAP * (properties.Length - 1)) / properties.Length;

            for (int index = 0; index < properties.Length; index++)
            {
                var field = new Rect(value.x + index * (width + BUTTON_GAP), value.y, width, value.height);
                EditorGUI.PropertyField(field, properties[index], GUIContent.none);
            }
        }

        public void Space() => GUILayout.Space(6f);

        private Rect Content(Rect row) =>
            new Rect(row.x + _rows.ContentX, row.y, row.width - _rows.ContentX - RIGHT_MARGIN, row.height);

        private static Rect ValueRect(Rect content) =>
            new Rect(content.x + LABEL_WIDTH, content.y, content.width - LABEL_WIDTH, content.height);

        /// <summary>
        /// The height a wrapped text needs at the window's width, and never less than a row, so a
        /// one-line note sits on the same grid as the rows around it.
        /// </summary>
        private float HeightOf(GUIStyle style, string text)
        {
            float width = EditorGUIUtility.currentViewWidth - _rows.ContentX - RIGHT_MARGIN;

            return Mathf.Max(FlowRowPainter.ROW_HEIGHT, style.CalcHeight(new GUIContent(text), width) + 2f);
        }
    }
}

#endif