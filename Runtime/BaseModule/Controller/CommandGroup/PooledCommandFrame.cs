using System;
using System.Collections.Generic;
using System.Reflection;

namespace FlowIoC.BaseModule.Controller.CommandGroup
{
    /// <summary>
    /// Reads Unity's stack text for the Command whose code threw from outside a running step - a
    /// lambda of a Command, or a closure class nested in one, with no group resolver below it. A
    /// throw inside Execute has the resolver on the stack and is somebody else's story. Types are
    /// looked up by name once, when the first exception arrives, and remembered.
    /// </summary>
    internal class PooledCommandFrame
    {
        private const string ResolverFrame = "FlowIoC.BaseModule.Controller.CommandGroup.CommandGroupResolver";

        private Dictionary<string, Type> _commandsByName;

        internal Type Find(string stackTrace)
        {
            if (string.IsNullOrEmpty(stackTrace) || stackTrace.Contains(ResolverFrame))
                return null;

            string[] lines = stackTrace.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                Type type = CommandOf(lines[i]);
                if (type != null)
                    return type;
            }

            return null;
        }

        /// <summary>
        /// "Ns.Outer+Command+&lt;&gt;c__DisplayClass1_0.&lt;Execute&gt;b__0 () (at ...)": the member is cut
        /// off, and each '+' prefix of the type is tried, so a closure class nested in a Command
        /// answers with the Command.
        /// </summary>
        private Type CommandOf(string line)
        {
            int call = line.IndexOf(" (", StringComparison.Ordinal);
            if (call <= 0) return null;

            string member = line.Substring(0, call);
            int lastDot = member.LastIndexOf('.');
            if (lastDot <= 0) return null;

            string[] parts = member.Substring(0, lastDot).Split('+');
            string name = parts[0];
            Type found = Lookup(name);

            for (int p = 1; p < parts.Length; p++)
            {
                name += "+" + parts[p];
                found = Lookup(name) ?? found;
            }

            return found;
        }

        private Type Lookup(string fullName)
        {
            _commandsByName ??= BuildIndex();
            return _commandsByName.TryGetValue(fullName, out Type type) ? type : null;
        }

        private static Dictionary<string, Type> BuildIndex()
        {
            var index = new Dictionary<string, Type>();

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try { types = assembly.GetTypes(); }
                catch (ReflectionTypeLoadException exception) { types = exception.Types; }

                foreach (Type type in types)
                {
                    if (type != null && !type.IsAbstract && typeof(CommandBody).IsAssignableFrom(type) && type.FullName != null)
                        index[type.FullName] = type;
                }
            }

            return index;
        }
    }
}
