#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace FlowIoC.Editor.ModelViewer.PropertyDrawer.Properties
{
    internal class DictionaryPropertyDrawer<TKey, TValue> : PropertyDrawer<Dictionary<TKey, TValue>>
    {
        private bool _foldOut;
        private Dictionary<TKey, bool> _keyFoldouts = new Dictionary<TKey, bool>();
        private Dictionary<TKey, PropertyDrawerBase> _valueDrawers = new Dictionary<TKey, PropertyDrawerBase>();
        private Dictionary<TKey, bool> _valueTypeDrawers = new Dictionary<TKey, bool>();
        
        private GUIStyle _boxStyle;
        private GUIStyle _keyStyle;
        private GUIStyle _valueHeaderStyle;
        private int _maxDisplayItems = 20;
        private bool _initialized = false;
        
        
        public bool ShowNestedValues { get; set; } = true;
        
        public DictionaryPropertyDrawer(string fieldName, bool readOnly) : base(fieldName, readOnly)
        {
            InitializeStyles();
        }
        
        private void InitializeStyles()
        {
            if (_initialized) return;
            
            _boxStyle = new GUIStyle(EditorStyles.helpBox);
            _boxStyle.margin = new RectOffset(5, 5, 5, 5);
            _boxStyle.padding = new RectOffset(5, 5, 5, 5);
            
            _keyStyle = new GUIStyle(EditorStyles.boldLabel);
            _keyStyle.normal.textColor = new Color(0.6f, 0.8f, 1.0f);
            
            _valueHeaderStyle = new GUIStyle(EditorStyles.boldLabel);
            _valueHeaderStyle.normal.textColor = new Color(0.5f, 0.8f, 0.5f);
            
            _initialized = true;
        }

        protected override void OnBeforeDrawGUI()
        {
            base.OnBeforeDrawGUI();
            
            if (!_initialized)
                InitializeStyles();
            
            EditorGUILayout.BeginVertical(_boxStyle);
        }

        protected override void OnDrawGUI()
        {
            base.OnDrawGUI();

            var dict = GetValue();
            if (dict == null)
            {
                EditorGUILayout.LabelField("Dictionary is null", EditorStyles.boldLabel);
                if (GUILayout.Button("Create New Dictionary"))
                {
                    SetValue(new Dictionary<TKey, TValue>());
                }
                return;
            }
            
            string typeInfo = $"Dictionary<{typeof(TKey).Name}, {typeof(TValue).Name}>";
            
            EditorGUILayout.BeginHorizontal();
            try
            {
                GUILayout.Label($"{typeInfo} ({dict.Count} items)", EditorStyles.boldLabel);
                GUI.enabled = !_readOnly;
                if (GUILayout.Button("+", GUILayout.Width(25)))
                {
                    _foldOut = true;
                    TKey newKey = default;
                    TValue newValue = default;
                    
                    
                    if (typeof(TKey).IsClass && typeof(TKey) != typeof(string) && 
                        typeof(TKey).GetConstructor(Type.EmptyTypes) != null)
                    {
                        newKey = (TKey)Activator.CreateInstance(typeof(TKey));
                    }
                    
                    if (typeof(TValue).IsClass && typeof(TValue) != typeof(string) && 
                        typeof(TValue).GetConstructor(Type.EmptyTypes) != null)
                    {
                        newValue = (TValue)Activator.CreateInstance(typeof(TValue));
                    }
                    
                    if (!dict.ContainsKey(newKey))
                    {
                        dict[newKey] = newValue;
                    }
                }
                GUI.enabled = true;
            }
            finally
            {
                EditorGUILayout.EndHorizontal();
            }
            
            
            _foldOut = true; 
            
            if (!_foldOut) return;

            if (dict.Count == 0)
            {
                EditorGUILayout.LabelField("Dictionary is empty", EditorStyles.miniLabel);
                return;
            }

            int displayCount = 0;
            EditorGUI.indentLevel++;
            
            try
            {
                foreach (var kvp in dict.ToList())
                {
                    if (displayCount >= _maxDisplayItems)
                    {
                        EditorGUILayout.LabelField($"...and {dict.Count - _maxDisplayItems} more items", EditorStyles.miniLabel);
                        break;
                    }
                    
                    DrawKeyValuePair(kvp.Key, kvp.Value);
                    displayCount++;
                }
            }
            finally
            {
                EditorGUI.indentLevel--;
            }
        }
        
        private void DrawKeyValuePair(TKey key, TValue value)
        {
            try
            {
                EditorGUILayout.BeginHorizontal();
                try
                {
                    if (!_keyFoldouts.ContainsKey(key))
                    {
                        _keyFoldouts[key] = true; 
                    }
                    
                
                    _keyFoldouts[key] = true;
                    
                    EditorGUILayout.LabelField($"Key: {key?.ToString() ?? "null"}", EditorStyles.boldLabel);
                    
                    if (GUILayout.Button("×", GUILayout.Width(25)) && !_readOnly)
                    {
                        var dict = GetValue();
                        if (dict != null && dict.ContainsKey(key))
                        {
                            dict.Remove(key);
                            _keyFoldouts.Remove(key);
                            _valueDrawers.Remove(key);
                            GUIUtility.ExitGUI();
                        }
                    }
                }
                finally
                {
                    EditorGUILayout.EndHorizontal();
                }
                
                
                EditorGUI.indentLevel++;
                try
                {
                    DrawValueForKey(key, value);
                }
                finally
                {
                    EditorGUI.indentLevel--;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error drawing key-value pair for key: {key}, Error: {ex.Message}");
            }
        }
        
        private void DrawValueForKey(TKey key, TValue value)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            try
            {
                var valueType = typeof(TValue);
                
                if (value == null)
                {
                    EditorGUILayout.LabelField("Value: null", EditorStyles.miniLabel);
                    return;
                }

                
                if (value is IDictionary || value is IList)
                {
                    DrawNestedValue(value);
                    return;
                }
                
                
                if (!_valueDrawers.TryGetValue(key, out PropertyDrawerBase drawer) || drawer == null)
                {
                    CreateValueDrawer(key, value.GetType(), value);
                    _valueDrawers.TryGetValue(key, out drawer);
                }
                

                EditorGUILayout.LabelField("Value:", EditorStyles.boldLabel);
                
                
                EditorGUI.indentLevel++;
                try
                {
                    if (drawer != null)
                    {
                        drawer.OnGUI();
                    }
                    else
                    {
                        EditorGUILayout.LabelField($"{TruncateValueDisplay(value)}", EditorStyles.wordWrappedLabel);
                    }
                }
                finally
                {
                    EditorGUI.indentLevel--;
                }
            }
            finally
            {
                EditorGUILayout.EndVertical();
            }
        }
        
        private void CreateValueDrawer(TKey key, Type valueType, TValue currentValue)
        {
            PropertyDrawerBase drawer = null;
            
            bool isComplexType = false;
            
            if (valueType.IsGenericType)
            {
                if (typeof(IDictionary).IsAssignableFrom(valueType))
                {
                    isComplexType = true;
                    Type keyType = valueType.GetGenericArguments()[0];
                    Type valType = valueType.GetGenericArguments()[1];
                    
                    Type dictDrawerType = typeof(DictionaryPropertyDrawer<,>).MakeGenericType(keyType, valType);
                    drawer = (PropertyDrawerBase)Activator.CreateInstance(dictDrawerType, "Value", _readOnly);
                    
                    
                    if (drawer is DictionaryPropertyDrawer<object, object> dictDrawer)
                    {
                        dictDrawer.ShowNestedValues = true;
                    }
                }
                else if (typeof(IList).IsAssignableFrom(valueType))
                {
                    isComplexType = true;
                    Type elementType = valueType.GetGenericArguments()[0];
                    Type listDrawerType = typeof(ListPropertyDrawer<>).MakeGenericType(elementType);
                    drawer = (PropertyDrawerBase)Activator.CreateInstance(listDrawerType, "Value", _readOnly);
                }
                else
                {
                    Type genericDrawerType = typeof(GenericTypeDrawer<>).MakeGenericType(valueType);
                    drawer = (PropertyDrawerBase)Activator.CreateInstance(genericDrawerType, "Value", _readOnly);
                }
            }
            else if (valueType.IsPrimitive || valueType == typeof(string))
            {
                Type propertyDrawerType = ModelViewerUtils.GetPropertyDrawerType(valueType);
                if (propertyDrawerType != null)
                {
                    drawer = (PropertyDrawerBase)Activator.CreateInstance(propertyDrawerType, "Value", _readOnly);
                }
            }
            else
            {
                Type genericDrawerType = typeof(GenericTypeDrawer<>).MakeGenericType(valueType);
                drawer = (PropertyDrawerBase)Activator.CreateInstance(genericDrawerType, "Value", _readOnly);
            }
            
            
            _valueTypeDrawers[key] = !isComplexType || !ShowNestedValues;
            
            if (drawer != null)
            {
                drawer.ShowFieldName = false;
                _valueDrawers[key] = drawer;
                
                if (drawer is PropertyDrawer<TValue> typedDrawer)
                {
                    typedDrawer.SetValue(currentValue);
                }
                else
                {
                    drawer.GetType().GetMethod("SetValue").Invoke(drawer, new object[] { currentValue });
                }
            }
        }
        
        private void DrawNestedDictionary(TValue value)
        {
            if (value == null) return;
            
            IDictionary dict = value as IDictionary;
            if (dict == null) return;
            
            string dictId = $"NestedDict_{value.GetHashCode()}";
            bool foldout = EditorGUILayout.Foldout(
                SessionState.GetBool(dictId, false),
                $"Dictionary ({dict.Count} items)",
                true, EditorStyles.foldout);
            
            SessionState.SetBool(dictId, foldout);
            
            if (!foldout)
            {
                return;
            }
            
            if (dict.Count == 0)
            {
                EditorGUILayout.LabelField("Empty dictionary", EditorStyles.miniLabel);
                return;
            }
            
            EditorGUI.indentLevel++;
            
            try
            {
                int index = 0;
                foreach (DictionaryEntry entry in dict)
                {
                    if (index >= 10)
                    {
                        EditorGUILayout.LabelField($"... and {dict.Count - 10} more items", EditorStyles.miniLabel);
                        break;
                    }
                    
                    
                    string keyId = $"Key_{dictId}_{entry.Key?.GetHashCode() ?? 0}";
                    bool keyFoldout = EditorGUILayout.Foldout(
                        SessionState.GetBool(keyId, false),
                        $"Key: {TruncateValueDisplay(entry.Key)}",
                        true);
                    
                    SessionState.SetBool(keyId, keyFoldout);
                    
                    if (keyFoldout)
                    {
                        EditorGUI.indentLevel++;
                        
                        try
                        {
                            DrawNestedValue(entry.Value);
                        }
                        finally
                        {
                            EditorGUI.indentLevel--;
                        }
                    }
                    
                    index++;
                }
            }
            finally
            {
                EditorGUI.indentLevel--;
            }
        }
        
        
        private void DrawNestedValue(object value)
        {
            if (value == null)
            {
                EditorGUILayout.LabelField("Value: null", EditorStyles.miniLabel);
                return;
            }
            
            Type valueType = value.GetType();
            
            
            if (value is IDictionary nestedDict)
            {
                
                EditorGUILayout.LabelField($"Dictionary Value ({nestedDict.Count} items)", EditorStyles.boldLabel);
                
                EditorGUI.indentLevel++;
                
                try
                {
                    if (nestedDict.Count == 0)
                    {
                        EditorGUILayout.LabelField("Empty Dictionary", EditorStyles.miniLabel);
                    }
                    else
                    {
                        int idx = 0;
                        foreach (DictionaryEntry entry in nestedDict)
                        {
                            if (idx >= 5)
                            {
                                EditorGUILayout.LabelField($"... and {nestedDict.Count - 5} more items", EditorStyles.miniLabel);
                                break;
                            }
                            
                            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                            try
                            {
                                EditorGUILayout.LabelField($"Key: {TruncateValueDisplay(entry.Key)}", EditorStyles.boldLabel);
                                
                                EditorGUI.indentLevel++;
                                try
                                {
                                    
                                    if (entry.Value is IDictionary || entry.Value is IList)
                                    {
                                        DrawNestedValue(entry.Value);
                                    }
                                    else
                                    {
                                        EditorGUILayout.LabelField($"Value: {TruncateValueDisplay(entry.Value)}", EditorStyles.wordWrappedLabel);
                                    }
                                }
                                finally
                                {
                                    EditorGUI.indentLevel--;
                                }
                            }
                            finally
                            {
                                EditorGUILayout.EndVertical();
                            }
                            
                            idx++;
                        }
                    }
                }
                finally
                {
                    EditorGUI.indentLevel--;
                }
            }
            
            else if (value is IList nestedList)
            {
                
                EditorGUILayout.LabelField($"List Value ({nestedList.Count} items)", EditorStyles.boldLabel);
                
                EditorGUI.indentLevel++;
                
                try
                {
                    if (nestedList.Count == 0)
                    {
                        EditorGUILayout.LabelField("Empty List", EditorStyles.miniLabel);
                    }
                    else
                    {
                        for (int i = 0; i < Math.Min(nestedList.Count, 5); i++)
                        {
                            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                            try
                            {
                                EditorGUILayout.LabelField($"[{i}]:", EditorStyles.boldLabel);
                                
                                EditorGUI.indentLevel++;
                                try
                                {
                                    
                                    if (nestedList[i] is IDictionary || nestedList[i] is IList)
                                    {
                                        DrawNestedValue(nestedList[i]);
                                    }
                                    else
                                    {
                                        EditorGUILayout.LabelField($"{TruncateValueDisplay(nestedList[i])}", EditorStyles.wordWrappedLabel);
                                    }
                                }
                                finally
                                {
                                    EditorGUI.indentLevel--;
                                }
                            }
                            finally
                            {
                                EditorGUILayout.EndVertical();
                            }
                        }
                        
                        if (nestedList.Count > 5)
                        {
                            EditorGUILayout.LabelField($"... and {nestedList.Count - 5} more items", EditorStyles.miniLabel);
                        }
                    }
                }
                finally
                {
                    EditorGUI.indentLevel--;
                }
            }
            else
            {
                EditorGUILayout.LabelField($"Value: {TruncateValueDisplay(value)}", EditorStyles.wordWrappedLabel);
            }
        }
        
        private void DrawNestedList(TValue value)
        {
            if (value == null) return;
            
            IList list = value as IList;
            if (list == null) return;
            
            EditorGUILayout.LabelField($"List ({list.Count} items)", EditorStyles.boldLabel);
            
            if (list.Count == 0)
            {
                EditorGUILayout.LabelField("Empty list", EditorStyles.miniLabel);
                return;
            }
            
            EditorGUI.indentLevel++;
            
            for (int i = 0; i < Mathf.Min(list.Count, 10); i++)
            {
                object item = list[i];
                EditorGUILayout.LabelField($"[{i}]: {TruncateValueDisplay(item)}", EditorStyles.wordWrappedLabel);
            }
            
            if (list.Count > 10)
            {
                EditorGUILayout.LabelField($"... and {list.Count - 10} more items", EditorStyles.miniLabel);
            }
            
            EditorGUI.indentLevel--;
        }
        
        private string TruncateValueDisplay(object value)
        {
            if (value == null) return "null";
            
            string display = value.ToString();
            if (display.Length > 100)
            {
                display = display.Substring(0, 97) + "...";
            }
            return display;
        }

        protected override void OnDrawCompletedGUI()
        {
            base.OnDrawCompletedGUI();
            EditorGUILayout.EndVertical();
        }
    }
}
#endif