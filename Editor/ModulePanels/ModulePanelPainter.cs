#if UNITY_EDITOR

using System;
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
    /// A labelled row may carry help - what the field means, what the buttons do - and the help
    /// is not written under the row: the row gets the same "?" a Root's inspector gives its
    /// fields, and the text opens under it when the "?" is pressed. Every labelled row keeps the
    /// gutter the "?" sits in, so the labels line up whether or not a row has something to say.
    /// Which help is open is remembered the inspector's way, by panel and label, for the session.
    ///
    /// A row that only reads does not light up under the pointer: nothing happens when it is
    /// clicked, and a highlight that promises a click would be a lie. The buttons and the "?"
    /// are the only things here that respond.
    /// </summary>
    public class ModulePanelPainter
    {
        private const float LABEL_WIDTH = 150f;
        private const float ACTION_HEIGHT = 26f;
        private const float BUTTON_HEIGHT = 18f;
        private const float ADD_HEIGHT = 22f;
        private const float ADD_PADDING = 28f;
        private const float BUTTON_PADDING = 16f;
        private const float BUTTON_GAP = 4f;
        private const float RIGHT_MARGIN = 8f;
        private const float TEXT_PADDING = 4f;
        private const float HELP_GUTTER = 16f;
        private const float ASIDE_FIELD_WIDTH = 56f;
        private const float ASIDE_GAP = 12f;
        private const float QUESTION_SIZE = 13f;

        private readonly FlowRowPainter _rows;
        private readonly FlowPalette _palette;
        private readonly Color _accent;
        private readonly Type _panel;
        private readonly FlowHelpState _state;

        private GUIStyle _question;

        internal ModulePanelPainter(FlowRowPainter rows, FlowPalette palette, Color accent, Type panel)
        {
            _rows = rows;
            _palette = palette;
            _accent = accent;
            _panel = panel;
            _state = new FlowHelpState();
        }

        /// <summary>A section's name, on a row tinted in the panel's colour.</summary>
        public void Heading(string text)
        {
            Rect rect = _rows.Row();
            _rows.Paint(rect, _accent, FlowRowPainter.HEADING_ALPHA);

            GUI.Label(Content(rect), text, _rows.Strong(false));
        }

        /// <summary>
        /// A section's name with its own buttons on the same row, pressed against the right edge in
        /// the order given - the one or two things done to the section as a whole, such as selecting
        /// the asset it is read from, so they sit beside its name rather than in a row below it.
        /// </summary>
        public void Heading(string text, params ModulePanelAction[] actions)
        {
            Rect rect = _rows.Row();
            _rows.Paint(rect, _accent, FlowRowPainter.HEADING_ALPHA);

            Rect content = Content(rect);
            float y = rect.y + (rect.height - BUTTON_HEIGHT) / 2f;
            float x = content.xMax;

            for (int index = actions.Length - 1; index >= 0; index--)
            {
                float width = ButtonWidth(actions[index]);
                x -= width;
                Button(new Rect(x, y, width, BUTTON_HEIGHT), actions[index]);
                x -= BUTTON_GAP;
            }

            content.width = Mathf.Max(0f, x - content.x);
            GUI.Label(content, text, _rows.Strong(false));
        }

        /// <summary>A value the reader looks at: its name on the left, the value beside it.</summary>
        public void Field(string label, string value, string help = null)
        {
            Rect rect = _rows.Row();
            _rows.Paint(rect, _accent, FlowRowPainter.QUIET_ALPHA);

            Rect content = Labelled(rect, label, help);
            GUI.Label(new Rect(content.x, content.y, LABEL_WIDTH, content.height), label, _rows.Name(false));
            GUI.Label(ValueRect(content), value, _rows.Cell(false));

            Help(label, help);
        }

        /// <summary>Something the reader should know, in the quiet grey a note is read in.</summary>
        public void Note(string text)
        {
            GUIStyle style = _rows.MutedWrapped();
            Rect rect = _rows.Row(HeightOf(style, text, 0f));
            _rows.PaintShaded(rect, _rows.Guide);

            GUI.Label(Content(rect), text, style);
        }

        /// <summary>Something that stops the reader - the buttons are off while the game plays, say.</summary>
        public void Warning(string text)
        {
            GUIStyle style = _rows.NameWrapped(false);
            Rect rect = _rows.Row(HeightOf(style, text, 0f));
            _rows.Paint(rect, _rows.Warn);

            GUI.Label(Content(rect), text, style);
        }

        /// <summary>
        /// A value the reader types: a password, a name. Returns what the field holds after this
        /// repaint, the way every IMGUI field does.
        /// </summary>
        public string TextField(string label, string value, bool secret = false, string help = null)
        {
            Rect rect = _rows.Row(ACTION_HEIGHT);
            _rows.Paint(rect, _accent, FlowRowPainter.QUIET_ALPHA);

            Rect content = Labelled(rect, label, help);
            GUI.Label(new Rect(content.x, content.y, LABEL_WIDTH, content.height), label, _rows.Name(false));

            Rect field = ValueRect(content);
            field.y += (content.height - EditorGUIUtility.singleLineHeight) / 2f;
            field.height = EditorGUIUtility.singleLineHeight;

            string typed = secret ? EditorGUI.PasswordField(field, value) : EditorGUI.TextField(field, value);

            Help(label, help);

            return typed;
        }

        /// <summary>
        /// One choice out of a list - which test is active, say. Returns the index the field holds
        /// after this repaint, the way every IMGUI field does; the panel decides what a change means.
        /// </summary>
        public int Popup(string label, int selected, string[] options, string help = null)
        {
            Rect rect = _rows.Row(ACTION_HEIGHT);
            _rows.Paint(rect, _accent, FlowRowPainter.QUIET_ALPHA);

            Rect content = Labelled(rect, label, help);
            GUI.Label(new Rect(content.x, content.y, LABEL_WIDTH, content.height), label, _rows.Name(false));

            Rect field = ValueRect(content);
            field.y += (content.height - EditorGUIUtility.singleLineHeight) / 2f;
            field.height = EditorGUIUtility.singleLineHeight;

            int picked = EditorGUI.Popup(field, selected, options);

            Help(label, help);

            return picked;
        }

        /// <summary>
        /// A block of text to read and copy from, never to edit - the save file as it is on disk.
        /// Selectable, so a line can be copied out; not a text area, so nothing typed here goes
        /// anywhere.
        /// </summary>
        public void Text(string text)
        {
            GUIStyle style = _rows.NameWrapped(false);
            Rect rect = _rows.Row(HeightOf(style, text, 0f) + TEXT_PADDING * 2f);
            _rows.Paint(rect, _accent, FlowRowPainter.QUIET_ALPHA);

            Rect content = Content(rect);
            content.y += TEXT_PADDING;
            content.height -= TEXT_PADDING * 2f;

            EditorGUI.SelectableLabel(content, text, style);
        }

        /// <summary>
        /// A row of buttons. The plain, confirming and cautioning ones are mini buttons laid out
        /// from the left in the order they are given, the last two tinted green and amber. The ones
        /// that add or destroy something are pressed against the right edge, apart from the rest:
        /// an add button larger and green, so the way to make more is found without reading the
        /// row, a destructive one mini and red, so the hand slows before it. A disabled one stays in
        /// its place greyed rather than disappearing, so the panel does not rearrange itself as the
        /// state changes.
        /// </summary>
        public void Actions(params ModulePanelAction[] actions) => Actions(null, actions);

        /// <summary>The same row of buttons, with help on what they do behind its "?".</summary>
        public void Actions(string help, params ModulePanelAction[] actions)
        {
            Rect rect = _rows.Row(ACTION_HEIGHT);
            _rows.Paint(rect, _accent, FlowRowPainter.QUIET_ALPHA);

            string key = actions.Length > 0 ? actions[0].Label : string.Empty;
            Rect content = Labelled(rect, key, help);
            float x = content.x;
            float y = rect.y + (rect.height - BUTTON_HEIGHT) / 2f;

            foreach (ModulePanelAction action in actions)
            {
                if (StandsApart(action))
                    continue;

                float width = ButtonWidth(action);
                Button(new Rect(x, y, width, BUTTON_HEIGHT), action);
                x += width + BUTTON_GAP;
            }

            float right = content.xMax;

            for (int index = actions.Length - 1; index >= 0; index--)
            {
                ModulePanelAction action = actions[index];

                if (!StandsApart(action))
                    continue;

                bool adds = action.Kind == ModulePanelActionKind.Add;
                float height = adds ? ADD_HEIGHT : BUTTON_HEIGHT;
                float width = adds
                    ? GUI.skin.button.CalcSize(new GUIContent(action.Label)).x + ADD_PADDING
                    : ButtonWidth(action);

                right -= width;
                Button(new Rect(right, rect.y + (rect.height - height) / 2f, width, height), action,
                    adds ? GUI.skin.button : EditorStyles.miniButton);
                right -= BUTTON_GAP;
            }

            Help(key, help);
        }

        /// <summary>
        /// One serialized field to edit, drawn the way the Inspector draws it - a slider for a
        /// [Range], an object picker for a reference - so undo, dirtying and saving are Unity's.
        /// The label is the property's own unless the panel says otherwise. This is the mark a
        /// panel that authors an asset is made of: it edits what the Inspector would edit, not a
        /// Model's values.
        /// </summary>
        public void Property(SerializedProperty property, string label = null, string help = null)
        {
            Properties(label ?? property.displayName, help, property);
        }

        /// <summary>
        /// One field with a small second one at the right end of its row - an id with its version
        /// beside it: the aside takes a short label and a narrow field, the first field the rest, so
        /// a number that is glanced at costs no row of its own.
        /// </summary>
        public void PropertyWithAside(SerializedProperty property, string label, SerializedProperty aside,
            string asideLabel, string help = null)
        {
            Rect rect = _rows.Row(ACTION_HEIGHT);
            _rows.Paint(rect, _accent, FlowRowPainter.QUIET_ALPHA);

            Rect content = Labelled(rect, label, help);
            GUI.Label(new Rect(content.x, content.y, LABEL_WIDTH, content.height), label, _rows.Name(false));

            Rect value = ValueRect(content);
            value.y += (content.height - EditorGUIUtility.singleLineHeight) / 2f;
            value.height = EditorGUIUtility.singleLineHeight;

            GUIStyle mini = _rows.Mini(false);
            float asideLabelWidth = mini.CalcSize(new GUIContent(asideLabel)).x;
            float asideWidth = asideLabelWidth + BUTTON_GAP + ASIDE_FIELD_WIDTH;

            EditorGUI.PropertyField(new Rect(value.x, value.y, value.width - asideWidth - ASIDE_GAP, value.height),
                property, GUIContent.none);
            GUI.Label(new Rect(value.xMax - asideWidth, value.y, asideLabelWidth, value.height), asideLabel, mini);
            EditorGUI.PropertyField(new Rect(value.xMax - ASIDE_FIELD_WIDTH, value.y, ASIDE_FIELD_WIDTH, value.height),
                aside, GUIContent.none);

            Help(label, help);
        }

        /// <summary>
        /// Several fields on one row under one label, sharing the value column - the cells of one
        /// row of a matrix side by side. Each field draws without a label of its own.
        /// </summary>
        public void Properties(string label, params SerializedProperty[] properties) =>
            Properties(label, null, properties);

        /// <summary>The same row of fields, with help on what they hold behind its "?".</summary>
        public void Properties(string label, string help, params SerializedProperty[] properties)
        {
            Rect rect = _rows.Row(ACTION_HEIGHT);
            _rows.Paint(rect, _accent, FlowRowPainter.QUIET_ALPHA);

            Rect content = Labelled(rect, label, help);
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

            Help(label, help);
        }

        public void Space() => GUILayout.Space(6f);

        /// <summary>
        /// One row the panel draws itself - a mock of a notification card, a swatch, a picture of
        /// a thing no mark here describes. The row is painted and lined up like every other, and the
        /// panel is handed the rect inside it, past the stripe and short of the right margin. This
        /// is the one mark that gives a rect away, so it is the one to reach for last: a labelled
        /// value, a note or a button is one of the marks above.
        /// </summary>
        public void Custom(float height, Action<Rect> draw)
        {
            Rect rect = _rows.Row(height);
            _rows.Paint(rect, _accent, FlowRowPainter.QUIET_ALPHA);

            draw?.Invoke(Content(rect));
        }

        private static float ButtonWidth(ModulePanelAction action) =>
            EditorStyles.miniButton.CalcSize(new GUIContent(action.Label)).x + BUTTON_PADDING;

        /// <summary>The buttons that add and destroy stand apart at the right edge; the rest stay in line.</summary>
        private static bool StandsApart(ModulePanelAction action) =>
            action.Kind == ModulePanelActionKind.Add || action.Kind == ModulePanelActionKind.Remove;

        /// <summary>One button: greyed when it cannot be pressed, tinted by what it does.</summary>
        private void Button(Rect rect, ModulePanelAction action, GUIStyle style = null)
        {
            Color previous = GUI.backgroundColor;

            switch (action.Kind)
            {
                case ModulePanelActionKind.Add:
                case ModulePanelActionKind.Confirm:
                    GUI.backgroundColor = _palette.ActionAdd;
                    break;
                case ModulePanelActionKind.Remove:
                    GUI.backgroundColor = _palette.ActionRemove;
                    break;
                case ModulePanelActionKind.Caution:
                    GUI.backgroundColor = _palette.ActionCaution;
                    break;
            }

            using (new EditorGUI.DisabledScope(!action.Enabled || action.OnClick == null))
            {
                if (GUI.Button(rect, action.Label, style ?? EditorStyles.miniButton))
                    action.OnClick();
            }

            GUI.backgroundColor = previous;
        }

        /// <summary>
        /// A labelled row's content past the help gutter, with the "?" drawn in the gutter where
        /// the row has help to show. The gutter is kept either way, so every label starts at the
        /// same x.
        /// </summary>
        private Rect Labelled(Rect row, string key, string help)
        {
            Rect content = Content(row);

            if (!string.IsNullOrEmpty(help))
                Question(new Rect(content.x, content.y, HELP_GUTTER, content.height), key);

            return new Rect(content.x + HELP_GUTTER, content.y, content.width - HELP_GUTTER, content.height);
        }

        private void Question(Rect gutter, string key)
        {
            bool open = _state.IsOpen(_panel, key);
            var button = new Rect(gutter.x, gutter.y + (gutter.height - QUESTION_SIZE) / 2f, QUESTION_SIZE, QUESTION_SIZE);

            Color previous = GUI.color;
            GUI.color = open ? _accent : new Color(1f, 1f, 1f, 0.45f);

            if (GUI.Button(button, new GUIContent("?", "What this does"), QuestionStyle()))
                _state.SetOpen(_panel, key, !open);

            GUI.color = previous;
        }

        /// <summary>The help under a row, drawn only while its "?" is open, indented to the label.</summary>
        private void Help(string key, string help)
        {
            if (string.IsNullOrEmpty(help) || !_state.IsOpen(_panel, key))
                return;

            GUIStyle style = _rows.MutedWrapped();
            Rect rect = _rows.Row(HeightOf(style, help, HELP_GUTTER));
            _rows.PaintShaded(rect, _rows.Guide);

            Rect content = Content(rect);
            GUI.Label(new Rect(content.x + HELP_GUTTER, content.y, content.width - HELP_GUTTER, content.height), help, style);
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

        private Rect Content(Rect row) =>
            new Rect(row.x + _rows.ContentX, row.y, row.width - _rows.ContentX - RIGHT_MARGIN, row.height);

        private static Rect ValueRect(Rect content) =>
            new Rect(content.x + LABEL_WIDTH, content.y, content.width - LABEL_WIDTH, content.height);

        /// <summary>
        /// The height a wrapped text needs at the window's width less an indent, and never less
        /// than a row, so a one-line note sits on the same grid as the rows around it.
        /// </summary>
        private float HeightOf(GUIStyle style, string text, float indent)
        {
            float width = EditorGUIUtility.currentViewWidth - _rows.ContentX - RIGHT_MARGIN - indent;

            return Mathf.Max(FlowRowPainter.ROW_HEIGHT, style.CalcHeight(new GUIContent(text), width) + 2f);
        }
    }
}

#endif
