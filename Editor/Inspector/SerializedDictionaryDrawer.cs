#if UNITY_EDITOR
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
    /// A key that is already in the dictionary is refused rather than written. The dictionary
    /// rebuilds itself from its two lists on every load and stops at the first repeated key, so a
    /// duplicate written here would take every pair after it with it the next time it saved.
    ///
    /// Only for UnityEngine.Rendering.SerializedDictionary, never for Dictionary itself: Unity 6.6
    /// draws a plain Dictionary field with its own editor, and this must not take that one over.
    /// </summary>
    [CustomPropertyDrawer(typeof(SerializedDictionary<,>), true)]
    [CustomPropertyDrawer(typeof(SerializedDictionary<,,,>), true)]
    public class SerializedDictionaryDrawer : PropertyDrawer
    {
        private const string KEYS = "m_Keys";
        private const string VALUES = "m_Values";
        private const float REMOVE_WIDTH = 20f;
        private const float GAP = 4f;

        private static float Line => EditorGUIUtility.singleLineHeight;
        private static float Spacing => EditorGUIUtility.standardVerticalSpacing;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            SerializedProperty keys = property.FindPropertyRelative(KEYS);
            SerializedProperty values = property.FindPropertyRelative(VALUES);

            if (keys == null || values == null || !property.isExpanded)
                return Line;

            float height = Line + Spacing;

            if (keys.arraySize != values.arraySize)
                height += Line * 2f + Spacing;

            int count = Mathf.Min(keys.arraySize, values.arraySize);

            for (int index = 0; index < count; index++)
                height += RowHeight(keys.GetArrayElementAtIndex(index), values.GetArrayElementAtIndex(index)) + Spacing;

            return height + Line + Spacing;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty keys = property.FindPropertyRelative(KEYS);
            SerializedProperty values = property.FindPropertyRelative(VALUES);

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

            int count = Mathf.Min(keys.arraySize, values.arraySize);
            int removeAt = -1;

            for (int index = 0; index < count; index++)
            {
                SerializedProperty key = keys.GetArrayElementAtIndex(index);
                SerializedProperty value = values.GetArrayElementAtIndex(index);
                float height = RowHeight(key, value);

                Rect row = new Rect(body.x, y, body.width, height);

                if (DrawRow(row, keys, index, key, value))
                    removeAt = index;

                y = row.yMax + Spacing;
            }

            if (removeAt >= 0)
                RemovePair(keys, values, removeAt);

            Rect add = new Rect(body.xMax - 60f, y, 60f, Line);
            if (GUI.Button(add, "+ Add"))
                AddPair(keys, values);

            EditorGUI.indentLevel = indent;
            EditorGUI.EndProperty();
        }

        /// <summary>Draws one pair; true when its remove button was pressed.</summary>
        private bool DrawRow(Rect row, SerializedProperty keys, int index, SerializedProperty key, SerializedProperty value)
        {
            float keyWidth = Mathf.Max(90f, (row.width - REMOVE_WIDTH - GAP * 2f) * 0.35f);

            Rect keyRect = new Rect(row.x, row.y, keyWidth, Line);
            Rect valueRect = new Rect(keyRect.xMax + GAP, row.y, row.width - keyWidth - REMOVE_WIDTH - GAP * 2f, row.height);
            Rect removeRect = new Rect(row.xMax - REMOVE_WIDTH, row.y, REMOVE_WIDTH, Line);

            DrawKey(keyRect, keys, index, key);
            DrawValue(valueRect, value);

            return GUI.Button(removeRect, "-");
        }

        /// <summary>
        /// The key, committed only when it stays unique. A string key commits on Enter or when the
        /// field loses focus, so a half-typed key never collides with another on the way.
        /// </summary>
        private void DrawKey(Rect rect, SerializedProperty keys, int index, SerializedProperty key)
        {
            if (key.propertyType == SerializedPropertyType.String)
            {
                EditorGUI.BeginChangeCheck();
                string typed = EditorGUI.DelayedTextField(rect, key.stringValue);

                if (EditorGUI.EndChangeCheck() && !string.IsNullOrEmpty(typed) && !IsTaken(keys, index, typed))
                    key.stringValue = typed;

                return;
            }

            object before = KeyOf(key);

            EditorGUI.BeginChangeCheck();
            EditorGUI.PropertyField(rect, key, GUIContent.none, false);

            if (EditorGUI.EndChangeCheck() && (KeyOf(key) == null || IsTaken(keys, index, KeyOf(key))))
                SetKey(key, before);
        }

        /// <summary>A key as something two keys can be compared by, whatever its type.</summary>
        private object KeyOf(SerializedProperty key)
        {
            switch (key.propertyType)
            {
                case SerializedPropertyType.String: return key.stringValue;
                case SerializedPropertyType.Enum: return (long) key.intValue;
                case SerializedPropertyType.Integer: return key.longValue;
                case SerializedPropertyType.ObjectReference: return key.objectReferenceValue;
                default: return key.boxedValue;
            }
        }

        private void SetKey(SerializedProperty key, object value)
        {
            switch (key.propertyType)
            {
                case SerializedPropertyType.String: key.stringValue = (string) value; break;
                case SerializedPropertyType.Enum: key.intValue = (int) (long) value; break;
                case SerializedPropertyType.Integer: key.longValue = (long) value; break;
                case SerializedPropertyType.ObjectReference: key.objectReferenceValue = (UnityEngine.Object) value; break;
                default: key.boxedValue = value; break;
            }
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

        private float RowHeight(SerializedProperty key, SerializedProperty value)
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

        private bool IsTaken(SerializedProperty keys, int index, object candidate)
        {
            for (int other = 0; other < keys.arraySize; other++)
            {
                if (other != index && Equals(KeyOf(keys.GetArrayElementAtIndex(other)), candidate))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// A new pair gets a key nobody holds - a numbered name, the first unused enum value, the
        /// next integer - so the dictionary still loads the moment it is added. A key type with no
        /// free value to offer adds nothing.
        /// </summary>
        private void AddPair(SerializedProperty keys, SerializedProperty values)
        {
            int index = keys.arraySize;

            keys.arraySize++;
            SerializedProperty key = keys.GetArrayElementAtIndex(index);

            if (!TryGiveFreeKey(keys, index, key))
            {
                keys.arraySize--;
                return;
            }

            values.arraySize = keys.arraySize;
            ResetValue(values.GetArrayElementAtIndex(index));
        }

        private bool TryGiveFreeKey(SerializedProperty keys, int index, SerializedProperty key)
        {
            switch (key.propertyType)
            {
                case SerializedPropertyType.String:
                    for (int number = 0; ; number++)
                    {
                        string name = number == 0 ? "Key" : $"Key {number}";
                        if (!IsTaken(keys, index, name))
                        {
                            key.stringValue = name;
                            return true;
                        }
                    }

                case SerializedPropertyType.Enum:
                    for (int option = 0; option < key.enumNames.Length; option++)
                    {
                        key.enumValueIndex = option;

                        if (!IsTaken(keys, index, KeyOf(key)))
                            return true;
                    }

                    return false;

                case SerializedPropertyType.Integer:
                    long next = 0;
                    while (IsTaken(keys, index, next))
                        next++;

                    key.longValue = next;
                    return true;

                default:
                    return KeyOf(key) != null && !IsTaken(keys, index, KeyOf(key));
            }
        }

        /// <summary>Growing an array copies the last element; a new pair starts from nothing.</summary>
        private void ResetValue(SerializedProperty value)
        {
            switch (value.propertyType)
            {
                case SerializedPropertyType.ObjectReference:
                    value.objectReferenceValue = null;
                    break;
                case SerializedPropertyType.String:
                    value.stringValue = string.Empty;
                    break;
                case SerializedPropertyType.Generic when !value.isArray:
                    SerializedProperty field = value.Copy();
                    SerializedProperty end = value.GetEndProperty();
                    if (field.Next(true))
                    {
                        do
                        {
                            if (SerializedProperty.EqualContents(field, end))
                                break;

                            if (field.propertyType == SerializedPropertyType.ObjectReference)
                                field.objectReferenceValue = null;
                            else if (field.propertyType == SerializedPropertyType.Boolean)
                                field.boolValue = false;
                        }
                        while (field.Next(false));
                    }

                    break;
            }
        }

        private void RemovePair(SerializedProperty keys, SerializedProperty values, int index)
        {
            RemoveAt(keys, index);
            RemoveAt(values, index);
        }

        /// <summary>
        /// An object reference used to be cleared by the first delete and removed by the second;
        /// checking the size keeps the two lists the same length either way.
        /// </summary>
        private void RemoveAt(SerializedProperty list, int index)
        {
            int size = list.arraySize;
            list.DeleteArrayElementAtIndex(index);

            if (list.arraySize == size)
                list.DeleteArrayElementAtIndex(index);
        }
    }
}
#endif
