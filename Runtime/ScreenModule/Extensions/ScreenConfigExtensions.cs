using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FlowIoC.BaseModule.Bind.Bindings.Mediator;
using FlowIoC.BaseModule.Contexts;
using FlowIoC.BaseModule.Injectable.Binders;
using FlowIoC.BaseModule.ViewsMediators.Mediator;
using FlowIoC.BaseModule.ViewsMediators.View;
using FlowIoC.ConsoleModule;
using FlowIoC.ScreenModule.Data;
using UnityEngine;

namespace FlowIoC.ScreenModule.Extensions
{
    internal static class ScreenConfigExtensions
    {
        private static Dictionary<string, Type> _typeCache = new Dictionary<string, Type>();

        private static readonly MethodInfo _genericBindMethod = typeof(MediationBinder)
            .GetMethods()
            .First(m => m.Name == "Bind" && m.IsGenericMethod && m.GetParameters().Length == 0);

        private static readonly MethodInfo _genericToMethod = typeof(MediatorBinding)
            .GetMethods()
            .First(m => m.Name == "To" && m.IsGenericMethod && m.GetParameters().Length == 0);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => _typeCache = new Dictionary<string, Type>();

        public static void RegisterViewMediators(this IEnumerable<CD_Screen> screenConfigs, IContext context)
        {
            if (context == null || context.MediationBinder == null)
            {
                FlowLogger.LogError(SystemLogType.Screen,"Context or MediationBinder is null");
                return;
            }

            MediationBinder mediationBinder = context.MediationBinder;

            //MapViewsAndMediators(screenConfigs);

            foreach (var config in screenConfigs)
            {
                if (string.IsNullOrEmpty(config.ViewTypeName) ||
                    string.IsNullOrEmpty(config.MediatorTypeName))
                {
                    FlowLogger.LogWarning(SystemLogType.Screen,$"ViewTypeName or MediatorTypeName is null at config file.");
                    continue;
                }
                
                _typeCache.TryGetValue(config.ViewTypeName, out Type viewType);
                _typeCache.TryGetValue(config.MediatorTypeName, out Type mediatorType);

                if (viewType == null || mediatorType == null)
                {
                    FlowLogger.LogWarning(SystemLogType.Screen,$"Could not find type for View '{config.ViewTypeName}' or Mediator '{config.MediatorTypeName}'");
                    continue;
                }

                if (!typeof(IView).IsAssignableFrom(viewType))
                {
                    FlowLogger.LogError(SystemLogType.Screen,$"Type {viewType.Name} must implement IView interface");
                    continue;
                }

                if (!typeof(IMediator).IsAssignableFrom(mediatorType))
                {
                    FlowLogger.LogError(SystemLogType.Screen,$"Type {mediatorType.Name} must implement IMediator interface");
                    continue;
                }

                MethodInfo bindMethodGeneric = _genericBindMethod.MakeGenericMethod(viewType);
                object binding = bindMethodGeneric.Invoke(mediationBinder, null);

                if (binding != null)
                {
                    MethodInfo toMethodGeneric = _genericToMethod.MakeGenericMethod(mediatorType);
                    toMethodGeneric.Invoke(binding, null);

                    FlowLogger.Log(SystemLogType.Screen, $"Successfully bound {viewType.Name} to {mediatorType.Name}");
                }
            }
        }

        public static void MapViewsAndMediators(this IEnumerable<CD_Screen> screenConfigs)
        {
            List<string> allTypeNames = screenConfigs
                .SelectMany(config => new[] {config.ViewTypeName, config.MediatorTypeName})
                .Where(name => !string.IsNullOrEmpty(name))
                .Distinct()
                .ToList();


            List<string> uncachedTypeNames = allTypeNames
                .Where(name => !_typeCache.ContainsKey(name))
                .ToList();
            
            if (uncachedTypeNames.Count > 0)
            {
                PopulateCache(uncachedTypeNames);
            }
        }
        
        public static Type ConvertStringToType(this CD_Screen screenConfig)
        {
            return _typeCache.GetValueOrDefault(screenConfig.ViewTypeName);
        }

        public static void RegisterViewMediators(this CD_Screen screenConfig, IContext context)
        {
            RegisterViewMediators(new[] {screenConfig}, context);
        }

        /// <summary>
        /// Populates the static _typeCache by searching for the given type names
        /// across all assemblies in the current AppDomain.
        /// </summary>
        /// <param name="typeNames">List of type names we need to resolve and cache.</param>
        private static void PopulateCache(List<string> typeNames)
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            HashSet<string> remaining = new HashSet<string>(typeNames);

            foreach (Assembly assembly in assemblies)
            {
                try
                {
                    Type[] typesInAssembly = assembly.GetTypes();

                    foreach (Type t in typesInAssembly)
                    {
                        if (remaining.Count == 0)
                            break;

                        if (remaining.Contains(t.Name))
                        {
                            _typeCache[t.Name] = t;
                            remaining.Remove(t.Name);
                        }

                        if (!string.IsNullOrEmpty(t.FullName) && remaining.Contains(t.FullName))
                        {
                            _typeCache[t.FullName] = t;
                            remaining.Remove(t.FullName);
                        }
                    }
                }
                catch (ReflectionTypeLoadException)
                {
                    // You may optionally log or handle partial loads here
                    continue;
                }

                if (remaining.Count == 0)
                    break;
            }
        }
    }
}