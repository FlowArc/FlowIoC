#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using FlowIoC.BaseModule.Root;
using FlowIoC.Editor.Root;
using UnityEditor;
using Object = UnityEngine.Object;

namespace FlowIoC.Editor.CodeGenerator.Menus.Module.DeleteModule
{
    /// <summary>
    /// Which of a Root's sub-context entries belong to the module about to be deleted, and taking
    /// them out of the list.
    ///
    /// It has to be exact in both directions. Leaving one behind is a Root listing a context that no
    /// longer exists - the thing this whole feature is about - and taking one too many silently
    /// unwires a module nobody asked about, which is worse, because nothing reports it.
    ///
    /// The script reference is what places an entry: its asset sits inside the module folder or it
    /// does not. An entry authored before the reference is placed by the type its name resolves to,
    /// and one that resolves to nothing at all is left alone - it is already broken by some earlier
    /// deletion and there is no way to tell whose it was.
    /// </summary>
    internal class ModuleSubContextEntries
    {
        private readonly Func<Object, string> _scriptPathOf;
        private readonly Func<string, string> _scriptPathForName;

        internal ModuleSubContextEntries() : this(
            AssetDatabase.GetAssetPath,
            name => AssetDatabase.GetAssetPath(new ContextScriptResolver().ForName(name)))
        {
        }

        internal ModuleSubContextEntries(
            Func<Object, string> scriptPathOf,
            Func<string, string> scriptPathForName)
        {
            _scriptPathOf = scriptPathOf;
            _scriptPathForName = scriptPathForName;
        }

        /// <summary>The positions in this Root's list that the module owns, in the order listed.</summary>
        internal IReadOnlyList<int> IndexesIn(IReadOnlyList<SubContextData> entries, string moduleAssetPath)
        {
            var found = new List<int>();

            string module = Folder(moduleAssetPath);

            if (entries == null || string.IsNullOrEmpty(module)) return found;

            for (var index = 0; index < entries.Count; index++)
            {
                if (IsInside(ScriptPathOf(entries[index]), module)) found.Add(index);
            }

            return found;
        }

        /// <summary>
        /// Takes the named positions out. It walks backwards because removing the first would move
        /// every position after it, and the second removal would then take the wrong entry - which
        /// is the whole reason this is a method rather than a loop at the call site.
        /// </summary>
        internal void RemoveAt(List<SubContextData> entries, IReadOnlyList<int> indexes)
        {
            if (entries == null || indexes == null) return;

            for (int at = indexes.Count - 1; at >= 0; at--)
            {
                int index = indexes[at];

                if (index >= 0 && index < entries.Count) entries.RemoveAt(index);
            }
        }

        /// <summary>
        /// The script the entry points at, by reference where there is one and through the name
        /// where there is not.
        /// </summary>
        private string ScriptPathOf(SubContextData entry)
        {
            string byReference = entry.ContextScript == null ? null : _scriptPathOf(entry.ContextScript);

            return string.IsNullOrEmpty(byReference)
                ? _scriptPathForName(entry.ContextFullName)
                : byReference;
        }

        /// <summary>
        /// The module folder as a path that can only match inside it. The trailing slash is what
        /// keeps PlayerHudModule out of PlayerModule's answer.
        /// </summary>
        private string Folder(string moduleAssetPath)
        {
            if (string.IsNullOrEmpty(moduleAssetPath)) return null;

            return moduleAssetPath.Replace('\\', '/').TrimEnd('/') + "/";
        }

        private bool IsInside(string assetPath, string moduleFolder)
        {
            return !string.IsNullOrEmpty(assetPath)
                   && assetPath.Replace('\\', '/')
                       .StartsWith(moduleFolder, StringComparison.OrdinalIgnoreCase);
        }
    }
}

#endif
