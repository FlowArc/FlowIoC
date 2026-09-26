#if UNITY_EDITOR
using UnityEditor;

namespace FlowIoC.Editor.Inspector
{
    /// <summary>
    /// The edits SerializedDictionaryDrawer makes to a dictionary's two lists, kept apart from the
    /// drawing so they can be tested without an inspector. Every edit leaves the lists the same
    /// length and every key unique, because the dictionary rebuilds itself from them on each load
    /// and stops at the first repeated key - losing every pair after it the next time it saves.
    /// </summary>
    internal class SerializedDictionaryPairs
    {
        internal const string KEYS = "m_Keys";
        internal const string VALUES = "m_Values";

        /// <summary>
        /// Adds a pair whose key nobody holds - a numbered name, the first unused enum value, the
        /// next integer - so the dictionary still loads the moment it is added. False, and nothing
        /// added, when the key type has no free value to offer.
        /// </summary>
        internal bool TryAdd(SerializedProperty keys, SerializedProperty values)
        {
            int index = keys.arraySize;

            keys.arraySize++;

            if (!TryGiveFreeKey(keys, index, keys.GetArrayElementAtIndex(index)))
            {
                keys.arraySize--;
                return false;
            }

            values.arraySize = keys.arraySize;
            ResetValue(values.GetArrayElementAtIndex(index));

            return true;
        }

        /// <summary>
        /// Writes a key unless another pair already holds it or it is empty. False, and the key left
        /// as it was, when refused.
        /// </summary>
        internal bool TrySetKey(SerializedProperty keys, int index, object key)
        {
            if (key == null || key is string text && text.Length == 0 || IsTaken(keys, index, key))
                return false;

            SetKey(keys.GetArrayElementAtIndex(index), key);
            return true;
        }

        internal void Remove(SerializedProperty keys, SerializedProperty values, int index)
        {
            RemoveAt(keys, index);
            RemoveAt(values, index);
        }

        /// <summary>A key as something two keys can be compared by, whatever its type.</summary>
        internal object KeyOf(SerializedProperty key)
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

        internal void SetKey(SerializedProperty key, object value)
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

        internal bool IsTaken(SerializedProperty keys, int index, object candidate)
        {
            for (int other = 0; other < keys.arraySize; other++)
            {
                if (other != index && Equals(KeyOf(keys.GetArrayElementAtIndex(other)), candidate))
                    return true;
            }

            return false;
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
                case SerializedPropertyType.Integer:
                    value.longValue = 0;
                    break;
                case SerializedPropertyType.Boolean:
                    value.boolValue = false;
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
