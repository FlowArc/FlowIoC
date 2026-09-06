#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FlowIoC.BaseModule.Signals;
using FlowIoC.Editor.Modules;
using FlowIoC.Editor.ModuleScanner;

namespace FlowIoC.Editor.ModuleCards
{
    /// <summary>
    /// What a module's card can say about itself, read from the assemblies the Editor has
    /// already loaded rather than from its source. Reflection is used because the alternative -
    /// a regular expression over C# - is wrong the first time somebody writes a signal on two
    /// lines, and because the three assembly names are already derivable from the module's own.
    ///
    /// A module whose assembly is not loaded collects nothing at all, and the caller leaves the
    /// existing block alone. A card that says a module has no signals because the project did
    /// not compile is worse than one that is a day out of date.
    /// </summary>
    internal class ModuleFactsCollector
    {
        private const string SHARED_SUFFIX = ".Shared";
        private const string SIGNALS_SUFFIX = ".Signals";
        private const string SERVICES_NAMESPACE = ".Services";
        private const string SYSTEMS_NAMESPACE = ".Systems";

        private static readonly Dictionary<Type, string> KEYWORDS = new Dictionary<Type, string>
        {
            {typeof(bool), "bool"},
            {typeof(byte), "byte"},
            {typeof(char), "char"},
            {typeof(decimal), "decimal"},
            {typeof(double), "double"},
            {typeof(float), "float"},
            {typeof(int), "int"},
            {typeof(long), "long"},
            {typeof(object), "object"},
            {typeof(short), "short"},
            {typeof(string), "string"},
            {typeof(uint), "uint"},
            {typeof(ulong), "ulong"},
        };

        private readonly Func<string, Assembly> _assemblyByName;

        internal ModuleFactsCollector() : this(DefaultAssemblyByName)
        {
        }

        internal ModuleFactsCollector(Func<string, Assembly> assemblyByName)
        {
            _assemblyByName = assemblyByName;
        }

        internal ModuleFactsEVO Collect(ModuleTargetEVO module, IReadOnlyList<ScannedModule> children)
        {
            Assembly own = _assemblyByName(module.ExpectedAssemblyName);
            if (own == null) return null;

            Assembly shared = _assemblyByName(module.ExpectedAssemblyName + SHARED_SUFFIX);
            Assembly signals = _assemblyByName(module.ExpectedAssemblyName + SIGNALS_SUFFIX);

            var assemblies = new List<string> {module.ExpectedAssemblyName};
            if (shared != null) assemblies.Add(module.ExpectedAssemblyName + SHARED_SUFFIX);
            if (signals != null) assemblies.Add(module.ExpectedAssemblyName + SIGNALS_SUFFIX);

            Type holder = HolderTypeIn(signals);

            return new ModuleFactsEVO
            {
                Kind = module.Kind.ToString(),
                Assemblies = assemblies,
                RootType = NameOfFirst(own, IsRoot),
                ContextType = NameOfFirst(own, IsContext),
                Incoming = SignalsOn(holder, "Incoming"),
                Outgoing = SignalsOn(holder, "Outgoing"),
                Publishes = Sorted(PublicTypeNames(shared)),
                Services = Sorted(InterfaceNames(own, SERVICES_NAMESPACE)),
                Systems = Sorted(InterfaceNames(own, SYSTEMS_NAMESPACE)),
                SubModules = ChildNames(children),
            };
        }

        private static Assembly DefaultAssemblyByName(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;

            return AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(assembly => assembly.GetName().Name == name);
        }

        private Type HolderTypeIn(Assembly assembly)
        {
            if (assembly == null) return null;

            return SafeTypes(assembly)
                .Where(type => type.IsClass && !type.IsAbstract && typeof(ISignalHolder).IsAssignableFrom(type))
                .OrderBy(type => type.Name, StringComparer.Ordinal)
                .FirstOrDefault();
        }

        /// <summary>
        /// The holder's Incoming and Outgoing are plain public fields on a plain class, so the
        /// signals are the public fields of that field's type. A holder that has neither half -
        /// which is what an internal holder looks like - yields nothing.
        /// </summary>
        private IReadOnlyList<string> SignalsOn(Type holder, string halfName)
        {
            var names = new List<string>();

            FieldInfo half = holder?.GetField(halfName, BindingFlags.Public | BindingFlags.Instance);
            if (half == null) return names;

            foreach (FieldInfo field in half.FieldType.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!IsSignal(field.FieldType)) continue;

                names.Add(field.Name + "(" + string.Join(", ", ParameterNames(field.FieldType)) + ")");
            }

            names.Sort(StringComparer.Ordinal);
            return names;
        }

        private bool IsSignal(Type type)
        {
            return type.Name == "Signal" || type.Name.StartsWith("Signal`", StringComparison.Ordinal);
        }

        private IEnumerable<string> ParameterNames(Type signalType)
        {
            return signalType.IsGenericType
                ? signalType.GetGenericArguments().Select(Friendly)
                : Enumerable.Empty<string>();
        }

        private string Friendly(Type type)
        {
            if (KEYWORDS.TryGetValue(type, out string keyword)) return keyword;

            if (!type.IsGenericType) return type.Name;

            string bare = type.Name.Substring(0, type.Name.IndexOf('`'));
            return bare + "<" + string.Join(", ", type.GetGenericArguments().Select(Friendly)) + ">";
        }

        private string NameOfFirst(Assembly assembly, Func<Type, bool> predicate)
        {
            return SafeTypes(assembly)
                .Where(predicate)
                .OrderBy(type => type.Name, StringComparer.Ordinal)
                .Select(type => type.Name)
                .FirstOrDefault();
        }

        /// <summary>
        /// Root and Context are matched by walking the base chain by name rather than by
        /// referencing the runtime types, so this stays correct if either moves namespace.
        /// </summary>
        private bool IsRoot(Type type) => !type.IsAbstract && HasBaseNamed(type, "Root`1");

        private bool IsContext(Type type) => !type.IsAbstract && HasBaseNamed(type, "Context");

        private bool HasBaseNamed(Type type, string baseName)
        {
            for (Type walk = type.BaseType; walk != null; walk = walk.BaseType)
            {
                if (walk.Name == baseName) return true;
            }

            return false;
        }

        private IEnumerable<string> PublicTypeNames(Assembly assembly)
        {
            return SafeTypes(assembly)
                .Where(type => type.IsPublic && !type.IsNested)
                .Select(type => type.Name);
        }

        private IEnumerable<string> InterfaceNames(Assembly assembly, string namespaceSuffix)
        {
            return SafeTypes(assembly)
                .Where(type => type.IsInterface && type.IsPublic)
                .Where(type => type.Namespace != null
                               && type.Namespace.EndsWith(namespaceSuffix, StringComparison.Ordinal))
                .Select(type => type.Name);
        }

        private IReadOnlyList<string> ChildNames(IReadOnlyList<ScannedModule> children)
        {
            var names = new List<string>();

            if (children != null)
            {
                foreach (ScannedModule child in children)
                    names.Add(child.Name + " (" + child.Kind + ")");
            }

            names.Sort(StringComparer.Ordinal);
            return names;
        }

        private IReadOnlyList<string> Sorted(IEnumerable<string> names)
        {
            var list = new List<string>(names);
            list.Sort(StringComparer.Ordinal);
            return list;
        }

        /// <summary>
        /// A partially loadable assembly yields what it can. This happens while a project is
        /// mid-compile, and throwing there would take the whole scan down.
        /// </summary>
        private IEnumerable<Type> SafeTypes(Assembly assembly)
        {
            if (assembly == null) return Enumerable.Empty<Type>();

            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException exception)
            {
                return exception.Types.Where(type => type != null);
            }
        }
    }
}

#endif
