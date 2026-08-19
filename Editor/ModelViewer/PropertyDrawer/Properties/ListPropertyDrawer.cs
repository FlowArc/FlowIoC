#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace FlowIoC.Editor.ModelViewer.PropertyDrawer.Properties
{
    internal class ListPropertyDrawer<T> : PropertyDrawer<List<T>>
    {
        public bool UseFoldOut;
        public bool CanDeleteItem;
        
        private bool _foldOut;
        
        private List<PropertyDrawerBase> _enabledProperties;
        private List<PropertyDrawerBase> _disabledProperties;

        private Type _propertyDrawerType;
        private bool _isNestedValueType;

        public Action<int, T> ItemValueChanged;

        public ListPropertyDrawer(string fieldName, bool readOnly) : base(fieldName, readOnly)
        {
            _enabledProperties = new List<PropertyDrawerBase>();
            _disabledProperties = new List<PropertyDrawerBase>();
            
            _propertyDrawerType = ModelViewerUtils.GetPropertyDrawerType(typeof(T));
            _isNestedValueType = IsNestedType(typeof(T));
            
            UseFoldOut = true;
        }

        private bool IsNestedType(Type type)
        {
            if (type.IsGenericType)
            {
                    
                if (typeof(IDictionary).IsAssignableFrom(type) || 
                    typeof(IList).IsAssignableFrom(type))
                {
                    return true;
                }
                
                var genericArgs = type.GetGenericArguments();
                foreach (var arg in genericArgs)
                {
                    if (IsNestedType(arg))
                        return true;
                }
            }
            
            return false;
        }

        protected override void OnBeforeDrawGUI()
        {
            base.OnBeforeDrawGUI();
            EditorGUILayout.BeginVertical();
        }

        protected override void OnDrawGUI()
        {
            base.OnDrawGUI();

            if (_propertyDrawerType == null)
            {
                EditorGUILayout.HelpBox(new GUIContent($"{_fieldName}: Type is not supported!"));
                return;
            }
            
            if(UseFoldOut)
                _foldOut = EditorGUILayout.Foldout(_foldOut, _fieldName);

            if (!_foldOut && UseFoldOut)
                return;
            
            var value = GetValue();
            if (value == null)
            {
                if (GUILayout.Button("NULL")) 
                    SetValue(new List<T>());
                
                return;
            }

            
            if (_isNestedValueType)
            {
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField($"Karmaşık içerikli List<{typeof(T).Name}> ({value.Count} öğe)");
                
                if (value.Count > 0)
                {
                    EditorGUI.indentLevel++;
                    
                    int displayCount = Math.Min(value.Count, 10);
                    for (int i = 0; i < displayCount; i++)
                    {
                        DisplayNestedValue(value[i], i);
                    }
                    
                    if (value.Count > 10)
                    {
                        EditorGUILayout.LabelField("... (ve diğer öğeler)");
                    }
                    
                    EditorGUI.indentLevel--;
                }
                
                EditorGUILayout.EndVertical();
            }
            else
            {
                CheckValueCountAndPropertyDrawerCount();
                
                for (var ii = 0; ii < value.Count; ii++)
                {
                    var piece = value[ii];
                    PieceGUI(piece, ii);
                }
            }
        }

        private void DisplayNestedValue(T value, int index)
        {
            if (value == null)
            {
                EditorGUILayout.LabelField($"[{index}]: null");
                return;
            }
            
            Type valueType = value.GetType();
            
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField($"[{index}]:", EditorStyles.boldLabel);
            
            if (typeof(IDictionary).IsAssignableFrom(valueType))
            {
                var dictionary = value as IDictionary;
                if (dictionary != null)
                {
                    EditorGUILayout.LabelField($"Dictionary ({dictionary.Count} items)");
                    
                    if (dictionary.Count > 0)
                    {
                        EditorGUI.indentLevel++;
                        int count = 0;
                        
                        foreach (DictionaryEntry entry in dictionary)
                        {
                            if (count < 3)
                            {
                                EditorGUILayout.LabelField($"Key: {TruncateValueForDisplay(entry.Key)}, Value: {TruncateValueForDisplay(entry.Value)}");
                                count++;
                            }
                            else
                            {
                                EditorGUILayout.LabelField("... (ve diğer öğeler)");
                                break;
                            }
                        }
                        
                        EditorGUI.indentLevel--;
                    }
                }
            }
            else if (typeof(IList).IsAssignableFrom(valueType))
            {
                var list = value as IList;
                if (list != null)
                {
                    EditorGUILayout.LabelField($"List ({list.Count} items)");
                    
                    if (list.Count > 0)
                    {
                        EditorGUI.indentLevel++;
                        for (int i = 0; i < Math.Min(list.Count, 3); i++)
                        {
                            EditorGUILayout.LabelField($"[{i}]: {TruncateValueForDisplay(list[i])}");
                        }
                        
                        if (list.Count > 3)
                        {
                            EditorGUILayout.LabelField("... (ve diğer öğeler)");
                        }
                        
                        EditorGUI.indentLevel--;
                    }
                }
            }
            else
            {
                EditorGUILayout.LabelField($"Değer: {TruncateValueForDisplay(value)}");
            }
            
            EditorGUILayout.EndVertical();
        }

        private string TruncateValueForDisplay(object value)
        {
            if (value == null)
                return "null";
                
            string strValue = value.ToString();
            if (strValue.Length > 50)
                return strValue.Substring(0, 47) + "...";
                
            return strValue;
        }

        protected override void OnDrawCompletedGUI()
        {
            base.OnDrawCompletedGUI();
            
            EditorGUILayout.EndVertical();
        }

        private void CheckValueCountAndPropertyDrawerCount()
        {
            var value = GetValue();
            if(value.Count == _enabledProperties.Count)
                return;

            for (var ii = 0; ii < value.Count; ii++)
            {
                if (ii >= _enabledProperties.Count)
                    GetAvailablePropertyDrawer();
                else
                    SendPropertyDrawerToPool(_enabledProperties[ii]);
            }
        }

        private void PieceGUI(T piece, int listIndex)
        {
            var propertyDrawer = _enabledProperties[listIndex];
            propertyDrawer.SetFieldName(listIndex.ToString());
            propertyDrawer.ShowFieldName = ShowFieldName;
            
            propertyDrawer.GetType().GetMethod("SetValue").Invoke(propertyDrawer, new []
            {
                piece as object
            });

            propertyDrawer.OnValueChanged += () =>
            {
                var value = GetValue();
                value[listIndex] = (T) propertyDrawer.GetType().GetMethod("GetValue").Invoke(propertyDrawer, null);
                
                SetValue(value);
                ItemValueChanged?.Invoke(listIndex, value[listIndex]);
            };

            EditorGUILayout.BeginHorizontal();
            
            propertyDrawer.OnGUI();

            if(CanDeleteItem)
            {
                GUI.backgroundColor = Color.red;
                var removeButton = GUILayout.Button("-", GUILayout.Width(15));
                GUI.backgroundColor = Color.white;

                if (removeButton)
                {
                    SendPropertyDrawerToPool(propertyDrawer);

                    var listValue = GetValue();
                    listValue.RemoveAt(listIndex);
                    SetValue(listValue);
                }
            }
            
            EditorGUILayout.EndHorizontal();
        }

        private void SendPropertyDrawerToPool(PropertyDrawerBase propertyDrawerBase)
        {
            propertyDrawerBase.OnValueChanged = null;

            _enabledProperties.Remove(propertyDrawerBase);
            _disabledProperties.Add(propertyDrawerBase);
        }
        
        private PropertyDrawerBase GetAvailablePropertyDrawer()
        {
            PropertyDrawerBase availablePropertyDrawer;
            if (_disabledProperties.Count == 0)
            {
                availablePropertyDrawer =
                    (PropertyDrawerBase) Activator.CreateInstance(_propertyDrawerType, _fieldName, _readOnly);
            }
            else
            {
                availablePropertyDrawer = _disabledProperties[0];
                _disabledProperties.Remove(availablePropertyDrawer);
            }

            availablePropertyDrawer.ShowFieldName = ShowFieldName;
            _enabledProperties.Add(availablePropertyDrawer);

            return availablePropertyDrawer;
        }
    }
}
#endif