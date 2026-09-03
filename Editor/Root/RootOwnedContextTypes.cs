#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using FlowIoC.BaseModule.Root;
using FlowIoC.Editor.CodeGenerator;

namespace FlowIoC.Editor.Root
{
    /// <summary>
    /// The contexts that already belong to a Root. Every Root names the context it builds as the
    /// argument of Root&lt;T&gt;, so the whole set is readable without anything having to be
    /// marked by hand: BaseScreenRoot and BaseScreenTestRoot both derive from Root&lt;T&gt;, so
    /// one walk up the base chain catches every Root in the project.
    ///
    /// Add Sub Context uses this to keep such a context out of its list. Adding one to a second
    /// Root would build a second instance of it and run the same bindings twice.
    /// </summary>
    internal class RootOwnedContextTypes
    {
        public HashSet<Type> Collect()
        {
            var owned = new HashSet<Type>();

            foreach (var assembly in AssemblyHelper.GetProjectAssemblies())
            {
                Type[] types;

                try
                {
                    types = assembly.GetTypes();
                }
                catch (Exception)
                {
                    continue;
                }

                foreach (var type in types)
                {
                    if (!typeof(RootBase).IsAssignableFrom(type))
                        continue;

                    Type contextType = ContextArgumentOf(type);

                    if (contextType != null)
                        owned.Add(contextType);
                }
            }

            return owned;
        }

        /// <summary>
        /// The context a Root builds, or null when the type is not a Root or is one whose
        /// argument is still an open type parameter - Root&lt;T&gt; itself and BaseScreenRoot
        /// answer nothing, and their concrete subclasses answer the context they name.
        /// </summary>
        private Type ContextArgumentOf(Type rootType)
        {
            for (Type current = rootType; current != null; current = current.BaseType)
            {
                if (!current.IsGenericType || current.GetGenericTypeDefinition() != typeof(Root<>))
                    continue;

                Type argument = current.GetGenericArguments()[0];

                return argument.IsGenericParameter ? null : argument;
            }

            return null;
        }
    }
}
#endif
