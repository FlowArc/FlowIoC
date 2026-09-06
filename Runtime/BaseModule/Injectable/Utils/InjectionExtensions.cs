using System;
using System.Collections.Generic;
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
        private const BindingFlags DECLARED_MEMBERS =
            BindingFlags.DeclaredOnly | BindingFlags.Instance |
            BindingFlags.Public | BindingFlags.NonPublic;

        private static Dictionary<Type, CachedInjectableData> _cachedInjectableData = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => _cachedInjectableData = new();

        private class CachedInjectableData
        {
            public List<InjectEntry> InjectEntries;
            public List<SignalParamEntry> SignalParamEntries;
        }

        /// <summary>
        /// One injectable property, with everything the resolver needs already worked out. The
        /// attribute used to be read back off the property on every injection, which meant a
        /// reflection lookup and a fresh attribute object per property per command execution -
        /// for an answer that cannot change after the type is compiled.
        /// </summary>
        private class InjectEntry
        {
            public PropertyInfo Property;
            public Type Type;
            public string Name;
            public bool IsSignal;
        }

        #region Entry Points

        internal static bool TryToInjectObject(this InjectionBinding binding)
        {
            InjectMembers(binding.Value, binding.BindedContext);
            return true;
        }

        internal static bool TryToInjectObject(this IContext context, object injectedObject)
        {
            InjectMembers(injectedObject, context);
            return true;
        }

        internal static void TryToInjectFunction(this IContext context, IFunctionBody functionBody)
        {
            InjectMembers(functionBody, context);
        }

        /// <summary>
        /// Fills a command and hands it the signal's payload. The members are resolved once and
        /// remembered against the run's binding generation: a pooled command that already holds
        /// what it asked for is left alone, and re-resolved only when a binder gained or lost a
        /// binding. The payload is a different matter and is read on every execution.
        /// </summary>
        internal static void InjectCommand(this IContext context, ICommandBody command, params object[] signalParams)
        {
            if (command is CommandBody body)
            {
                int generation = context?.InjectionBinderCrossContext?.BindingGeneration ?? 0;

                if (body.InjectedContext != context || body.InjectionStamp != generation)
                {
                    InjectMembers(command, context);
                    body.InjectedContext = context;
                    body.InjectionStamp = generation;
                }
            }
            else
            {
                InjectMembers(command, context);
            }

            InjectSignalParamsToCommand(context, command, signalParams);
        }

        internal static bool TryToInjectMediator(this IContext context, IMediator mediator, IView view)
        {
            InjectMediatorMembers(mediator, view, context);

            mediator.OnRegister();
            return true;
        }

        #endregion

        #region Member Injection

        private static void InjectMembers(object target, IContext context)
        {
            if (target == null)
                return;

            List<InjectEntry> entries = GetInjectEntries(target.GetType());

            for (int i = 0; i < entries.Count; i++)
            {
                InjectEntry entry = entries[i];
                object value = Resolve(context, entry.Type, entry.Name);

                if (value == null)
                {
                    LogInjectionFailed(target, entry, "Injection value is null!");
                    continue;
                }

                entry.Property.SetValue(target, value);
            }
        }

        /// <summary>
        /// A Mediator reads differently from everything else: its [Inject] names the View it
        /// drives rather than something in a container, and only its [InjectSignal] is resolved.
        /// </summary>
        private static void InjectMediatorMembers(object mediator, object view, IContext context)
        {
            Type viewType = view.GetType();
            List<InjectEntry> entries = GetInjectEntries(mediator.GetType());

            for (int i = 0; i < entries.Count; i++)
            {
                InjectEntry entry = entries[i];

                if (entry.IsSignal)
                {
                    object signal = Resolve(context, entry.Type, entry.Name);
                    if (signal == null)
                    {
                        LogInjectionFailed(mediator, entry, "Signal instance not found in context!");
                        continue;
                    }

                    entry.Property.SetValue(mediator, signal);
                    continue;
                }

                if (entry.Type == viewType || viewType.IsSubclassOf(entry.Type) ||
                    (entry.Type.IsInterface && entry.Type.IsAssignableFrom(viewType)))
                {
                    entry.Property.SetValue(mediator, view);
                    continue;
                }

                LogInjectionFailed(mediator, entry,
                    "[Inject] can only be used for View injection in Mediators! Current type is " + entry.Type.Name);
            }
        }

        /// <summary>
        /// The instance bound to a type, asked of this context and its sub-contexts first and of
        /// the cross-context binder last. Signals and everything else resolve the same way; they
        /// only differ in which attribute asked for them.
        /// </summary>
        private static object Resolve(IContext context, Type type, string name)
        {
            if (context == null)
                return null;

            List<IContext> allContexts = context.AllContexts;

            for (int i = 0; i < allContexts.Count; i++)
            {
                object instance = allContexts[i].InjectionBinder.GetInstance(type, name);
                if (instance != null)
                    return instance;
            }

            InjectionBinderCrossContext crossContext = context.InjectionBinderCrossContext;
            return crossContext?.GetInstance(type, name);
        }

        #endregion

        #region Entry Building

        /// <summary>
        /// The injectable properties of a type, most-base class first. Built by walking the
        /// declaration chain rather than the assembly's types, so a base class in another assembly
        /// is read like any other and a public property is not collected twice - once as the
        /// derived type's inherited member and once as the base type's own.
        /// </summary>
        private static List<InjectEntry> GetInjectEntries(Type targetType)
        {
            CachedInjectableData data = GetCachedData(targetType);
            return data.InjectEntries ??= BuildInjectEntries(targetType);
        }

        private static List<InjectEntry> BuildInjectEntries(Type targetType)
        {
            List<InjectEntry> entries = new List<InjectEntry>();
            if (targetType == null)
                return entries;

            List<Type> chain = new List<Type>();
            for (Type type = targetType; type != null && type != typeof(object); type = type.BaseType)
                chain.Add(type);
            chain.Reverse();

            HashSet<MethodInfo> seenAccessors = new HashSet<MethodInfo>();

            for (int c = 0; c < chain.Count; c++)
            {
                PropertyInfo[] properties = chain[c].GetProperties(DECLARED_MEMBERS);
                Array.Sort(properties, CompareByMetadataToken);

                for (int p = 0; p < properties.Length; p++)
                {
                    PropertyInfo property = properties[p];

                    InjectSignalAttribute signalAttribute = property.GetCustomAttribute<InjectSignalAttribute>(false);
                    InjectAttribute injectAttribute = signalAttribute == null
                        ? property.GetCustomAttribute<InjectAttribute>(false)
                        : null;

                    if (signalAttribute == null && injectAttribute == null)
                        continue;

                    // An override re-declares a property the base class already gave us.
                    MethodInfo accessor = property.SetMethod ?? property.GetMethod;
                    MethodInfo declaration = accessor?.GetBaseDefinition() ?? accessor;
                    if (declaration != null && !seenAccessors.Add(declaration))
                        continue;

                    bool isSignalType = typeof(ISignalHolder).IsAssignableFrom(property.PropertyType);

                    // Reported here rather than at every injection: the attribute a property wears
                    // cannot change once it compiles, so the complaint is worth making once.
                    if (signalAttribute != null && !isSignalType)
                    {
                        LogAttributeMisuse(targetType, property,
                            "[InjectSignal] can only be used with Signal types!");
                        continue;
                    }

                    if (injectAttribute != null && isSignalType)
                    {
                        LogAttributeMisuse(targetType, property,
                            "Signals must use [InjectSignal]!");
                        continue;
                    }

                    entries.Add(new InjectEntry
                    {
                        Property = property,
                        Type = property.PropertyType,
                        Name = signalAttribute != null ? signalAttribute.Name : injectAttribute.Name,
                        IsSignal = signalAttribute != null
                    });
                }
            }

            return entries;
        }

        private static int CompareByMetadataToken(PropertyInfo left, PropertyInfo right)
            => left.MetadataToken.CompareTo(right.MetadataToken);

        private static CachedInjectableData GetCachedData(Type type)
        {
            if (_cachedInjectableData.TryGetValue(type, out CachedInjectableData data))
                return data;

            data = new CachedInjectableData();
            _cachedInjectableData.Add(type, data);
            return data;
        }

        #endregion

        #region Signal Params

        private static void InjectSignalParamsToCommand(IContext context, ICommandBody command, params object[] signalParams)
        {
            if (signalParams == null || signalParams.Length == 0)
                return;

            List<SignalParamEntry> entries = GetSignalParamEntries(command.GetType());
            if (entries.Count == 0)
                return;

            // A null context has no resolver to share, and a resolver already running is
            // one this call must not disturb - a [SignalParam] setter that dispatches
            // lands here while the outer Resolve is mid-flight.
            SignalParamResolver resolver = context?.SignalParamResolver;
            if (resolver == null || resolver.IsResolving)
                resolver = new SignalParamResolver();
            resolver.Resolve(command, entries, signalParams);

            IReadOnlyList<SignalParamDiagnostic> diagnostics = resolver.Diagnostics;
            if (diagnostics.Count == 0)
                return;

            // Copy before logging: a log sink that dispatches would re-enter the resolver
            // and clear this list out from under the loop.
            SignalParamDiagnostic[] snapshot = new SignalParamDiagnostic[diagnostics.Count];
            for (int i = 0; i < snapshot.Length; i++)
                snapshot[i] = diagnostics[i];

            for (int i = 0; i < snapshot.Length; i++)
                LogSignalParamDiagnostic(snapshot[i]);
        }

        private static List<SignalParamEntry> GetSignalParamEntries(Type commandType)
        {
            CachedInjectableData data = GetCachedData(commandType);
            return data.SignalParamEntries ??= new SignalParamEntryBuilder().Build(commandType);
        }

        private static void LogSignalParamDiagnostic(SignalParamDiagnostic diagnostic)
        {
            string reason = diagnostic.Kind switch
            {
                SignalParamDiagnosticKind.IndexOutOfRange when diagnostic.RequestedIndex < 0 =>
                    $"[SignalParam({diagnostic.RequestedIndex})] has a negative index. Indices count from zero.",
                SignalParamDiagnosticKind.IndexOutOfRange =>
                    $"[SignalParam({diagnostic.RequestedIndex})] needs at least {diagnostic.RequestedIndex + 1} {diagnostic.PropertyType.Name} values in the payload because the index counts from zero, but the signal carried {diagnostic.CandidateCount}.",
                SignalParamDiagnosticKind.DuplicateClaim =>
                    $"[SignalParam({diagnostic.RequestedIndex})] asks for a {diagnostic.PropertyType.Name} value that '{diagnostic.ClaimingPropertyName}' already took. Give the two properties different indices.",
                SignalParamDiagnosticKind.NoFreeSlot =>
                    $"No unclaimed {diagnostic.PropertyType.Name} value is left. The signal carried {diagnostic.CandidateCount} and {diagnostic.ClaimedCount} were already taken.",
                _ =>
                    $"The {diagnostic.PropertyType.Name} value for this property could not be bound."
            };

            FlowLogger.LogError(SystemLogType.CommandOperation,
                "<b><color=#FF6666>► Signal Param could not be bound!</color></b>\n" +
                "<b><color=#FF6666>► Command:</color><color=#FFEFD5> " + diagnostic.TargetType.Name + "</color></b>\n" +
                "<b><color=#FF6666>► Property:</color><color=#FFEFD5> " + diagnostic.PropertyName + "</color></b>\n" +
                "<b><color=#FF6666>► Type:</color><color=#FFEFD5> " + diagnostic.PropertyType.Name + "</color></b>\n" +
                "<b><color=#FF6666>► Reason:</color><color=#FFEFD5> " + reason + "</color></b>",
                "► Signal Param could not be bound!\n" +
                "► Command: " + diagnostic.TargetType.Name + "\n" +
                "► Property: " + diagnostic.PropertyName + "\n" +
                "► Type: " + diagnostic.PropertyType.Name + "\n" +
                "► Reason: " + reason);
        }

        #endregion

        #region Reporting

        private static void LogInjectionFailed(object target, InjectEntry entry, string message)
        {
            FlowLogger.LogError(SystemLogType.Injection,
                "<b><color=#FF6666>► INJECTION FAILED!</color></b> " + message + "\n" +
                "<b><color=#FF6666>► Instance Type:</color><color=#FFEFD5> " + target.GetType().Name + "</color></b>\n" +
                "<b><color=#FF6666>► Member:</color><color=#FFEFD5> " + entry.Property.Name + "</color></b>\n" +
                "<b><color=#FF6666>► Type:</color><color=#FFEFD5> " + entry.Type.Name + "</color></b>");
        }

        private static void LogAttributeMisuse(Type targetType, PropertyInfo property, string message)
        {
            FlowLogger.LogError(SystemLogType.Injection,
                "<b><color=#FF6666>► INJECTION FAILED!</color></b> " + message + "\n" +
                "<b><color=#FF6666>► Instance Type:</color><color=#FFEFD5> " + targetType.Name + "</color></b>\n" +
                "<b><color=#FF6666>► Member:</color><color=#FFEFD5> " + property.Name + "</color></b>\n" +
                "<b><color=#FF6666>► Type:</color><color=#FFEFD5> " + property.PropertyType.Name + "</color></b>");
        }

        #endregion
    }
}