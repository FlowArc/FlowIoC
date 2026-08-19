#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace FlowIoC.Editor.ModelViewer.PropertyDrawer.Properties
{

    internal class GenericTypeDrawer<T> : PropertyDrawer<T>
    {
        private bool _foldOut;
        private bool _initialized;
        private ModelViewerUtils.TypeInfo _typeInfo;
        private bool _isCollection;
        private bool _isDeepNested;
        private Type _genericType;
        
        private List<PropertyDrawerBase> _childDrawers;
        private int _maxDepthLevel = 3; 
        private int _maxDisplayItems = 10; 

        public GenericTypeDrawer(string fieldName, bool readOnly) : base(fieldName, readOnly)
        {
            _childDrawers = new List<PropertyDrawerBase>();
            _genericType = typeof(T);
            InitializeTypeInfo();
        }

        private void InitializeTypeInfo()
        {
            if (_initialized)
                return;
                
            _typeInfo = ModelViewerUtils.AnalyzeType(_genericType);
            _isCollection = _typeInfo.IsList || _typeInfo.IsDictionary;
            
            _isDeepNested = IsDeepNested(_typeInfo);
            
            _initialized = true;
        }
        
        private bool IsDeepNested(ModelViewerUtils.TypeInfo info)
        {
            if (info.IsGeneric && info.GenericArguments != null)
            {
                foreach (var arg in info.GenericArguments)
                {
                    if (arg.IsGeneric)
                        return true; 
                }
            }
            return false;
        }

        protected override void OnBeforeDrawGUI()
        {
            base.OnBeforeDrawGUI();
            EditorGUILayout.BeginVertical("box");
        }

        protected override void OnDrawGUI()
        {
            base.OnDrawGUI();

            if (!_initialized)
                InitializeTypeInfo();
                
            _foldOut = EditorGUILayout.Foldout(_foldOut, GetTypeDisplayName());

            if (!_foldOut)
                return;
                
            T value = GetValue();
            if (value == null)
            {
                EditorGUILayout.LabelField("null", EditorStyles.miniLabel);
                return;
            }
            
            EditorGUI.indentLevel++;
            
            if (_isCollection)
            {
                DrawCollection(value);
            }
            else if (_isDeepNested)
            {
                DrawDeepNestedType(value);
            }
            else
            {
                DrawSimpleGenericType(value);
            }
            
            EditorGUI.indentLevel--;
        }
        
        private void DrawCollection(T collection)
        {
            if (collection is IDictionary dictionary)
            {
                DrawDictionary(dictionary);
            }
            else if (collection is IList list)
            {
                DrawList(list);
            }
            else
            {
                EditorGUILayout.LabelField("Unsupported collection type", EditorStyles.miniLabel);
            }
        }
        
        private void DrawDictionary(IDictionary dictionary)
        {
            EditorGUILayout.LabelField($"Total {dictionary.Count} items", EditorStyles.miniLabel);
            
            int itemCounter = 0;
            foreach (DictionaryEntry entry in dictionary)
            {
                if (itemCounter >= _maxDisplayItems)
                {
                    EditorGUILayout.LabelField($"... and {dictionary.Count - _maxDisplayItems} more items", EditorStyles.miniLabel);
                    break;
                }
                
                EditorGUILayout.BeginHorizontal();
                
                EditorGUILayout.LabelField("Key:", GUILayout.Width(40));
                DrawSingleValue(entry.Key);
                
                EditorGUILayout.LabelField("Value:", GUILayout.Width(40));
                DrawSingleValue(entry.Value);
                
                EditorGUILayout.EndHorizontal();
                
                itemCounter++;
            }
        }
        
        private void DrawList(IList list)
        {
            EditorGUILayout.LabelField($"Total {list.Count} items", EditorStyles.miniLabel);
            
            for (int i = 0; i < Mathf.Min(list.Count, _maxDisplayItems); i++)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"[{i}]:", GUILayout.Width(40));
                DrawSingleValue(list[i]);
                EditorGUILayout.EndHorizontal();
            }
            
            if (list.Count > _maxDisplayItems)
            {
                EditorGUILayout.LabelField($"... and {list.Count - _maxDisplayItems} more items", EditorStyles.miniLabel);
            }
        }
        
        private void DrawSingleValue(object value)
        {
            if (value == null)
            {
                EditorGUILayout.LabelField("null", EditorStyles.miniLabel);
                return;
            }
            
            Type valueType = value.GetType();
            ModelViewerUtils.TypeInfo valueInfo = ModelViewerUtils.AnalyzeType(valueType);
            
        
            if (valueInfo.IsComplex)
            {
                if (valueInfo.IsList || valueInfo.IsDictionary)
                {
                    string countInfo = "";
                    if (value is ICollection collection)
                        countInfo = $" ({collection.Count} items)";
                    
                    EditorGUILayout.LabelField($"{valueType.Name}{countInfo}", EditorStyles.miniLabel);
                }
                else if (valueInfo.IsGeneric)
                {
                    EditorGUILayout.LabelField(GetFriendlyTypeName(valueType), EditorStyles.miniLabel);
                }
                else
                {
                    EditorGUILayout.LabelField(TruncateString(value.ToString(), 100), EditorStyles.miniLabel);
                }
            }
            else
            {
                EditorGUILayout.LabelField(value.ToString(), EditorStyles.miniLabel);
            }
        }
        
        private void DrawDeepNestedType(T value)
        {
            EditorGUILayout.LabelField("Deep Nested Type:", EditorStyles.boldLabel);
            
            EditorGUILayout.LabelField($"Type: {GetFriendlyTypeName(_genericType)}", EditorStyles.miniLabel);
            
            EditorGUILayout.LabelField("Nested Structure:", EditorStyles.boldLabel);
            DisplayNestedTypeStructure(_typeInfo, 0);
            
            EditorGUILayout.LabelField("Value Summary:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(TruncateString(value.ToString(), 200), EditorStyles.wordWrappedLabel);
        }
        
        private void DisplayNestedTypeStructure(ModelViewerUtils.TypeInfo info, int depth)
        {
            if (depth > _maxDepthLevel)
            {
                EditorGUILayout.LabelField("...(more nested levels)", EditorStyles.miniLabel);
                return;
            }
            
            if (info.GenericArguments != null)
            {
                for (int i = 0; i < info.GenericArguments.Length; i++)
                {
                    var arg = info.GenericArguments[i];
                    string indent = new string(' ', depth * 4);
                    string paramName = info.IsDictionary ? (i == 0 ? "Key" : "Value") : $"T{i}";
                    
                    EditorGUILayout.LabelField($"{indent}{paramName}: {GetFriendlyTypeName(arg.Type)}", EditorStyles.miniLabel);
                    
                    if (arg.IsGeneric)
                    {
                        DisplayNestedTypeStructure(arg, depth + 1);
                    }
                }
            }
        }
        
        private void DrawSimpleGenericType(T value)
        {
            EditorGUILayout.LabelField(TruncateString(value.ToString(), 200), EditorStyles.wordWrappedLabel);
        }

        private string GetTypeDisplayName()
        {
            return $"{_fieldName}: {GetFriendlyTypeName(_genericType)}";
        }
        
        private string GetFriendlyTypeName(Type type)
        {
            if (!type.IsGenericType)
                return type.Name;
                
            var genericArgs = type.GetGenericArguments();
            var baseTypeName = type.Name.Split('`')[0];
            
            if (type.GetGenericTypeDefinition() == typeof(List<>))
                return $"List<{GetFriendlyTypeName(genericArgs[0])}>";
                
            if (type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
                return $"Dictionary<{GetFriendlyTypeName(genericArgs[0])}, {GetFriendlyTypeName(genericArgs[1])}>";
                
            return $"{baseTypeName}<{string.Join(", ", genericArgs.Select(a => GetFriendlyTypeName(a)))}>";
        }
        
        private string TruncateString(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text))
                return text;
                
            if (text.Length <= maxLength)
                return text;
                
            return text.Substring(0, maxLength - 3) + "...";
        }

        protected override void OnDrawCompletedGUI()
        {
            base.OnDrawCompletedGUI();
            EditorGUILayout.EndVertical();
        }
    }
}
#endif 