#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Linq;
using FlowIoC.BaseModule.Root.Utils;
using UnityEditor;
using Object = UnityEngine.Object;

namespace FlowIoC.Editor.Root
{
    /// <summary>
    /// The script asset a context type is declared in, which is what a Root's sub-context entry
    /// stores. Both directions live here: a type to its script when an entry is written, and a
    /// script back to its type when one is read.
    ///
    /// The candidates come from a name search, so they are filtered on the class the script
    /// actually compiles to. Two modules may each hold a SettingsScreenContext, and taking the
    /// first match would wire the Root to the wrong module's context with nothing downstream able
    /// to tell. That is the same filter MonoScriptText applies for the same reason.
    ///
    /// Both sides are delegates so a test needs no editor and no asset on disk.
    /// </summary>
    internal class ContextScriptResolver
    {
        private readonly Func<string, IReadOnlyList<Object>> _candidates;
        private readonly Func<Object, Type> _classOf;
        private readonly Func<IReadOnlyList<Type>> _contextTypes;

        internal ContextScriptResolver() : this(
            typeName => AssetDatabase.FindAssets($"t:MonoScript {typeName}")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<MonoScript>)
                .Where(script => script != null)
                .Cast<Object>()
                .ToList(),
            script => (script as MonoScript)?.GetClass(),
            AssemblyExtensions.GetAllContextTypes)
        {
        }

        internal ContextScriptResolver(
            Func<string, IReadOnlyList<Object>> candidates,
            Func<Object, Type> classOf,
            Func<IReadOnlyList<Type>> contextTypes)
        {
            _candidates = candidates;
            _classOf = classOf;
            _contextTypes = contextTypes;
        }

        /// <summary>
        /// The script that declares this type, or null when there is none. A type from a
        /// precompiled assembly has no script asset, and neither does a class whose file is named
        /// something else - MonoScript.GetClass answers null there. Both are ordinary, and the
        /// caller keeps whatever name the entry already had.
        /// </summary>
        internal Object For(Type contextType)
        {
            if (contextType == null) return null;

            foreach (Object candidate in _candidates(contextType.Name))
            {
                if (_classOf(candidate) == contextType) return candidate;
            }

            return null;
        }

        /// <summary>
        /// The script that declares the context of this full name, or null when nothing compiles
        /// to it. It goes through the type rather than straight to a file of that name, so the
        /// answer is the class the project actually built - and a name left behind by a deleted
        /// module answers null, which is the entry the Root inspector reports.
        ///
        /// This is what the inspector's Resolve button and the generators use, both of which know
        /// the name and not the type.
        /// </summary>
        internal Object ForName(string contextFullName)
        {
            if (string.IsNullOrEmpty(contextFullName)) return null;

            return For(_contextTypes().FirstOrDefault(type => type.FullName == contextFullName));
        }

        /// <summary>The class a script compiles to, or null when it declares none this can read.</summary>
        internal Type TypeOf(Object script) => script == null ? null : _classOf(script);
    }
}

#endif