#if UNITY_EDITOR

using System.Collections.Generic;
using Modules.AudioModule.Controllers;
using Modules.AudioModule.Shared;
using Modules.AudioModule.Shared.Data.UnityObjects;
using UnityEditor;
using UnityEngine;

namespace Modules.AudioModule.Editor
{
    /// <summary>
    /// An [AudioKeyId] string drawn as a list of the keys the code declares, so an id is picked and
    /// never typed. Inside a bank the list holds only that bank's module's keys. An id no key
    /// declares any more stays, marked, until somebody picks another.
    /// </summary>
    [CustomPropertyDrawer(typeof(AudioKeyIdAttribute))]
    internal class AudioKeyIdDrawer : PropertyDrawer
    {
        private const string NONE = "(none)";

        private IReadOnlyList<AudioKey> _declared;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

            _declared ??= new ListDeclaredKeysFunction().Execute();

            string module = (property.serializedObject.targetObject as CD_AudioBank)?.Module;
            var ids = new List<string> {NONE};

            foreach (AudioKey key in _declared)
            {
                if (string.IsNullOrEmpty(module) || key.Bank == module)
                    ids.Add(key.Id);
            }

            string current = property.stringValue;
            int selected = string.IsNullOrEmpty(current) ? 0 : ids.IndexOf(current);

            if (selected < 0)
            {
                ids.Add(current + "  (not declared)");
                selected = ids.Count - 1;
            }

            EditorGUI.BeginProperty(position, label, property);
            int picked = EditorGUI.Popup(position, label.text, selected, ids.ToArray());

            if (picked != selected)
                property.stringValue = picked == 0 ? string.Empty : ids[picked];

            EditorGUI.EndProperty();
        }
    }
}

#endif
