#if UNITY_EDITOR
using System;
using System.Collections.Generic;

namespace FlowIoC.Editor.CodeGenerator
{
    /// <summary>
    /// Where a type name lives, for the generators that write an injected member from a name typed
    /// into a window.
    ///
    /// It is an index rather than a search because the search it replaces walked every loaded
    /// assembly's types on every question, and the Create Function window asks one per row on every
    /// repaint. Built once and answered from a dictionary after that, including the misses - a name
    /// half typed is a miss that will be asked again on the very next keystroke.
    ///
    /// A domain reload throws the instance away with everything else, which is exactly when the
    /// answers could have changed.
    /// </summary>
    internal class InjectableTypeIndex
    {
        private Dictionary<string, string> _namespaces;

        /// <summary>
        /// The namespace the type is declared in, or null when nothing in the project has that
        /// name. A type in the global namespace answers with an empty string rather than null, so
        /// "found, and needs no using" is not confused with "not found".
        /// </summary>
        internal string NamespaceFor(string typeName)
        {
            if (string.IsNullOrWhiteSpace(typeName)) return null;

            Build();

            return _namespaces.TryGetValue(typeName.Trim(), out string found) ? found : null;
        }

        internal bool Knows(string typeName) => NamespaceFor(typeName) != null;

        /// <summary>
        /// The first type of a given name wins, which is what the search this replaces did. A name
        /// that two assemblies both declare is ambiguous however it is resolved, and the author
        /// writing it into a window is the one who has to disambiguate it.
        /// </summary>
        private void Build()
        {
            if (_namespaces != null) return;

            _namespaces = new Dictionary<string, string>();

            foreach (System.Reflection.Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;

                // An assembly whose dependencies are not all loaded throws rather than answering,
                // and one such assembly must not cost the index every other one.
                try
                {
                    types = assembly.GetTypes();
                }
                catch (Exception)
                {
                    continue;
                }

                foreach (Type type in types)
                {
                    if (_namespaces.ContainsKey(type.Name)) continue;

                    _namespaces[type.Name] = type.Namespace ?? string.Empty;
                }
            }
        }
    }
}
#endif
