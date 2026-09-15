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
        private const float SQUARE = 18f;
        private const float FLAG_ALPHA = 0.28f;
        private const float HEADING_HEIGHT = FlowRowPainter.ROW_HEIGHT * 2f;
        private const float QUESTION_SIZE = 13f;

        private readonly FlowRowPainter _rows;
        private readonly FlowPalette _palette;
        private readonly Color _accent;
        private readonly Type _panel;
        private readonly FlowHelpState _state;

        private GUIStyle _question;

        /// <summary>
        /// The width of the last row painted, learnt on repaint. A wrapped text's height has to be
        /// computed before its row exists, and the window's width is the wrong number when the rows
        /// sit beside a sidebar or a scrollbar: a text wrapped at the real width but given the
        /// height of a wider one runs past its row.
        /// </summary>
        private float _rowWidth;
        private GUIStyle _square;

        internal ModulePanelPainter(FlowRowPainter rows, FlowPalette palette, Color accent, Type panel)
        {
            _rows = rows;
            _palette = palette;
            _accent = accent;
            _panel = panel;
            _state = new FlowHelpState();
        }

        /// <summary>
        /// A section's name, drawn the way the bar's strip names the module and the sidebar names
        /// its list: a band taken down a step, the words in the panel's colour and in capitals. It
        /// is nothing like a row, so a section is read as a title over rows rather than as one more
        /// row that happens to be lit.
        /// </summary>
        public void Heading(string text)
        {
            Rect rect = Row(HEADING_HEIGHT);
            PaintHeading(rect);

            GUI.Label(Content(rect), text.ToUpperInvariant(), _rows.Heading(_accent));
        }

        /// <summary>
        /// A section's name with its own buttons on the same row, pressed against the right edge in
        /// the order given - the one or two things done to the section as a whole, such as making
        /// it the active one or deleting it, so they sit beside its name rather than in a row below
        /// it. A label that is one glyph - "-", "+" - is drawn as a small square rather than a word.
        /// </summary>
        public void Heading(string text, params ModulePanelAction[] actions)
        {
            Rect rect = Row(HEADING_HEIGHT);
            PaintHeading(rect);

            Rect content = Content(rect);
            float x = content.xMax;

            for (int index = actions.Length - 1; index >= 0; index--)
            {
                ModulePanelAction action = actions[index];
                bool glyph = action.Label != null && action.Label.Length == 1;
                float width = glyph ? SQUARE : ButtonWidth(action);
                float height = glyph ? SQUARE : BUTTON_HEIGHT;

                x -= width;
                Button(new Rect(x, rect.y + (rect.height - height) / 2f, width, height), action, glyph ? SquareStyle() : null);
                x -= BUTTON_GAP;
            }

            content.width = Mathf.Max(0f, x - content.x);
            GUI.Label(content, text.ToUpperInvariant(), _rows.Heading(_accent));
        }

        /// <summary>The quiet tint of a row taken down a step, the way the bar's strip sits under its title.</summary>
        private void PaintHeading(Rect rect)
        {
            _rows.Paint(rect, _accent, FlowRowPainter.QUIET_ALPHA);
            _rows.Darken(rect);
        }

        /// <summary>A value the reader looks at: its name on the left, the value beside it.</summary>
        public void Field(string label, string value, string help = null)
        {
            Rect rect = Row();
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
            Rect rect = Row(HeightOf(style, text, 0f));
            _rows.PaintShaded(rect, _rows.Guide);

            GUI.Label(Centred(Content(rect), style, text), text, style);
        }

        /// <summary>Something that stops the reader - the buttons are off while the game plays, say.</summary>
        public void Warning(string text) => Message(text, _rows.Warn);

        /// <summary>Something that is wrong - a slot left empty, a name used twice - in the red an error row wears.</summary>
        public void Error(string text) => Message(text, _rows.Error);

        private void Message(string text, Color tint)
        {
            GUIStyle style = _rows.NameWrapped(false);
            Rect rect = Row(HeightOf(style, text, 0f));
            _rows.Paint(rect, tint);

            GUI.Label(Centred(Content(rect), style, text), text, style);
        }

        /// <summary>
        /// A value the reader types: a password, a name. Returns what the field holds after this
        /// repaint, the way every IMGUI field does.
        /// </summary>
        public string TextField(string label, string value, bool secret = false, string help = null)
        {
            Rect rect = Row(ACTION_HEIGHT);
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
            Rect rect = Row(ACTION_HEIGHT);
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
            Rect rect = Row(HeightOf(style, text, 0f) + TEXT_PADDING * 2f);
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
            Rect rect = Row(ACTION_HEIGHT);
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
            Rect rect = Row(ACTION_HEIGHT);
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
        public void Properties(string label, string help, params SerializedProperty[] properties) =>
            Properties(label, help, null, properties);

        /// <summary>
        /// The same row of fields with small square buttons at its right edge, in the order given -
        /// the "+" and "-" that add and take a column of the matrix the row heads, each tinted by
        /// its kind and its label a single glyph. A null entry draws nothing but keeps its square,
        /// so the rows of a matrix with fewer buttons than their heading keep their columns in line.
        /// Null or empty for none.
        /// </summary>
        public void Properties(string label, string help, ModulePanelAction?[] trailing, params SerializedProperty[] properties) =>
            Properties(label, help, trailing, null, properties);

        /// <summary>
        /// The same row with some of its fields flagged: a flagged field is washed and framed in the
        /// red an error row wears, so the eye lands on the empty slot rather than on the sentence
        /// about it. One flag per field, or null for none.
        /// </summary>
        public void Properties(string label, string help, ModulePanelAction?[] trailing, bool[] flagged,
            params SerializedProperty[] properties)
        {
            Rect rect = Row(ACTION_HEIGHT);
            _rows.Paint(rect, _accent, FlowRowPainter.QUIET_ALPHA);

            Rect content = Labelled(rect, label, help);
            GUI.Label(new Rect(content.x, content.y, LABEL_WIDTH, content.height), label, _rows.Name(false));

            Rect value = ValueRect(content);
            value.y += (content.height - EditorGUIUtility.singleLineHeight) / 2f;
            value.height = EditorGUIUtility.singleLineHeight;

            if (trailing != null && trailing.Length > 0)
            {
                float x = value.xMax;

                for (int index = trailing.Length - 1; index >= 0; index--)
                {
                    x -= SQUARE;

                    if (trailing[index].HasValue)
                        Button(new Rect(x, rect.y + (rect.height - SQUARE) / 2f, SQUARE, SQUARE), trailing[index].Value, SquareStyle());

                    x -= BUTTON_GAP;
                }

                value.width = x + BUTTON_GAP - ASIDE_GAP - value.x;
            }

            float width = (value.width - BUTTON_GAP * (properties.Length - 1)) / properties.Length;

            for (int index = 0; index < properties.Length; index++)
            {
                var field = new Rect(value.x + index * (width + BUTTON_GAP), value.y, width, value.height);
                EditorGUI.PropertyField(field, properties[index], GUIContent.none);

                if (flagged != null && index < flagged.Length && flagged[index])
                    Flag(field);
            }

            Help(label, help);
        }

        /// <summary>A red wash over a field and a hairline around it, drawn after the field so both show.</summary>
        private void Flag(Rect field)
        {
            Color red = _rows.Error;

            EditorGUI.DrawRect(field, new Color(red.r, red.g, red.b, FLAG_ALPHA));
            EditorGUI.DrawRect(new Rect(field.x, field.y, field.width, 1f), red);
            EditorGUI.DrawRect(new Rect(field.x, field.yMax - 1f, field.width, 1f), red);
            EditorGUI.DrawRect(new Rect(field.x, field.y, 1f, field.height), red);
            EditorGUI.DrawRect(new Rect(field.xMax - 1f, field.y, 1f, field.height), red);
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
            Rect rect = Row(height);
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
            Rect rect = Row(HeightOf(style, help, HELP_GUTTER));
            _rows.PaintShaded(rect, _rows.Guide);

            Rect content = Content(rect);
            var indented = new Rect(content.x + HELP_GUTTER, content.y, content.width - HELP_GUTTER, content.height);
            GUI.Label(Centred(indented, style, help), help, style);
        }

        /// <summary>
        /// A mini button that is exactly the rect it is given: the skin's mini button carries a fixed
        /// height and an overflow that would stretch a square into a low rectangle, so both are
        /// cleared, and the padding with them, so a single glyph sits centred.
        /// </summary>
        private GUIStyle SquareStyle()
        {
            return _square ??= new GUIStyle(EditorStyles.miniButton)
            {
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(0, 0, 0, 0),
                overflow = new RectOffset(0, 0, 0, 0),
                fixedHeight = 0f,
                fixedWidth = 0f,
                fontStyle = FontStyle.Bold
            };
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

        /// <summary>One row, its width remembered for the next wrapped text; on layout the rect is a stand-in and is not learnt from.</summary>
        private Rect Row(float height = FlowRowPainter.ROW_HEIGHT)
        {
            Rect rect = _rows.Row(height);

            if (Event.current.type == EventType.Repaint && rect.width > 1f)
                _rowWidth = rect.width;

            return rect;
        }

        private Rect Content(Rect row) =>
            new Rect(row.x + _rows.ContentX, row.y, row.width - _rows.ContentX - RIGHT_MARGIN, row.height);

        private static Rect ValueRect(Rect content) =>
            new Rect(content.x + LABEL_WIDTH, content.y, content.width - LABEL_WIDTH, content.height);

        /// <summary>
        /// The rect a wrapped text is drawn in, its height the text's own and centred in the row: a
        /// wrapped style anchors at the top so a long text reads from its first line down, and a
        /// one-line message would otherwise sit high in its row.
        /// </summary>
        private static Rect Centred(Rect content, GUIStyle style, string text)
        {
            float height = Mathf.Min(content.height, style.CalcHeight(new GUIContent(text), content.width));

            return new Rect(content.x, content.y + (content.height - height) / 2f, content.width, height);
        }

        /// <summary>
        /// The height a wrapped text needs at the window's width less an indent, and never less
        /// than a row, so a one-line note sits on the same grid as the rows around it.
        /// </summary>
        private float HeightOf(GUIStyle style, string text, float indent)
        {
            float total = _rowWidth > 0f ? _rowWidth : EditorGUIUtility.currentViewWidth;
            float width = total - _rows.ContentX - RIGHT_MARGIN - indent;

            return Mathf.Max(FlowRowPainter.ROW_HEIGHT, style.CalcHeight(new GUIContent(text), width) + 2f);
        }
    }
}

#endif
