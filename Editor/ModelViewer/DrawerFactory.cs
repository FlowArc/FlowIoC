#if UNITY_EDITOR
using System;
using System.Collections;
using System.Reflection;
using FlowIoC.Editor.ModelViewer.MemberInfoDrawer;
using FlowIoC.Editor.ModelViewer.PropertyDrawer;

namespace FlowIoC.Editor.ModelViewer
{

    internal static class DrawerFactory
    {
        public static MemberInfoDrawerBase CreateMemberInfoDrawer(MemberInfo memberInfo, object targetObject)
        {
            var memberType = memberInfo.GetMemberType();
            
            if (memberType == null)
                return null;
                
            Type drawerType = GetMemberInfoDrawerType(memberType);
            
            if (drawerType == null)
                return null;
                
            return (MemberInfoDrawerBase)Activator.CreateInstance(drawerType, memberInfo, targetObject);
        }
        
        public static PropertyDrawerBase CreatePropertyDrawer(Type propertyType, string fieldName, bool readOnly)
        {
            if (propertyType == null)
                return null;
                
            Type drawerType = GetPropertyDrawerType(propertyType);
            
            if (drawerType == null)
                return null;
                
            return (PropertyDrawerBase)Activator.CreateInstance(drawerType, fieldName, readOnly);
        }
        
        private static Type GetMemberInfoDrawerType(Type memberType)
        {
            var typeInfo = ModelViewerUtils.AnalyzeType(memberType);
            
            if (memberType.IsSubclassOf(typeof(UnityEngine.Object)))
            {
                return typeof(MemberInfoDrawer.Properties.ObjectMemberInfoDrawer);
            }
            
            if (typeInfo.IsComplex && typeInfo.IsGeneric)
            {
                var genericMemberDrawerType = typeof(MemberInfoDrawer.Properties.GenericMemberInfoDrawer<>)
                    .MakeGenericType(memberType);
                    
                return genericMemberDrawerType;
            }
            
            if (typeof(IDictionary).IsAssignableFrom(memberType) && memberType.IsGenericType)
            {
                var dictMemberInfoDrawer = typeof(MemberInfoDrawer.Properties.DictMemberInfoDrawer<,>)
                    .MakeGenericType(memberType.GetGenericArguments());
                    
                return dictMemberInfoDrawer;
            }
            
            if (typeof(IList).IsAssignableFrom(memberType) && memberType.IsGenericType)
            {
                var listMemberInfoDrawer = typeof(MemberInfoDrawer.Properties.ListMemberInfoDrawer<>)
                    .MakeGenericType(memberType.GetGenericArguments());
                    
                return listMemberInfoDrawer;
            }
            
            return ModelViewerUtils.GetMemberInfoDrawerType(memberType);
        }
        
        private static Type GetPropertyDrawerType(Type propertyType)
        {
            var typeInfo = ModelViewerUtils.AnalyzeType(propertyType);
            
            if (propertyType.IsSubclassOf(typeof(UnityEngine.Object)))
            {
                return typeof(PropertyDrawer.Properties.ObjectPropertyDrawer<>).MakeGenericType(propertyType);
            }
            
            if (typeInfo.IsComplex && typeInfo.IsGeneric)
            {
                var genericPropertyDrawerType = typeof(PropertyDrawer.Properties.GenericTypeDrawer<>)
                    .MakeGenericType(propertyType);
                    
                return genericPropertyDrawerType;
            }
            
            if (typeof(IDictionary).IsAssignableFrom(propertyType) && propertyType.IsGenericType)
            {
                var dictPropertyDrawer = typeof(PropertyDrawer.Properties.DictionaryPropertyDrawer<,>)
                    .MakeGenericType(propertyType.GetGenericArguments());
                    
                return dictPropertyDrawer;
            }
            
            if (typeof(IList).IsAssignableFrom(propertyType) && propertyType.IsGenericType)
            {
                var listPropertyDrawer = typeof(PropertyDrawer.Properties.ListPropertyDrawer<>)
                    .MakeGenericType(propertyType.GetGenericArguments());
                    
                return listPropertyDrawer;
            }
            
            return ModelViewerUtils.GetPropertyDrawerType(propertyType);
        }
    }
}
#endif 