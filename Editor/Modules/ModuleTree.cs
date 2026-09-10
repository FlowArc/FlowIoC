#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;

namespace FlowIoC.Editor.Modules
{
    /// <summary>
    /// The order a module list is drawn in: a module under the module it lives in, and
    /// everything inside a module before the module after it.
    ///
    /// Whoever lists modules hands over a flat list in whatever order they came, each naming its
    /// parent and nothing more. Turning that into a tree here rather than in each window keeps
    /// the windows to drawing, gives every list the same order, and lets a test describe a
    /// project in four lines.
    /// </summary>
    internal class ModuleTree
    {
        internal List<ModuleTreeRowEVO<T>> Build<T>(IReadOnlyList<T> rows) where T : IModuleTreeItem
        {
            var names = new HashSet<string>(rows.Select(row => row.Name));
            var children = new Dictionary<string, List<T>>();
            var top = new List<T>();

            // A row whose parent the list does not hold is still a module, so it goes to the top
            // rather than nowhere: a list that loses a row is worse than one that misplaces it.
            foreach (T row in rows)
            {
                if (row.ParentName == null || !names.Contains(row.ParentName))
                {
                    top.Add(row);

                    continue;
                }

                if (!children.TryGetValue(row.ParentName, out List<T> siblings))
                    children[row.ParentName] = siblings = new List<T>();

                siblings.Add(row);
            }

            var tree = new List<ModuleTreeRowEVO<T>>();

            Add(top, null, children, tree);

            return tree;
        }

        /// <summary>
        /// One level of siblings, each followed by everything inside it. An entry's descendants
        /// are the entries added between it and the end of its walk, so they are collected once
        /// the walk under it is done.
        /// </summary>
        private void Add<T>(
            List<T> siblings, ModuleTreeRowEVO<T> parent,
            Dictionary<string, List<T>> children, List<ModuleTreeRowEVO<T>> tree) where T : IModuleTreeItem
        {
            foreach (T row in Ordered(siblings))
            {
                var entry = new ModuleTreeRowEVO<T>
                {
                    Row = row,
                    Depth = parent == null ? 0 : parent.Depth + 1,
                    Parent = parent
                };

                tree.Add(entry);

                int first = tree.Count;

                if (children.TryGetValue(row.Name, out List<T> inside))
                    Add(inside, entry, children, tree);

                for (int index = first; index < tree.Count; index++)
                    entry.Descendants.Add(tree[index]);
            }
        }

        /// <summary>
        /// Sub modules, then screens, then tests - the order the kinds are declared in - and by
        /// name inside each kind, so the badge column reads as groups rather than as a shuffle.
        /// </summary>
        private IEnumerable<T> Ordered<T>(List<T> siblings) where T : IModuleTreeItem
        {
            return siblings
                .OrderBy(row => row.Kind)
                .ThenBy(row => row.Name, StringComparer.Ordinal);
        }
    }
}

#endif
