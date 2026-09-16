using System;
using System.Collections.Generic;
using System.Reflection;
using FlowIoC.BaseModule.Attributes;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Controller.Binders;
using FlowIoC.BaseModule.Function.Provider;
using FlowIoC.BaseModule.Injectable;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.Injectable.CrossContext;
using FlowIoC.BaseModule.Signals;
using Modules.DeviceDebuggerModule.Constants;
using Modules.DeviceDebuggerModule.Data.ValueObjects;
using Modules.DeviceDebuggerModule.Models;

namespace Modules.DeviceDebuggerModule.Controllers
{
    /// <summary>
    /// Runs on every Show. The holders are read fresh from the cross-context binder each time,
    /// because a scene that came and went may have bound or taken back its holder, and the
    /// persistent Root must not keep a scene module's. The assemblies are scanned for annotated
    /// steps once, and each step gets a signal of the debugger's own bound to it here, so a tap
    /// runs it through the ordinary pipeline like any other step.
    /// </summary>
    internal class DiscoverOptionsCommand : Command
    {
        [Inject] private IDeviceDebuggerModel _model { get; set; }
        [Inject] private IFunctionProvider _functionProvider { get; set; }
        [Inject] private InjectionBinderCrossContext _crossContext { get; set; }
        [Inject] private ICommandBinder _commandBinder { get; set; }

        public override void Execute()
        {
            if (!DeviceDebuggerConstants.IS_AVAILABLE) return;

            List<ISignalHolder> holders = Holders();

            if (_model.CommandOptionTypes == null)
                _model.SetCommandOptionTypes(AnnotatedCommandTypes());

            List<DebugOptionVO> options = _functionProvider.Call<ScanDebugOptionsFunction>()
                .AddParams(holders, StepsWhoseServiceIsBound(_model.CommandOptionTypes))
                .ExecuteAndGetResult<List<DebugOptionVO>>();

            foreach (DebugOptionVO option in options)
            {
                if (option.CommandType == null) continue;

                option.Trigger = _model.TriggerFor(option.Key, option.Label, out bool created);

                if (!created) continue;

                if (option.Argument != null)
                    _commandBinder.Bind(option.Trigger).ToSequence(option.CommandType, option.Argument);
                else
                    _commandBinder.Bind(option.Trigger).ToSequence(option.CommandType);
            }

            List<DebugSignalVO> signals = _functionProvider.Call<ScanSignalHoldersFunction>()
                .AddParams(holders)
                .ExecuteAndGetResult<List<DebugSignalVO>>();

            _model.SetOptions(options, signals);
        }

        /// <summary>
        /// A step nested in a Service interface runs only where that Service is bound: a Haptic
        /// step in a scene without HapticServiceRoot would inject nothing and throw on the tap. A
        /// step nested in no interface is kept as it is.
        /// </summary>
        private List<Type> StepsWhoseServiceIsBound(IReadOnlyList<Type> steps)
        {
            var bound = new HashSet<Type>();

            foreach (InjectionBinding binding in _crossContext.GetAllInjectionBindings())
            {
                if (binding.Key is Type key) bound.Add(key);
            }

            var kept = new List<Type>();

            foreach (Type step in steps)
            {
                Type service = ServiceOf(step);

                if (service == null || bound.Contains(service)) kept.Add(step);
            }

            return kept;
        }

        private static Type ServiceOf(Type step)
        {
            Type owner = step?.DeclaringType;

            while (owner != null && !owner.IsInterface) owner = owner.DeclaringType;

            return owner;
        }

        private List<ISignalHolder> Holders()
        {
            var holders = new List<ISignalHolder>();

            foreach (InjectionBinding binding in _crossContext.GetAllInjectionBindings())
            {
                if (binding.Value is ISignalHolder holder && !holders.Contains(holder))
                    holders.Add(holder);
            }

            return holders;
        }

        private static List<Type> AnnotatedCommandTypes()
        {
            var types = new List<Type>();

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (IsSkipped(assembly)) continue;

                foreach (Type type in TypesOf(assembly))
                {
                    if (type == null || type.IsAbstract || !typeof(CommandBody).IsAssignableFrom(type)) continue;
                    if (!type.IsDefined(typeof(DebugOptionAttribute), false)) continue;

                    types.Add(type);
                }
            }

            return types;
        }

        private static bool IsSkipped(Assembly assembly)
        {
            string name = assembly.GetName().Name ?? "";

            foreach (string prefix in DeviceDebuggerConstants.SKIPPED_ASSEMBLY_PREFIXES)
            {
                if (name.StartsWith(prefix, StringComparison.Ordinal)) return true;
            }

            return false;
        }

        private static Type[] TypesOf(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                return e.Types ?? Type.EmptyTypes;
            }
        }
    }
}
