#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FlowIoC.BaseModule.Root;
using FlowIoC.Editor.ModelViewer.MemberInfoDrawer;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FlowIoC.Editor.ModelViewer
{
    internal class InspectWindow : EditorWindow
    {
        private object _inspectedObject;
        private object _inspectedObjectContext;

        private Dictionary<MemberInfo, MemberInfoDrawerBase> _activePropertyDrawersDict;

        public void Initialize(object inspectedObject, object inspectedObjectContext, string bindingName = "")
        {
            _inspectedObject = inspectedObject;
            _inspectedObjectContext = inspectedObjectContext;

            _activePropertyDrawersDict = new Dictionary<MemberInfo, MemberInfoDrawerBase>();

            if (inspectedObjectContext != null)
            {
                PlayerPrefs.SetString(titleContent.text, inspectedObjectContext.GetType().Name + "_" + inspectedObject.GetType().Name + "_" + bindingName);
            }
        }

        public void OnGUI()
        {
            if (!Application.isPlaying)
            {
                EditorGUILayout.LabelField("Enter Play Mode to view Models.");
                return;
            }

            switch (_inspectedObject)
            {
                case null when _inspectedObjectContext == null:
                    FindContextAndLoadObjectReferenceFromContext();
                    return;
                case null:
                    return;
                default:
                    DisplayObjectFields(_inspectedObject);
                    break;
            }
        }

        private void Update()
        {
            if (!Application.isPlaying)
                return;

            if (_inspectedObject == null)
                return;

            Repaint();
        }

        private void DisplayObjectFields(object rootObject)
        {
            var memberInfoList = ModelViewerUtils.GetTypeMembersList(rootObject);

            foreach (var memberInfo in memberInfoList)
            {
                DisplayFieldInfoGUI(memberInfo, rootObject);
            }
        }

        private void DisplayFieldInfoGUI(MemberInfo memberInfo, object rootObject)
        {
            Type memberType = GetMemberType(memberInfo, rootObject);
            if (memberType == null)
                return;

            var typeInfo = ModelViewerUtils.AnalyzeType(memberType);
            
            if ((typeInfo.IsDictionary || typeInfo.IsList) && typeInfo.IsGeneric && IsDeepNestedType(typeInfo))
            {
                try
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    
                    var style = new GUIStyle(EditorStyles.boldLabel);
                    style.normal.textColor = new Color(0.4f, 0.7f, 1f);
                    EditorGUILayout.LabelField(memberInfo.Name, style);
                    
                    var fieldValue = memberInfo.GetValue(rootObject);
                    if (fieldValue != null)
                    {
                        EditorGUILayout.Space();
                        ModelViewerUtils.VisualizeDeepNestedType(fieldValue);
                    }
                    else
                    {
                        EditorGUILayout.LabelField("null", EditorStyles.miniLabel);
                    }
                }
                finally
                {
                    EditorGUILayout.EndVertical();
                }
                return;
            }
            else if (typeInfo.IsDictionary && typeInfo.IsGeneric)
            {
                try
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    
                    var style = new GUIStyle(EditorStyles.boldLabel);
                    style.normal.textColor = new Color(0.4f, 0.7f, 1f);
                    EditorGUILayout.LabelField(memberInfo.Name, style);
                    
                    var fieldValue = memberInfo.GetValue(rootObject);
                    if (fieldValue != null)
                    {
                        var propertyDrawer = GetPropertyDrawer(memberInfo, rootObject);
                        if (propertyDrawer != null)
                        {
                            if (propertyDrawer.GetType().IsGenericType && 
                                propertyDrawer.GetType().GetGenericTypeDefinition().Name.Contains("DictionaryPropertyDrawer"))
                            {
                                var foldOutField = propertyDrawer.GetType().GetField("_foldOut", 
                                    System.Reflection.BindingFlags.NonPublic | 
                                    System.Reflection.BindingFlags.Instance);
                                    
                                if (foldOutField != null)
                                {
                                    foldOutField.SetValue(propertyDrawer, true);
                                }
                                
                                var showNestedValuesProperty = propertyDrawer.GetType().GetProperty("ShowNestedValues");
                                if (showNestedValuesProperty != null)
                                {
                                    showNestedValuesProperty.SetValue(propertyDrawer, true);
                                }
                            }
                            
                            propertyDrawer.OnGUI();
                        }
                    }
                    else
                    {
                        EditorGUILayout.LabelField("null", EditorStyles.miniLabel);
                    }
                }
                finally
                {
                    EditorGUILayout.EndVertical();
                }
                return;
            }
            
            if (memberType.IsClass && !ModelViewerUtils.IsPropertyDrawerTypeExist(memberType) && !memberType.IsSubclassOf(typeof(Object)))
            {
                try
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                    var style = new GUIStyle(EditorStyles.boldLabel);
                    style.normal.textColor = new Color(0.2f, 0.5f, 0.8f);
                    EditorGUILayout.LabelField(memberInfo.Name, style);
                    
                    var fieldValue = memberInfo.GetValue(rootObject);
                    if (fieldValue != null)
                    {
                        DisplayObjectFields(fieldValue);
                    }
                    else
                    {
                        EditorGUILayout.LabelField("null", EditorStyles.miniLabel);
                    }
                }
                finally
                {
                    EditorGUILayout.EndVertical();
                }
                return;
            }

            if (typeInfo.IsComplex && typeInfo.IsGeneric)
            {
                try
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    
                    var style = new GUIStyle(EditorStyles.boldLabel);
                    style.normal.textColor = new Color(0.2f, 0.6f, 0.3f);
                    EditorGUILayout.LabelField(memberInfo.Name + $" ({GetFriendlyTypeName(memberType)})", style);
                    
                    var propertyDrawer = GetPropertyDrawer(memberInfo, rootObject);
                    if (propertyDrawer != null)
                    {
                        propertyDrawer.OnGUI();
                    }
                }
                finally
                {
                    EditorGUILayout.EndVertical();
                }
                return;
            }

            var standardDrawer = GetPropertyDrawer(memberInfo, rootObject);
            standardDrawer?.OnGUI();
        }

        private MemberInfoDrawerBase GetPropertyDrawer(MemberInfo memberInfo, object rootObject)
        {
            MemberInfoDrawerBase memberInfoDrawer = null;
            if (!_activePropertyDrawersDict.TryGetValue(memberInfo, out MemberInfoDrawerBase value))
            {
                var memberType = memberInfo.GetMemberType();

                if (memberType.IsInterface)
                {
                    if (memberInfo.GetValue(rootObject) != null)
                        memberType = memberInfo.GetValue(rootObject).GetType();
                    else
                        return null;
                }

                memberInfoDrawer = DrawerFactory.CreateMemberInfoDrawer(memberInfo, rootObject);
                
                if (memberInfoDrawer != null)
                {
                    _activePropertyDrawersDict.Add(memberInfo, memberInfoDrawer);
                }
            }
            else
                memberInfoDrawer = value;

            return memberInfoDrawer;
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

        private void FindContextAndLoadObjectReferenceFromContext()
        {
            var titleString = PlayerPrefs.GetString(titleContent.text);
            var contextName = titleString.Split('_')[0];
            var objectTypeName = titleString.Split('_')[1];
            var bindingName = titleString.Split('_')[2];

            var context = FindObjectsByType<RootBase>(FindObjectsSortMode.None)
                .Select(x => x.GetContext())
                .ToList()
                .FirstOrDefault(x => x.GetType().Name == contextName);

            if (context != null)
            {
                var binding = context.InjectionBinder
                    .GetAllInjectionBindings()
                    .FirstOrDefault(x => x.Value.GetType().Name == objectTypeName && x.Name == bindingName);

                if (binding == null)
                    binding = context.InjectionBinderCrossContext
                        .GetAllInjectionBindings()
                        .FirstOrDefault(x => x.Value.GetType().Name == objectTypeName && x.Name == bindingName);

                if (binding != null)
                    _inspectedObject = binding.Value;
            }

            _activePropertyDrawersDict = new Dictionary<MemberInfo, MemberInfoDrawerBase>();
        }

        private void OnDestroy()
        {
            _activePropertyDrawersDict = new Dictionary<MemberInfo, MemberInfoDrawerBase>();
        }

        private Type GetMemberType(MemberInfo memberInfo, object rootObject)
        {
            Type memberType = null;
            
            if (memberInfo is FieldInfo fieldInfo)
            {
                memberType = fieldInfo.FieldType;
            }
            else if (memberInfo is PropertyInfo propertyInfo)
            {
                memberType = propertyInfo.PropertyType;
            }
            
            if (memberType != null && memberType.IsInterface)
            {
                var value = memberInfo.GetValue(rootObject);
                if (value != null)
                {
                    memberType = value.GetType();
                }
            }
            
            return memberType;
        }

        private bool IsDeepNestedType(ModelViewerUtils.TypeInfo typeInfo)
        {
            if (typeInfo.GenericArguments == null || typeInfo.GenericArguments.Length == 0)
                return false;
                
            foreach (var argInfo in typeInfo.GenericArguments)
            {
                if (argInfo.IsGeneric || argInfo.IsDictionary || argInfo.IsList)
                    return true;
            }
            
            return false;
        }
    }
}
#endif