using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FlowIoC.BaseModule.Contexts;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Function;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.Injectable.CrossContext;
using FlowIoC.BaseModule.Signals;
using FlowIoC.BaseModule.ViewsMediators.Mediator;
using FlowIoC.BaseModule.ViewsMediators.View;
using FlowIoC.ConsoleModule;
using UnityEngine;

namespace FlowIoC.BaseModule.Injectable.Utils
{
    public static class InjectionExtensions
    {
        private static Dictionary<Type, CachedInjectableData> _cachedInjectableData = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => _cachedInjectableData = new();

        private class CachedInjectableData
        {
            public Dictionary<Type, List<FieldInfo>> CachedFieldInfoList = new();
            public Dictionary<Type, List<PropertyInfo>> CachedPropertyInfoList = new();
        }

        internal static bool TryToInjectObject(this InjectionBinding binding)
        {
            //List<FieldInfo> injectableFields = GetInjectableFieldInfoList<InjectAttribute>(binding.Value);
            List<PropertyInfo> injectableProperties = GetInjectablePropertyInfoList<InjectAttribute>(binding.Value);

            //List<FieldInfo> signalFields = GetInjectableFieldInfoList<InjectSignalAttribute>(binding.Value);
            List<PropertyInfo> signalProperties = GetInjectablePropertyInfoList<InjectSignalAttribute>(binding.Value);

            // foreach (FieldInfo field in injectableFields)
            // {
            //     if (IsSignalType(field.FieldType))
            //     {
            //         LogGeneralInjectionError(binding.Value, field, field.FieldType, "Signals must use [InjectSignal]!");
            //         continue;
            //     }
            //
            //     SetInjectedValue(binding.Value, binding.BindedContext, field);
            // }

            foreach (PropertyInfo property in injectableProperties)
            {
                if (IsSignalType(property.PropertyType))
                {
                    LogGeneralInjectionError(binding.Value, property, property.PropertyType, "Signals must use [InjectSignal]!");
                    continue;
                }

                SetInjectedValue(binding.Value, binding.BindedContext, property);
            }

            // foreach (FieldInfo field in signalFields)
            // {
            //     if (!IsSignalType(field.FieldType))
            //     {
            //         LogGeneralInjectionError(binding.Value, field, field.FieldType, "[InjectSignal] can only be used with Signal types!");
            //         continue;
            //     }
            //
            //     SetInjectedValue(binding.Value, binding.BindedContext, field);
            //     LogGeneralInjectionSuccess(binding.Value, field, field.FieldType, "Signal successfully injected");
            // }

            foreach (PropertyInfo property in signalProperties)
            {
                if (!IsSignalType(property.PropertyType))
                {
                    LogGeneralInjectionError(binding.Value, property, property.PropertyType, "[InjectSignal] can only be used with Signal types!");
                    continue;
                }

                SetInjectedValue(binding.Value, binding.BindedContext, property);
                LogGeneralInjectionSuccess(binding.Value, property, property.PropertyType, "Signal successfully injected");
            }

            return true;
        }

        internal static bool TryToInjectObject(this IContext context, object injectedObject)
        {
            //List<FieldInfo> injectableFields = GetInjectableFieldInfoList<InjectAttribute>(injectedObject);
            List<PropertyInfo> injectableProperties = GetInjectablePropertyInfoList<InjectAttribute>(injectedObject);

            //List<FieldInfo> signalFields = GetInjectableFieldInfoList<InjectSignalAttribute>(injectedObject);
            List<PropertyInfo> signalProperties = GetInjectablePropertyInfoList<InjectSignalAttribute>(injectedObject);

            // foreach (FieldInfo field in injectableFields)
            // {
            //     if (IsSignalType(field.FieldType))
            //     {
            //         LogGeneralInjectionError(injectedObject, field, field.FieldType, "Signals must use [InjectSignal]!");
            //         continue;
            //     }
            //
            //     SetInjectedValue(injectedObject, context, field);
            // }

            foreach (PropertyInfo property in injectableProperties)
            {
                if (IsSignalType(property.PropertyType))
                {
                    LogGeneralInjectionError(injectedObject, property, property.PropertyType, "Signals must use [InjectSignal]!");
                    continue;
                }

                SetInjectedValue(injectedObject, context, property);
            }

            // foreach (FieldInfo field in signalFields)
            // {
            //     if (!IsSignalType(field.FieldType))
            //     {
            //         LogGeneralInjectionError(injectedObject, field, field.FieldType, "[InjectSignal] can only be used with Signal types!");
            //         continue;
            //     }
            //
            //     SetInjectedValue(injectedObject, context, field);
            //     LogGeneralInjectionSuccess(injectedObject, field, field.FieldType, "Signal successfully injected");
            // }

            foreach (PropertyInfo property in signalProperties)
            {
                if (!IsSignalType(property.PropertyType))
                {
                    LogGeneralInjectionError(injectedObject, property, property.PropertyType, "[InjectSignal] can only be used with Signal types!");
                    continue;
                }

                SetInjectedValue(injectedObject, context, property);
                LogGeneralInjectionSuccess(injectedObject, property, property.PropertyType, "Signal successfully injected");
            }

            return true;
        }

        #region InjectMediators

        internal static bool TryToInjectMediator(this IContext context, IMediator mediator, IView view)
        {
            //InjectMediatorFields(mediator, view, context);
            InjectMediatorProperties(mediator, view, context);

            mediator.OnRegister();
            return true;
        }

        private static void InjectMediatorFields(object mediator, object view, IContext context)
        {
            Type viewType = view.GetType();

            //List<FieldInfo> signalFields = GetInjectableFieldInfoList<InjectSignalAttribute>(mediator);
            //List<FieldInfo> viewFields = GetInjectableFieldInfoList<InjectAttribute>(mediator);

            // foreach (FieldInfo fieldInfo in viewFields)
            // {
            //     if (fieldInfo.FieldType == viewType || viewType.IsSubclassOf(fieldInfo.FieldType) ||
            //         (fieldInfo.FieldType.IsInterface && fieldInfo.FieldType.IsAssignableFrom(viewType)))
            //     {
            //         fieldInfo.SetValue(mediator, view);
            //         LogMediatorViewInjection(mediator, fieldInfo, fieldInfo.FieldType);
            //         continue;
            //     }
            //
            //     LogMediatorInjectionError(mediator, fieldInfo, fieldInfo.FieldType,
            //         $"[Inject] can only be used for View injection in Mediators! Current type is {fieldInfo.FieldType.Name}");
            // }
            //
            // foreach (FieldInfo fieldInfo in signalFields)
            // {
            //     if (!IsSignalType(fieldInfo.FieldType))
            //     {
            //         LogMediatorInjectionError(mediator, fieldInfo, fieldInfo.FieldType,
            //             $"[InjectSignal] can only be used with Signal types! Current type is {fieldInfo.FieldType.Name}");
            //         continue;
            //     }
            //
            //     object injectionValue = context.GetInjectedObject(fieldInfo);
            //     if (injectionValue == null)
            //     {
            //         LogMediatorInjectionError(mediator, fieldInfo, fieldInfo.FieldType, $"Signal instance not found in context!");
            //         continue;
            //     }
            //
            //     fieldInfo.SetValue(mediator, injectionValue);
            //     LogMediatorInjectionSuccess(mediator, fieldInfo, fieldInfo.FieldType, $"Signal successfully injected");
            // }
        }

        private static void InjectMediatorProperties(object mediator, object view, IContext context)
        {
            Type viewType = view.GetType();

            List<PropertyInfo> signalProperties = GetInjectablePropertyInfoList<InjectSignalAttribute>(mediator);
            List<PropertyInfo> viewProperties = GetInjectablePropertyInfoList<InjectAttribute>(mediator);

            foreach (PropertyInfo propertyInfo in viewProperties)
            {
                if (propertyInfo.PropertyType == viewType || viewType.IsSubclassOf(propertyInfo.PropertyType) ||
                    (propertyInfo.PropertyType.IsInterface && propertyInfo.PropertyType.IsAssignableFrom(viewType)))
                {
                    propertyInfo.SetValue(mediator, view);
                    LogMediatorViewInjection(mediator, propertyInfo, propertyInfo.PropertyType);
                    continue;
                }

                LogMediatorInjectionError(mediator, propertyInfo, propertyInfo.PropertyType,
                    $"[Inject] can only be used for View injection in Mediators! Current type is {propertyInfo.PropertyType.Name}");
            }

            foreach (PropertyInfo propertyInfo in signalProperties)
            {
                if (!IsSignalType(propertyInfo.PropertyType))
                {
                    LogMediatorInjectionError(mediator, propertyInfo, propertyInfo.PropertyType,
                        $"[InjectSignal] can only be used with Signal types! Current type is {propertyInfo.PropertyType.Name}");
                    continue;
                }

                object injectionValue = context.GetInjectedObject(propertyInfo);
                if (injectionValue == null)
                {
                    LogMediatorInjectionError(mediator, propertyInfo, propertyInfo.PropertyType,
                        $"Signal instance not found in context!");
                    continue;
                }

                propertyInfo.SetValue(mediator, injectionValue);
                LogMediatorInjectionSuccess(mediator, propertyInfo, propertyInfo.PropertyType,
                    $"Signal successfully injected");
            }
        }

        #endregion

        #region InjectFunctions

        internal static void TryToInjectFunction(this IContext context, IFunctionBody functionBody)
        {
            //List<FieldInfo> injectableFields = GetInjectableFieldInfoList<InjectAttribute>(functionBody);
            List<PropertyInfo> injectableProperties = GetInjectablePropertyInfoList<InjectAttribute>(functionBody);

            //List<FieldInfo> signalFields = GetInjectableFieldInfoList<InjectSignalAttribute>(functionBody);
            List<PropertyInfo> signalProperties = GetInjectablePropertyInfoList<InjectSignalAttribute>(functionBody);

            // foreach (FieldInfo field in injectableFields)
            // {
            //     if (IsSignalType(field.FieldType))
            //     {
            //         LogFunctionInjectionError(functionBody, field, field.FieldType, "Signals must use [InjectSignal]!");
            //         continue;
            //     }
            //
            //     SetInjectedValue(functionBody, context, field);
            // }

            foreach (PropertyInfo property in injectableProperties)
            {
                if (IsSignalType(property.PropertyType))
                {
                    LogFunctionInjectionError(functionBody, property, property.PropertyType, "Signals must use [InjectSignal]!");
                    continue;
                }

                SetInjectedValue(functionBody, context, property);
            }

            // foreach (FieldInfo field in signalFields)
            // {
            //     if (!IsSignalType(field.FieldType))
            //     {
            //         LogFunctionInjectionError(functionBody, field, field.FieldType, "[InjectSignal] can only be used with Signal types!");
            //         continue;
            //     }
            //
            //     SetInjectedValue(functionBody, context, field);
            //     LogFunctionInjectionSuccess(functionBody, field, field.FieldType, "Signal successfully injected");
            // }

            foreach (PropertyInfo property in signalProperties)
            {
                if (!IsSignalType(property.PropertyType))
                {
                    LogFunctionInjectionError(functionBody, property, property.PropertyType, "[InjectSignal] can only be used with Signal types!");
                    continue;
                }

                SetInjectedValue(functionBody, context, property);
                LogFunctionInjectionSuccess(functionBody, property, property.PropertyType, "Signal successfully injected");
            }
        }

        private static void LogFunctionInjectionError(object function, MemberInfo member, Type memberType, string message)
        {
            FlowLogger.LogError(SystemLogType.Function,
                "<b><color=#FF6666>► FUNCTION INJECTION FAILED!</color></b> " + message + "\n" +
                "<b><color=#FF6666>► Function:</color><color=#FFEFD5> " + function.GetType().Name + "</color></b>\n" +
                "<b><color=#FF6666>► Member:</color><color=#FFEFD5> " + member.Name + "</color></b>\n" +
                "<b><color=#FF6666>► Type:</color><color=#FFEFD5> " + memberType?.Name + "</color></b>"
                );
        }

        private static void LogFunctionInjectionSuccess(object function, MemberInfo member, Type memberType, string message)
        {
            // FlowConsoleLogger.Log(ConsoleLogType.Function, 
            //     "► INJECTION SUCCESS! " + message + "\n" +
            //     "► Function: " + function.GetType().Name + "\n" +
            //     "► Member: " + member.Name + "\n" +
            //     "► Type: " + memberType?.Name
            //     );
        }

        #endregion

        #region InjectCommands

        internal static void InjectCommand(this IContext context, ICommandBody command, params object[] signalParams)
        {
            //List<FieldInfo> injectableFields = GetInjectableFieldInfoList<InjectAttribute>(command);
            List<PropertyInfo> injectableProperties = GetInjectablePropertyInfoList<InjectAttribute>(command);

            //List<FieldInfo> signalFields = GetInjectableFieldInfoList<InjectSignalAttribute>(command);
            List<PropertyInfo> signalProperties = GetInjectablePropertyInfoList<InjectSignalAttribute>(command);

            // foreach (FieldInfo field in injectableFields)
            // {
            //     if (IsSignalType(field.FieldType))
            //     {
            //         LogCommandInjectionError(command, field, field.FieldType, "Signals must use [InjectSignal]!");
            //         continue;
            //     }
            //
            //     SetInjectedValue(command, context, field);
            // }

            foreach (PropertyInfo property in injectableProperties)
            {
                if (IsSignalType(property.PropertyType))
                {
                    LogCommandInjectionError(command, property, property.PropertyType, "Signals must use [InjectSignal]!");
                    continue;
                }

                SetInjectedValue(command, context, property);
            }

            // foreach (FieldInfo field in signalFields)
            // {
            //     if (!IsSignalType(field.FieldType))
            //     {
            //         LogCommandInjectionError(command, field, field.FieldType, "[InjectSignal] can only be used with Signal types!");
            //         continue;
            //     }
            //
            //     SetInjectedValue(command, context, field);
            //     LogCommandInjectionSuccess(command, field, field.FieldType, "Signal successfully injected");
            // }

            foreach (PropertyInfo property in signalProperties)
            {
                if (!IsSignalType(property.PropertyType))
                {
                    LogCommandInjectionError(command, property, property.PropertyType, "[InjectSignal] can only be used with Signal types!");
                    continue;
                }

                SetInjectedValue(command, context, property);
                LogCommandInjectionSuccess(command, property, property.PropertyType, "Signal successfully injected");
            }

            InjectSignalParamsToCommand(context, command, signalParams);
        }

        private static void LogCommandInjectionError(object command, MemberInfo member, Type memberType, string message)
        {
            FlowLogger.LogError(SystemLogType.CommandOperation,
                "<b><color=#FF6666>► COMMAND INJECTION FAILED!</color></b> " + message + "\n" +
                "<b><color=#FF6666>► Command:</color><color=#FFEFD5> " + command.GetType().Name + "</color></b>\n" +
                "<b><color=#FF6666>► Member:</color><color=#FFEFD5> " + member.Name + "</color></b>\n" +
                "<b><color=#FF6666>► Type:</color><color=#FFEFD5> " + memberType?.Name + "</color></b>"
            );
        }

        private static void LogCommandInjectionSuccess(object command, MemberInfo member, Type memberType, string message)
        {
            // FlowConsoleLogger.Log(ConsoleLogType.Command, 
            //     "► COMMAND INJECTION SUCCESS! " + message + "\n" +
            //     "► Command: " + command.GetType().Name + "\n" +
            //     "► Member: " + member.Name + "\n" +
            //     "► Type: " + memberType?.Name
            //     );
        }

        private static void InjectSignalParamsToCommand(IContext context, ICommandBody command, params object[] signalParams)
        {
            if (signalParams == null || signalParams.Length == 0)
                return;

            //List<FieldInfo> signalFields = GetInjectableFieldInfoList<SignalParamAttribute>(command);
            List<PropertyInfo> signalProperties = GetInjectablePropertyInfoList<SignalParamAttribute>(command);

            AssignSignalParameters(command, signalProperties, signalParams);
        }

        private static void AssignSignalParameters(object command, IEnumerable<PropertyInfo> signalProperties, IEnumerable<object> signalParams)
        {
            IEnumerable<object> enumerable = signalParams.ToList();
            // foreach (FieldInfo signalField in signalFields)
            // {
            //     if (TryGetSignalParameter(enumerable, signalField.FieldType, out object param))
            //     {
            //         signalField.SetValue(command, param);
            //     }
            //     else
            //     {
            //         LogSignalParamError(command, signalField.FieldType);
            //     }
            // }

            foreach (PropertyInfo signalProperty in signalProperties)
            {
                if (TryGetSignalParameter(enumerable, signalProperty.PropertyType, out object param))
                {
                    signalProperty.SetValue(command, param);
                }
                else
                {
                    LogSignalParamError(command, signalProperty.PropertyType);
                }
            }
        }

        private static bool TryGetSignalParameter(IEnumerable<object> signalParams, Type paramType, out object param)
        {
            IEnumerable<object> enumerable = signalParams.ToList();
            param = enumerable.FirstOrDefault(x => x != null && x.GetType() == paramType);
            if (param == null)
            {
                param = enumerable.FirstOrDefault(x => x != null && paramType.IsInstanceOfType(x));
            }

            return param != null;
        }

        private static void LogSignalParamError(object command, Type paramType)
        {
            FlowLogger.LogError(SystemLogType.CommandOperation,
            "<b><color=#FF6666>► Signal Param is not found!</color></b>\n" +
                "<b><color=#FF6666>► Command:</color><color=#FFEFD5> " + command.GetType().Name + "</color></b>\n" +
                "<b><color=#FF6666>► ParamType:</color><color=#FFEFD5> " + paramType.Name + "</color></b>",
            
            "► Signal Param is not found!\n" +
                "► Command: " + command.GetType().Name + "\n" +
                "► ParamType: " + paramType.Name
            );
        }

        #endregion

        #region GetInjectable Fields-Properties

        // private static List<FieldInfo> GetInjectableFieldInfoList<TAttribute>(object instance)
        //     where TAttribute : Attribute
        // {
        //     Type instanceType = instance.GetType();
        //     List<Type> injectableTypes = instanceType.GetAllChildClasses();
        //
        //     if (!injectableTypes.Contains(instanceType))
        //         injectableTypes.Add(instanceType);
        //
        //     if (!_cachedInjectableData.ContainsKey(instanceType))
        //         _cachedInjectableData.Add(instanceType, new CachedInjectableData());
        //
        //     if (!_cachedInjectableData[instanceType].CachedFieldInfoList.ContainsKey(typeof(TAttribute)))
        //     {
        //         List<FieldInfo> injectableFields = new List<FieldInfo>();
        //         foreach (Type injectableType in injectableTypes)
        //         {
        //             List<FieldInfo> fields = injectableType
        //                 .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
        //                 .Where(x =>
        //                     x.GetCustomAttributes(typeof(TAttribute)).ToList().Count != 0)
        //                 .ToList();
        //
        //             injectableFields = injectableFields.Concat(fields).ToList();
        //         }
        //
        //         _cachedInjectableData[instanceType].CachedFieldInfoList.Add(typeof(TAttribute), injectableFields);
        //         return injectableFields;
        //     }
        //
        //     return _cachedInjectableData[instanceType].CachedFieldInfoList[typeof(TAttribute)];
        // }

        private static List<PropertyInfo> GetInjectablePropertyInfoList<TAttribute>(object instance)
            where TAttribute : Attribute
        {
            Type instanceType = instance.GetType();
            List<Type> injectableTypes = instanceType.GetAllChildClasses();

            if (!injectableTypes.Contains(instanceType))
                injectableTypes.Add(instanceType);

            if (!_cachedInjectableData.ContainsKey(instanceType))
                _cachedInjectableData.Add(instanceType, new CachedInjectableData
                {
                });

            if (!_cachedInjectableData[instanceType].CachedPropertyInfoList.ContainsKey(typeof(TAttribute)))
            {
                List<PropertyInfo> injectableProperties = new List<PropertyInfo>();
                foreach (Type injectableType in injectableTypes)
                {
                    List<PropertyInfo> properties = injectableType
                        .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                        .Where(x =>
                            x.GetCustomAttributes(typeof(TAttribute)).ToList().Count != 0)
                        .ToList();

                    injectableProperties = injectableProperties.Concat(properties).ToList();
                }

                _cachedInjectableData[instanceType].CachedPropertyInfoList.Add(typeof(TAttribute), injectableProperties);
                return injectableProperties;
            }

            return _cachedInjectableData[instanceType].CachedPropertyInfoList[typeof(TAttribute)];
        }

        #endregion

        #region Mediator Injection Helpers

        private static Type GetMemberType(MemberInfo memberInfo)
        {
            if (memberInfo is FieldInfo fieldInfo)
                return fieldInfo.FieldType;
            if (memberInfo is PropertyInfo propertyInfo)
                return propertyInfo.PropertyType;
            return null;
        }

        private static bool IsSignalType(Type memberType)
        {
            return memberType != null && typeof(ISignalHolder).IsAssignableFrom(memberType);
        }

        private static void LogMediatorInjectionSuccess(object mediator, MemberInfo member, Type memberType, string message)
        {
            // FlowConsoleLogger.Log(ConsoleLogType.Injection, "► INJECTION SUCCESS! " + message + "\n" +
            //                                          "► Mediator: " + mediator.GetType().Name + "\n" +
            //                                          "► Member: " + member.Name + "\n" +
            //                                          "► Type: " + memberType?.Name);
        }

        private static void LogMediatorInjectionError(object mediator, MemberInfo member, Type memberType, string message)
        {
            FlowLogger.LogError(SystemLogType.Injection,
                "<b><color=#FF6666>► INJECTION FAILED!</color></b> " + message + "\n" +
                "<b><color=#FF6666>► Mediator:</color><color=#FFEFD5> " + mediator.GetType().Name + "</color></b>\n" +
                "<b><color=#FF6666>► Member:</color><color=#FFEFD5> " + member.Name + "</color></b>\n" +
                "<b><color=#FF6666>► Type:</color><color=#FFEFD5> " + memberType?.Name + "</color></b>\n" +
                "<b><color=#FF6666>► Solution:</color><color=#FFEFD5> Use [InjectSignal] only for Signal types in Mediators</color></b>"
                );
        }

        private static void LogMediatorViewInjection(object mediator, MemberInfo member, Type memberType)
        {
            // FlowConsoleLogger.Log(ConsoleLogType.Injection, "► VIEW INJECTION SUCCESS!\n" +
            //                                          "► Mediator: " + mediator.GetType().Name + "\n" +
            //                                          "► View: " + member.Name + "\n" +
            //                                          "► Type: " + memberType?.Name);
        }

        #endregion

        private static void SetInjectedValue(object objectInstance, IContext context, MemberInfo injectedMemberInfo)
        {
            Type memberType = GetMemberType(injectedMemberInfo);
            object injectionValue = context.GetInjectedObject(injectedMemberInfo);

            if (injectionValue == null)
            {
                LogGeneralInjectionError(objectInstance, injectedMemberInfo, memberType, "Injection value is null!");
                return;
            }

            if (injectedMemberInfo.MemberType == MemberTypes.Field)
                (injectedMemberInfo as FieldInfo)?.SetValue(objectInstance, injectionValue);
            else if (injectedMemberInfo.MemberType == MemberTypes.Property)
                (injectedMemberInfo as PropertyInfo)?.SetValue(objectInstance, injectionValue);
        }

        private static object GetInjectedObject(this IContext context, MemberInfo memberInfo)
        {
            Type injectionType = null;
            if (memberInfo.MemberType == MemberTypes.Field)
                injectionType = (memberInfo as FieldInfo)?.FieldType;
            else if (memberInfo.MemberType == MemberTypes.Property)
                injectionType = (memberInfo as PropertyInfo)?.PropertyType;

            var injectSignalAttr = memberInfo.GetCustomAttribute<InjectSignalAttribute>();
            var injectAttr = memberInfo.GetCustomAttribute<InjectAttribute>();

            if (injectSignalAttr != null)
                return GetInjectedSignal(context, injectionType, injectSignalAttr.Name);

            if (injectAttr != null)
                return GetInjectedInstance(context, injectionType, injectAttr.Name);

            return null;
        }

        private static object GetInjectedSignal(IContext context, Type signalType, string name)
        {
            InjectionBinderCrossContext crossContextInjectionBinder = context.InjectionBinderCrossContext;
            List<IContext> allContexts = context.AllContexts;

            foreach (IContext cntxt in allContexts)
            {
                var signal = cntxt.InjectionBinder.GetInstance(signalType, name);
                if (signal != null)
                    return signal;
            }

            return crossContextInjectionBinder.GetInstance(signalType, name);
        }

        private static object GetInjectedInstance(IContext context, Type instanceType, string name)
        {
            InjectionBinderCrossContext crossContextInjectionBinder = context.InjectionBinderCrossContext;
            List<IContext> allContexts = context.AllContexts;

            foreach (IContext cntxt in allContexts)
            {
                var instance = cntxt.InjectionBinder.GetInstance(instanceType, name);
                if (instance != null)
                    return instance;
            }

            return crossContextInjectionBinder.GetInstance(instanceType, name);
        }

        private static List<Type> GetAllChildClasses(this Type type)
        {
            InjectionCacheData cashData = InjectionCaching.GetCacheData(type);

            if (cashData != null)
                return cashData.Children;

            List<Type> childTypes = Assembly
                .GetAssembly(type)
                .GetTypes()
                .Where(x => x.IsAssignableFrom(type) && !x.IsInterface)
                .ToList();

            InjectionCaching.AddCacheData(type, childTypes);

            return childTypes;
        }

        private static void LogGeneralInjectionError(object instance, MemberInfo member, Type memberType, string message)
        {
            FlowLogger.LogError(SystemLogType.Injection,
                "<b><color=#FF6666>► INJECTION FAILED!</color></b> " + message + "\n" +
                "<b><color=#FF6666>► Instance Type:</color><color=#FFEFD5> " + instance.GetType().Name + "</color></b>\n" +
                "<b><color=#FF6666>► Member:</color><color=#FFEFD5> " + member.Name + "</color></b>\n" +
                "<b><color=#FF6666>► Type:</color><color=#FFEFD5> " + memberType?.Name + "</color></b>"
                );
        }

        private static void LogGeneralInjectionSuccess(object instance, MemberInfo member, Type memberType, string message)
        {
            // FlowConsoleLogger.Log(ConsoleLogType.Injection, 
            //     "► INJECTION SUCCESS! " + message + "\n" +
            //     "► Instance Type: " + instance.GetType().Name + "\n" +
            //     "► Member: " + member.Name + "\n" +
            //     "► Type: " + memberType?.Name
            //     );
        }
    }
}