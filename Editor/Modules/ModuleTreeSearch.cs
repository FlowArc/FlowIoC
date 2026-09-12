#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;

namespace FlowIoC.Editor.Modules
{
    /// <summary>
    /// The search over a module tree, as every list that offers one runs it: a case-insensitive
    /// substring of the module name. A match is kept with everything inside it, because what is
    /// inside is what picking or deleting the match would take - hiding it would answer the search
    /// with less than the reader needs. The modules above a match are kept too, so a match is never
    /// shown without the module it lives in. The rows come back in the tree's own order, and a
    /// shown row's ancestors are always among them, which is what lets a tree painter hang each
    /// row from its parent as if nothing had been filtered.
    /// </summary>
    internal class ModuleTreeSearch
    {
        internal List<ModuleTreeRowEVO<T>> Filter<T>(IReadOnlyList<ModuleTreeRowEVO<T>> rows, string search)
            where T : IModuleTreeItem
        {
            if (string.IsNullOrEmpty(search)) return rows.ToList();

            var kept = new List<ModuleTreeRowEVO<T>>();

            foreach (ModuleTreeRowEVO<T> row in rows)
            {
                if (Matches(row, search) || Ancestors(row).Any(ancestor => Matches(ancestor, search))
                                         || row.Descendants.Any(descendant => Matches(descendant, search)))
                    kept.Add(row);
            }

            return kept;
        }

        private static bool Matches<T>(ModuleTreeRowEVO<T> row, string search) where T : IModuleTreeItem =>
            row.Row.Name != null && row.Row.Name.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;

        private static IEnumerable<ModuleTreeRowEVO<T>> Ancestors<T>(ModuleTreeRowEVO<T> row) where T : IModuleTreeItem
        {
            for (ModuleTreeRowEVO<T> parent = row.Parent; parent != null; parent = parent.Parent)
                yield return parent;
        }
    }
}
#endif
