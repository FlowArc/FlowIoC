#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace FlowIoC.Editor.CodeGenerator.Menus
{
    /// <summary>
    /// The name field of a single-file generator and, beside it, what the name is about to
    /// produce: the same warning mark and the word Preview that Create Module puts at the top
    /// right of its own window, then the class name in the same yellow. The two stand in the two
    /// columns Create Module's name and preview stand in, so the four generators read as the
    /// module generator at a smaller scale.
    ///
    /// While there is no name the panel is the error that says so, and the rest of the window
    /// stays off until there is one - the same contract Create Module keeps, and the reason the
    /// button under the window cannot be pressed on an empty name.
    /// </summary>
    internal class GeneratorNamePreview
    {
        private const string YELLOW = "#ffdd00ff";

        /// <summary>How much of the width the field takes; the preview has the rest.</summary>
        private const float FIELD_SHARE = 0.45f;

        private const float COLUMNS_SPACING = 8f;

        /// <summary>The error box's height: the label over the field and the field itself, together.</summary>
        private const float MISSING_HEIGHT = 38f;

        private GUIStyle _text;

        /// <summary>
        /// Draws the labelled field and returns what is in it. The preview beside it appears
        /// with the first character; until then the panel asks for the name, and leaves
        /// GUI.enabled off for whatever the window draws after it.
        /// </summary>
        /// <param name="label">What the field asks for.</param>
        /// <param name="typed">What is in the field.</param>
        /// <param name="name">The class the file will declare, given what was typed.</param>
        /// <param name="detailCaption">
        /// What a third line of the box is called, for a generator whose class name is not the
        /// whole answer - "Base Type:" for a function. Null for the rest.
        /// </param>
        /// <param name="detail">What that third line says.</param>
        public string Draw(string label, string typed, string name, string detailCaption = null, string detail = null)
        {
            float fieldWidth = Mathf.Round(EditorGUIUtility.currentViewWidth * FIELD_SHARE);

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.BeginVertical(GUILayout.Width(fieldWidth));
            EditorGUILayout.LabelField(label);
            typed = EditorGUILayout.TextField(typed);
            EditorGUILayout.EndVertical();

            GUILayout.Space(COLUMNS_SPACING);

            EditorGUILayout.BeginVertical();

            if (string.IsNullOrEmpty(typed)) DrawMissing(label);
            else DrawPreview(name, detailCaption, detail);

            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();

            GUI.enabled = !string.IsNullOrEmpty(typed);

            return typed;
        }

        /// <summary>
        /// The red box Create Module shows for an empty name, asking for this window's: the label
        /// with its colon gone and its case dropped, so "View Name:" asks for the view name.
        /// </summary>
        private void DrawMissing(string label)
        {
            string what = label.TrimEnd(':', ' ').ToLowerInvariant();

            Color previous = GUI.backgroundColor;
            GUI.backgroundColor = Color.red;

            var box = new GUIStyle(EditorStyles.helpBox) {padding = new RectOffset(4, 4, 0, 0)};

            EditorGUILayout.BeginHorizontal(box, GUILayout.Height(MISSING_HEIGHT));

            // A little top margin under the icon: an image in a style of its own is drawn from
            // the top of its rect whatever the alignment says, so it needs pushing onto the line
            // the words sit on.
            var icon = new GUIStyle(GUIStyle.none)
            {
                alignment = TextAnchor.MiddleCenter,
                margin = new RectOffset(0, 4, 2, 0),
                padding = new RectOffset(0, 0, 0, 0)
            };

            GUILayout.Label(EditorGUIUtility.IconContent("console.erroricon"), icon,
                GUILayout.Width(30), GUILayout.Height(MISSING_HEIGHT));
            EditorGUILayout.LabelField("<size=12>Please, enter <b>" + what + "</b>!</size>", Text,
                GUILayout.Height(MISSING_HEIGHT));

            EditorGUILayout.EndHorizontal();

            GUI.backgroundColor = previous;
        }

        private void DrawPreview(string name, string detailCaption, string detail)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(EditorGUIUtility.IconContent("console.warnicon"), GUILayout.Width(18), GUILayout.Height(13));
            EditorGUILayout.LabelField($"<size=10><color={YELLOW}>Preview</color></size>", Text, GUILayout.Height(13));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField(
                "<size=10>Class Name:</size> <size=12><color=" + YELLOW + "><u>" + name + "</u></color></size>",
                Text, GUILayout.Height(15));

            if (!string.IsNullOrEmpty(detail))
            {
                EditorGUILayout.LabelField(
                    "<size=10>" + detailCaption + "</size> <size=11>" + detail + "</size>", Text, GUILayout.Height(15));
            }

            EditorGUILayout.EndVertical();
        }

        /// <summary>Built on use: EditorStyles is not loaded when a window's fields are.</summary>
        private GUIStyle Text => _text ??= new GUIStyle(EditorStyles.whiteLabel)
        {
            alignment = TextAnchor.MiddleLeft,
            richText = true,
            margin = new RectOffset(0, 0, 0, 0),
            padding = new RectOffset(0, 0, 0, 0)
        };
    }
}

#endif