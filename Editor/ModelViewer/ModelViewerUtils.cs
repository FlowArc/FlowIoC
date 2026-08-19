#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FlowIoC.BaseModule.Attributes;
using FlowIoC.Editor.ModelViewer.MemberInfoDrawer;
using FlowIoC.Editor.ModelViewer.PropertyDrawer;
using FlowIoC.Editor.ModelViewer.PropertyDrawer.Properties;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FlowIoC.Editor.ModelViewer
{
    internal static class ModelViewerUtils
    {
        private static Dictionary<Type, Type> _memberInfoDrawerTypesDict;
        private static Dictionary<Type, Type> _propertyDrawerTypesDict;

        public static List<MemberInfo> GetTypeMembersList(object rootObject)
        {
            var rootType = rootObject.GetType();
            
            var publicFieldInfoList = rootType
                .GetFields(BindingFlags.Instance | BindingFlags.Public)
                .Where(fieldInfo => 
                    fieldInfo.GetCustomAttributes(typeof(HideInModelViewerAttribute)).ToList().Count == 0)
                .Cast<MemberInfo>()
                .ToList();

            var privateFieldInfoList = rootType
                .GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
                .Where(fieldInfo =>
                    fieldInfo.GetCustomAttributes(typeof(ShowInModelViewerAttribute)).ToList().Count != 0)
                .Cast<MemberInfo>()
                .ToList();
            
            var publicPropertyInfoList = rootType
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(propertyInfo => 
                    propertyInfo.GetCustomAttributes(typeof(HideInModelViewerAttribute)).ToList().Count == 0)
                .Cast<MemberInfo>()
                .ToList();

            var privatePropertyInfoList = rootType
                .GetProperties(BindingFlags.Instance | BindingFlags.NonPublic)
                .Where(propertyInfo =>
                    propertyInfo.GetCustomAttributes(typeof(ShowInModelViewerAttribute)).ToList().Count != 0)
                .Cast<MemberInfo>()
                .ToList();

            var allPropertyInfoList = publicPropertyInfoList.Concat(privatePropertyInfoList).ToList();
            var allFieldInfoList = publicFieldInfoList.Concat(privateFieldInfoList).ToList();
            
            return allFieldInfoList.Concat(allPropertyInfoList).ToList();
        }
        
        public static bool IsPropertyDrawerTypeExist(Type type)
        {
            if(_memberInfoDrawerTypesDict == null)
                InitializeMemberInfoDrawerTypes();
            
            var result = _memberInfoDrawerTypesDict != null && _memberInfoDrawerTypesDict.ContainsKey(type);
            if (result)
                return true;

            if (typeof(IList).IsAssignableFrom(type))
                return true;

            if (typeof(IDictionary).IsAssignableFrom(type))
                return true;
            
            if (type.IsGenericType)
            {
                var genericArgs = type.GetGenericArguments();
                foreach (var arg in genericArgs)
                {
                    if (IsPropertyDrawerTypeExist(arg))
                        return true;
                }
            }
            
            return false;
        }
        
        public static Type GetMemberInfoDrawerType(Type memberInfoType)
        {
            if(_memberInfoDrawerTypesDict == null)
                InitializeMemberInfoDrawerTypes();

            if (memberInfoType.IsSubclassOf(typeof(Object)))
                memberInfoType = typeof(Object);

            Type memberInfoDrawerType = null;
                
            if (_memberInfoDrawerTypesDict != null && !_memberInfoDrawerTypesDict.ContainsKey(memberInfoType))
            {
                if (typeof(Enum).IsAssignableFrom(memberInfoType))
                {
                    memberInfoDrawerType = _memberInfoDrawerTypesDict[typeof(Enum)];
                }
                else if (typeof(IList).IsAssignableFrom(memberInfoType))
                {
                    Type elementType = null;
                    
                    if (memberInfoType.IsGenericType)
                    {
                        var genericArgs = memberInfoType.GetGenericArguments();
                        if (genericArgs.Length > 0)
                        {
                            elementType = genericArgs[0];
                        }
                    }
                    
                    if (elementType == null)
                    {
                        var listMemberInfoDrawer = typeof(MemberInfoDrawerBase)
                            .Assembly
                            .GetTypes()
                            .FirstOrDefault(x => x.Name.Contains("ListMemberInfoDrawer"))
                            ?.MakeGenericType(memberInfoType.GetGenericArguments());

                        memberInfoDrawerType = listMemberInfoDrawer;
                    }
                    else
                    {
                        var listMemberInfoDrawer = typeof(MemberInfoDrawerBase)
                            .Assembly
                            .GetTypes()
                            .FirstOrDefault(x => x.Name.Contains("ListMemberInfoDrawer"))
                            ?.MakeGenericType(memberInfoType.GetGenericArguments());

                        memberInfoDrawerType = listMemberInfoDrawer;
                    }
                }
                else if (typeof(IDictionary).IsAssignableFrom(memberInfoType))
                {
                    var genericArgs = memberInfoType.GetGenericArguments();
                    Type keyType = null;
                    Type valueType = null;
                    
                    if (genericArgs.Length >= 2)
                    {
                        keyType = genericArgs[0];
                        valueType = genericArgs[1];
                    }
                    
                    var dictMemberInfoDrawer = typeof(MemberInfoDrawerBase)
                        .Assembly
                        .GetTypes()
                        .FirstOrDefault(x => x.Name.Contains("DictMemberInfoDrawer"))
                        ?.MakeGenericType(genericArgs);

                    memberInfoDrawerType = dictMemberInfoDrawer;
                    
                    if (valueType != null && valueType.IsGenericType)
                    {
                        // Check if Value types in Dictionary are also supported
                    }
                }
                else
                    return null;
            }
            else if (_memberInfoDrawerTypesDict != null) memberInfoDrawerType = _memberInfoDrawerTypesDict[memberInfoType];

            return memberInfoDrawerType;
        }

        public static Type GetPropertyDrawerType(Type propertyType)
        {
            if(_propertyDrawerTypesDict == null)
                InitializePropertyDrawerTypes();

            if (propertyType.IsSubclassOf(typeof(Object)))
            {
                var objectPropertyDrawer = typeof(MemberInfoDrawerBase)
                    .Assembly
                    .GetTypes()
                    .FirstOrDefault(x => x.Name.Contains("ObjectPropertyDrawer"))
                    ?.MakeGenericType(propertyType);
                return objectPropertyDrawer;
            }

            Type propertyDrawerType = null;
                
            if (_propertyDrawerTypesDict != null && !_propertyDrawerTypesDict.ContainsKey(propertyType))
            {
                if (typeof(IList).IsAssignableFrom(propertyType))
                {
                    var listPropertyDrawer = typeof(MemberInfoDrawerBase)
                        .Assembly
                        .GetTypes()
                        .FirstOrDefault(x => x.Name.Contains("ListPropertyDrawer"))
                        ?.MakeGenericType(propertyType.GetGenericArguments());

                    propertyDrawerType = listPropertyDrawer;
                }
                else if (propertyType.IsClass)
                {
                    var classPropertyDrawer = typeof(MemberInfoDrawerBase)
                        .Assembly
                        .GetTypes()
                        .FirstOrDefault(x => x.Name.Contains("ClassPropertyDrawer"))
                        ?.MakeGenericType(propertyType);
                    return classPropertyDrawer;
                }
                else if (propertyType.IsEnum)
                {
                    return typeof(EnumPropertyDrawer);
                }
                else    
                    return null;
            }
            else if (_propertyDrawerTypesDict != null) propertyDrawerType = _propertyDrawerTypesDict[propertyType];

            return propertyDrawerType;
        }
        
        private static void InitializeMemberInfoDrawerTypes()
        {
            _memberInfoDrawerTypesDict = new Dictionary<Type, Type>();
            
            var propertyDrawerTypes = typeof(MemberInfoDrawerBase).Assembly.GetTypes()
                .Where(x => x.IsSubclassOf(typeof(MemberInfoDrawerBase)))
                .ToList();

            foreach (var propertyDrawerType in propertyDrawerTypes)
            {
                var drawerType = propertyDrawerType.GetProperty("PropertyType")?.PropertyType;
                if (drawerType != null) _memberInfoDrawerTypesDict.Add(drawerType, propertyDrawerType);
            }
        }

        private static void InitializePropertyDrawerTypes()
        {
            _propertyDrawerTypesDict = new Dictionary<Type, Type>();
            
            var propertyDrawerTypes = typeof(PropertyDrawerBase)
                .Assembly
                .GetTypes()
                .Where(x => x.IsSubclassOf(typeof(PropertyDrawerBase)))
                .ToList();

            foreach (var propertyDrawerType in propertyDrawerTypes)
            {
                var drawerType = propertyDrawerType.GetProperty("PropertyType")?.PropertyType;
                if(drawerType == null)
                    continue;
                
                _propertyDrawerTypesDict.Add(drawerType, propertyDrawerType);
            }
        }
        
        public static Type GetMemberType(this MemberInfo memberInfo)
        {
            if (memberInfo.MemberType == MemberTypes.Field)
                return (memberInfo as FieldInfo)?.FieldType;
            else if(memberInfo.MemberType == MemberTypes.Property)
                return (memberInfo as PropertyInfo)?.PropertyType;

            return null;
        }

        public static object GetValue(this MemberInfo memberInfo, object rootObject)
        {
            return memberInfo.MemberType switch
            {
                MemberTypes.Field => (memberInfo as FieldInfo)?.GetValue(rootObject),
                MemberTypes.Property => (memberInfo as PropertyInfo)?.GetValue(rootObject),
                _ => null
            };
        }

        public static void SetValue(this MemberInfo memberInfo, object rootObject, object newValue)
        {
            if (memberInfo.MemberType == MemberTypes.Field)
                (memberInfo as FieldInfo)?.SetValue(rootObject, newValue);
            else if(memberInfo.MemberType == MemberTypes.Property)
                (memberInfo as PropertyInfo)?.SetValue(rootObject, newValue);
        }

 
        public static bool IsComplexType(Type type)
        {
            if (type == null)
                return false;
            
            if (type.IsPrimitive || type == typeof(string) || type == typeof(decimal) || 
                type.IsEnum || type == typeof(DateTime) || type == typeof(Guid))
                return false;
            
            if (type.IsArray)
                return IsComplexType(type.GetElementType());
            
            if (type.IsGenericType)
            {
                if (typeof(IDictionary).IsAssignableFrom(type) || typeof(IList).IsAssignableFrom(type))
                {
                    var genericArgs = type.GetGenericArguments();
                    foreach (var arg in genericArgs)
                    {
                        if (IsComplexType(arg))
                            return true;
                    }
                }
                
                return true;
            }
            
            if (type.IsClass || type.IsValueType)
            {
                if (type.IsSubclassOf(typeof(UnityEngine.Object)))
                    return false;
                
                if (type.IsValueType && type.IsPrimitive)
                    return false;
                
                if (type.Namespace != null && type.Namespace.StartsWith("System") && !type.IsGenericType)
                    return false;
                
                return true;
            }
            
            return false;
        }

        public static TypeInfo AnalyzeType(Type type)
        {
            TypeInfo info = new TypeInfo();
            info.Type = type;
            info.IsComplex = IsComplexType(type);
            
            if (type == null)
                return info;
            
            info.IsPrimitive = type.IsPrimitive || type == typeof(string) || type == typeof(decimal) || 
                               type.IsEnum || type == typeof(DateTime) || type == typeof(Guid);
            
            info.IsList = typeof(IList).IsAssignableFrom(type);
            info.IsDictionary = typeof(IDictionary).IsAssignableFrom(type);
            
            if (type.IsGenericType)
            {
                info.IsGeneric = true;
                info.GenericTypeDefinition = type.GetGenericTypeDefinition();
                
                Type[] genericArgs = type.GetGenericArguments();
                info.GenericArguments = new TypeInfo[genericArgs.Length];
                
                for (int i = 0; i < genericArgs.Length; i++)
                {
                    info.GenericArguments[i] = AnalyzeType(genericArgs[i]);
                }
            }
            
            return info;
        }

        public static Type GetAdvancedPropertyDrawerType(Type propertyType)
        {
            if (propertyType == null)
                return null;
            
            TypeInfo typeInfo = AnalyzeType(propertyType);
            
            if (propertyType.IsSubclassOf(typeof(UnityEngine.Object)))
            {
                var objectPropertyDrawer = typeof(MemberInfoDrawerBase)
                    .Assembly
                    .GetTypes()
                    .FirstOrDefault(x => x.Name.Contains("ObjectPropertyDrawer"))
                    ?.MakeGenericType(propertyType);
                return objectPropertyDrawer;
            }
            
            if (typeInfo.IsDictionary && typeInfo.IsGeneric && typeInfo.GenericArguments.Length >= 2)
            {
                var dictPropertyDrawer = typeof(MemberInfoDrawerBase)
                    .Assembly
                    .GetTypes()
                    .FirstOrDefault(x => x.Name.Contains("DictionaryPropertyDrawer"))
                    ?.MakeGenericType(typeInfo.Type.GetGenericArguments());
                
                return dictPropertyDrawer;
            }
            
            if (typeInfo.IsList && typeInfo.IsGeneric && typeInfo.GenericArguments.Length >= 1)
            {
                var listPropertyDrawer = typeof(MemberInfoDrawerBase)
                    .Assembly
                    .GetTypes()
                    .FirstOrDefault(x => x.Name.Contains("ListPropertyDrawer"))
                    ?.MakeGenericType(typeInfo.Type.GetGenericArguments());
                
                return listPropertyDrawer;
            }
            
            if (propertyType.IsEnum)
            {
                return typeof(MemberInfoDrawerBase)
                    .Assembly
                    .GetTypes()
                    .FirstOrDefault(x => x.Name.Contains("EnumPropertyDrawer"));
            }
            
            if (propertyType.IsClass)
            {
                var classPropertyDrawer = typeof(MemberInfoDrawerBase)
                    .Assembly
                    .GetTypes()
                    .FirstOrDefault(x => x.Name.Contains("ClassPropertyDrawer"))
                    ?.MakeGenericType(propertyType);
                return classPropertyDrawer;
            }
            
            return GetPropertyDrawerType(propertyType);
        }

        public static void VisualizeDeepNestedType(object value, int maxDepth = 5, int currentDepth = 0)
        {
            if (value == null)
            {
                EditorGUILayout.LabelField("null", EditorStyles.miniLabel);
                return;
            }
            
            if (currentDepth >= maxDepth)
            {
                EditorGUILayout.LabelField($"{TruncateValueForDisplay(value)} (maximum depth reached)", EditorStyles.miniLabel);
                return;
            }
            
            Type type = value.GetType();
            TypeInfo typeInfo = AnalyzeType(type);
            
            if (typeInfo.IsDictionary)
            {
                IDictionary dict = value as IDictionary;
                if (dict == null) return;
                
                string typeDisplay = GetFriendlyTypeName(type);
                string foldoutId = $"Dict_{typeDisplay}_{value.GetHashCode()}_{currentDepth}";
                bool foldout = EditorGUILayout.Foldout(
                    SessionState.GetBool(foldoutId, false),
                    $"{typeDisplay} ({dict.Count} items)",
                    true);
                
                SessionState.SetBool(foldoutId, foldout);
                
                if (foldout && dict.Count > 0)
                {
                    EditorGUI.indentLevel++;
                    
                    foreach (DictionaryEntry entry in dict)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("Key:", GUILayout.Width(40));
                        EditorGUILayout.LabelField(TruncateValueForDisplay(entry.Key), EditorStyles.boldLabel);
                        EditorGUILayout.EndHorizontal();
                        
                        EditorGUI.indentLevel++;
                        VisualizeDeepNestedType(entry.Value, maxDepth, currentDepth + 1);
                        EditorGUI.indentLevel--;
                    }
                    
                    EditorGUI.indentLevel--;
                }
            }
            else if (typeInfo.IsList)
            {
                IList list = value as IList;
                if (list == null) return;
                
                string typeDisplay = GetFriendlyTypeName(type);
                string foldoutId = $"List_{typeDisplay}_{value.GetHashCode()}_{currentDepth}";
                bool foldout = EditorGUILayout.Foldout(
                    SessionState.GetBool(foldoutId, false),
                    $"{typeDisplay} ({list.Count} items)",
                    true);
                
                SessionState.SetBool(foldoutId, foldout);
                
                if (foldout && list.Count > 0)
                {
                    EditorGUI.indentLevel++;
                    
                    for (int i = 0; i < list.Count; i++)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField($"[{i}]:", GUILayout.Width(40));
                        EditorGUILayout.EndHorizontal();
                        
                        EditorGUI.indentLevel++;
                        VisualizeDeepNestedType(list[i], maxDepth, currentDepth + 1);
                        EditorGUI.indentLevel--;
                    }
                    
                    EditorGUI.indentLevel--;
                }
            }
            else if (typeInfo.IsGeneric && !typeInfo.IsList && !typeInfo.IsDictionary)
            {
                string typeDisplay = GetFriendlyTypeName(type);
                string foldoutId = $"GenericType_{typeDisplay}_{value.GetHashCode()}_{currentDepth}";
                bool foldout = EditorGUILayout.Foldout(
                    SessionState.GetBool(foldoutId, false),
                    $"{typeDisplay}",
                    true);
                
                SessionState.SetBool(foldoutId, foldout);
                
                if (foldout)
                {
                    EditorGUI.indentLevel++;
                    
                    var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                    foreach (var prop in properties)
                    {
                        if (prop.CanRead)
                        {
                            object propValue = prop.GetValue(value);
                            EditorGUILayout.LabelField(prop.Name + ":", EditorStyles.boldLabel);
                            
                            EditorGUI.indentLevel++;
                            VisualizeDeepNestedType(propValue, maxDepth, currentDepth + 1);
                            EditorGUI.indentLevel--;
                        }
                    }
                    
                    EditorGUI.indentLevel--;
                }
            }
            else
            {
                EditorGUILayout.LabelField(TruncateValueForDisplay(value));
            }
        }
        
        private static string TruncateValueForDisplay(object value)
        {
            if (value == null)
                return "null";
                
            if (value is string str)
            {
                if (str.Length > 100)
                    return str.Substring(0, 97) + "...";
                else
                    return str;
            }
            
            if (IsComplexType(value.GetType()))
            {
                return $"{value.GetType().Name} instance";
            }
            
            return value.ToString();
        }
        
        private static string GetFriendlyTypeName(Type type)
        {
            if (!type.IsGenericType)
                return type.Name;
                
            var genericArgs = type.GetGenericArguments();
            var baseType = type.Name;
            var indexOfBacktick = baseType.IndexOf('`');
            
            if (indexOfBacktick > 0)
                baseType = baseType.Substring(0, indexOfBacktick);
                
            string args = string.Join(", ", genericArgs.Select(arg => GetFriendlyTypeName(arg)));
            return $"{baseType}<{args}>";
        }

        public class TypeInfo
        {
            public Type Type { get; set; }
            public bool IsComplex { get; set; }
            public bool IsPrimitive { get; set; }
            public bool IsGeneric { get; set; }
            public bool IsList { get; set; }
            public bool IsDictionary { get; set; }
            public Type GenericTypeDefinition { get; set; }
            public TypeInfo[] GenericArguments { get; set; }
            
            public override string ToString()
            {
                return $"{Type.Name} (Complex: {IsComplex}, Generic: {IsGeneric}, List: {IsList}, Dictionary: {IsDictionary})";
            }
        }
    }
}
#endif