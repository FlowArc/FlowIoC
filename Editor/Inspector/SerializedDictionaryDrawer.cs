#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace FlowIoC.Editor.Inspector
{
    /// <summary>
    /// Draws a SerializedDictionary as one list of pairs - the key on the left, the value on the
    /// right - instead of the two lists it stores. A pair is added and removed as one, so the two
    /// lists can never fall out of step.
    ///
    /// A key that is already in the dictionary is refused rather than written, and the refusal is
    /// shown under the list until the next key is accepted: the dictionary rebuilds itself from its
    /// two lists on every load and stops at the first repeated key, so a duplicate written here
    /// would take every pair after it with it the next time it saved.
    ///
    /// Only for UnityEngine.Rendering.SerializedDictionary, never for Dictionary itself: Unity 6.6
    /// draws a plain Dictionary field with its own editor, and this must not take that one over.
    /// </summary>
    [CustomPropertyDrawer(typeof(SerializedDictionary<,>), true)]
    [CustomPropertyDrawer(typeof(SerializedDictionary<,,,>), true)]
    public class SerializedDictionaryDrawer : PropertyDrawer
    {
        private const float REMOVE_WIDTH = 20f;
        private const float GAP = 4f;

        private readonly SerializedDictionaryPairs _pairs = new SerializedDictionaryPairs();

        /// <summary>The last refusal per dictionary, by the dictionary's property path.</summary>
        private readonly Dictionary<string, Refusal> _refusals = new Dictionary<string, Refusal>();

        private struct Refusal
        {
            public int Index;
            public string Message;
        }

        private static float Line => EditorGUIUtility.singleLineHeight;
        private static float Spacing => EditorGUIUtility.standardVerticalSpacing;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            SerializedProperty keys = property.FindPropertyRelative(SerializedDictionaryPairs.KEYS);
            SerializedProperty values = property.FindPropertyRelative(SerializedDictionaryPairs.VALUES);

            if (keys == null || values == null || !property.isExpanded)
                return Line;

            float height = Line + Spacing;

            if (keys.arraySize != values.arraySize)
                height += Line * 2f + Spacing;

            int count = Mathf.Min(keys.arraySize, values.arraySize);

            for (int index = 0; index < count; index++)
                height += RowHeight(values.GetArrayElementAtIndex(index)) + Spacing;

            height += Line;

            if (_refusals.ContainsKey(property.propertyPath))
                height += Spacing + Line * 1.5f;

            return height + Spacing;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty keys = property.FindPropertyRelative(SerializedDictionaryPairs.KEYS);
            SerializedProperty values = property.FindPropertyRelative(SerializedDictionaryPairs.VALUES);

            if (keys == null || values == null)
            {
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }

            EditorGUI.BeginProperty(position, label, property);

            Rect header = new Rect(position.x, position.y, position.width, Line);
            property.isExpanded = EditorGUI.Foldout(header, property.isExpanded,
                new GUIContent($"{label.text}  ({keys.arraySize})", label.tooltip), true);

            if (!property.isExpanded)
            {
                EditorGUI.EndProperty();
                return;
            }

            int indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel++;
            Rect body = EditorGUI.IndentedRect(new Rect(position.x, header.yMax + Spacing, position.width, 0f));
            EditorGUI.indentLevel = 0;

            float y = body.y;

            if (keys.arraySize != values.arraySize)
            {
                // Two lists of different lengths do not load at all; the pairs past the shorter one
                // are what the button gives up.
                Rect warning = new Rect(body.x, y, body.width, Line * 2f);
                EditorGUI.HelpBox(warning, $"{keys.arraySize} keys and {values.arraySize} values. Trim to pairs.", MessageType.Error);

                Rect trim = new Rect(warning.xMax - 60f, warning.y + (warning.height - Line) * 0.5f, 56f, Line);
                if (GUI.Button(trim, "Trim"))
                {
                    int pairs = Mathf.Min(keys.arraySize, values.arraySize);
                    keys.arraySize = pairs;
                    values.arraySize = pairs;
                }

                y = warning.yMax + Spacing;
            }

            _refusals.TryGetValue(property.propertyPath, out Refusal refusal);
            bool refused = _refusals.ContainsKey(property.propertyPath);

            int count = Mathf.Min(keys.arraySize, values.arraySize);
            int removeAt = -1;

            for (int index = 0; index < count; index++)
            {
                SerializedProperty value = values.GetArrayElementAtIndex(index);
                Rect row = new Rect(body.x, y, body.width, RowHeight(value));

                // Every other pair on a darker band, so a long list of multi-line values still
                // reads pair by pair.
                if (index % 2 == 1 && Event.current.type == EventType.Repaint)
                {
                    Rect band = new Rect(row.x - 2f, row.y - Spacing * 0.5f, row.width + 4f, row.height + Spacing);
                    EditorGUI.DrawRect(band, EditorGUIUtility.isProSkin ? new Color(0f, 0f, 0f, 0.14f) : new Color(0f, 0f, 0f, 0.07f));
                }

                if (DrawRow(row, property.propertyPath, keys, index, value, refused && refusal.Index == index))
                    removeAt = index;

                y = row.yMax + Spacing;
            }

            if (removeAt >= 0)
            {
                _pairs.Remove(keys, values, removeAt);
                _refusals.Remove(property.propertyPath);
            }

            Rect add = new Rect(body.xMax - 60f, y, 60f, Line);
            if (GUI.Button(add, "+ Add"))
            {
                _refusals.Remove(property.propertyPath);

                if (!_pairs.TryAdd(keys, values))
                    Refuse(property.propertyPath, keys.arraySize, "Every key this dictionary can take is already used.");
            }

            if (_refusals.TryGetValue(property.propertyPath, out Refusal shown))
            {
                Rect note = new Rect(body.x, add.yMax + Spacing, body.width, Line * 1.5f);
                EditorGUI.HelpBox(note, shown.Message, MessageType.Warning);
            }

            EditorGUI.indentLevel = indent;
            EditorGUI.EndProperty();
        }

        /// <summary>Draws one pair; true when its remove button was pressed.</summary>
        private bool DrawRow(Rect row, string path, SerializedProperty keys, int index, SerializedProperty value, bool refused)
        {
            float keyWidth = Mathf.Max(90f, (row.width - REMOVE_WIDTH - GAP * 2f) * 0.35f);

            Rect keyRect = new Rect(row.x, row.y, keyWidth, Line);
            Rect valueRect = new Rect(keyRect.xMax + GAP, row.y, row.width - keyWidth - REMOVE_WIDTH - GAP * 2f, row.height);
            Rect removeRect = new Rect(row.xMax - REMOVE_WIDTH, row.y, REMOVE_WIDTH, Line);

            Color previous = GUI.color;
            if (refused)
                GUI.color = new Color(1f, 0.55f, 0.55f);

            DrawKey(keyRect, path, keys, index);

            GUI.color = previous;

            DrawValue(valueRect, value);

            return GUI.Button(removeRect, "-");
        }

        /// <summary>
        /// The key, committed only when it stays unique. A string key commits on Enter or when the
        /// field loses focus, so a half-typed key never collides with another on the way.
        /// </summary>
        private void DrawKey(Rect rect, string path, SerializedProperty keys, int index)
        {
            SerializedProperty key = keys.GetArrayElementAtIndex(index);

            if (key.propertyType == SerializedPropertyType.String)
            {
                EditorGUI.BeginChangeCheck();
                string typed = EditorGUI.DelayedTextField(rect, key.stringValue);

                if (EditorGUI.EndChangeCheck())
                    Commit(path, keys, index, typed);

                return;
            }

            object before = _pairs.KeyOf(key);

            EditorGUI.BeginChangeCheck();
            EditorGUI.PropertyField(rect, key, GUIContent.none, false);

            if (!EditorGUI.EndChangeCheck())
                return;

            object chosen = _pairs.KeyOf(key);
            _pairs.SetKey(key, before);
            Commit(path, keys, index, chosen);
        }

        private void Commit(string path, SerializedProperty keys, int index, object key)
        {
            if (_pairs.TrySetKey(keys, index, key))
            {
                _refusals.Remove(path);
                return;
            }

            Refuse(path, index, key == null || key is string text && text.Length == 0
                ? "A key cannot be empty."
                : $"'{key}' is already a key. Each key appears once.");
        }

        private void Refuse(string path, int index, string message)
        {
            _refusals[path] = new Refusal {Index = index, Message = message};
        }

        /// <summary>
        /// A value that is a class or a struct is drawn open, its fields stacked in the value column,
        /// so a pair reads as one row rather than as a key beside a closed foldout.
        /// </summary>
        private void DrawValue(Rect rect, SerializedProperty value)
        {
            if (!IsOpenValue(value))
            {
                EditorGUI.PropertyField(new Rect(rect.x, rect.y, rect.width, Line), value, GUIContent.none, true);
                return;
            }

            float labelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = Mathf.Max(80f, rect.width * 0.45f);

            float y = rect.y;
            SerializedProperty field = value.Copy();
            SerializedProperty end = value.GetEndProperty();

            if (field.NextVisible(true))
            {
                do
                {
                    if (SerializedProperty.EqualContents(field, end))
                        break;

                    float height = EditorGUI.GetPropertyHeight(field, true);
                    EditorGUI.PropertyField(new Rect(rect.x, y, rect.width, height), field, true);
                    y += height + Spacing;
                }
                while (field.NextVisible(false));
            }

            EditorGUIUtility.labelWidth = labelWidth;
        }

        private float RowHeight(SerializedProperty value)
        {
            if (!IsOpenValue(value))
                return Mathf.Max(Line, EditorGUI.GetPropertyHeight(value, GUIContent.none, true));

            float height = 0f;
            SerializedProperty field = value.Copy();
            SerializedProperty end = value.GetEndProperty();

            if (field.NextVisible(true))
            {
                do
                {
                    if (SerializedProperty.EqualContents(field, end))
                        break;

                    height += EditorGUI.GetPropertyHeight(field, true) + Spacing;
                }
                while (field.NextVisible(false));
            }

            return Mathf.Max(Line, height - Spacing);
        }

        private bool IsOpenValue(SerializedProperty value)
        {
            return value.propertyType == SerializedPropertyType.Generic && !value.isArray && value.hasVisibleChildren;
        }
    }
}
#endif
