#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;

namespace FlowIoC.Editor.ModuleScanner
{
    /// <summary>
    /// The order the panel draws its rows in: a module under the module it lives in, and
    /// everything inside a module before the module after it.
    ///
    /// The scan hands over a flat list in whatever order the folders came, each row naming its
    /// parent and nothing more. Turning that into a tree here rather than in the window keeps
    /// the window to drawing, and lets a test describe a project in four lines.
    /// </summary>
    internal class ModuleTree
    {
        internal List<ModuleTreeRowEVO> Build(IReadOnlyList<ModuleRowEVO> rows)
        {
            var names = new HashSet<string>(rows.Select(row => row.Name));
            var children = new Dictionary<string, List<ModuleRowEVO>>();
            var top = new List<ModuleRowEVO>();

            // A row whose parent the scan did not find is still a module, so it goes to the top
            // rather than nowhere: a list that loses a row is worse than one that misplaces it.
            foreach (ModuleRowEVO row in rows)
            {
                if (row.ParentName == null || !names.Contains(row.ParentName))
                {
                    top.Add(row);

                    continue;
                }

                if (!children.TryGetValue(row.ParentName, out List<ModuleRowEVO> siblings))
                    children[row.ParentName] = siblings = new List<ModuleRowEVO>();

                siblings.Add(row);
            }

            var tree = new List<ModuleTreeRowEVO>();

            Add(top, null, children, tree);

            return tree;
        }

        /// <summary>
        /// One level of siblings, each followed by everything inside it. An entry's HasIssue is
        /// settled after its children are added, because that is when what is under it is known.
        /// </summary>
        private void Add(
            List<ModuleRowEVO> siblings, ModuleTreeRowEVO parent,
            Dictionary<string, List<ModuleRowEVO>> children, List<ModuleTreeRowEVO> tree)
        {
            foreach (ModuleRowEVO row in Ordered(siblings))
            {
                var entry = new ModuleTreeRowEVO
                {
                    Row = row,
                    Depth = parent == null ? 0 : parent.Depth + 1,
                    Parent = parent
                };

                tree.Add(entry);

                int first = tree.Count;

                if (children.TryGetValue(row.Name, out List<ModuleRowEVO> inside))
                    Add(inside, entry, children, tree);

                entry.HasIssue = row.Status != ModuleCheckStatus.Ok
                                 || tree.Skip(first).Any(descendant => descendant.HasIssue);
            }
        }

        /// <summary>
        /// Sub modules, then screens, then tests - the order the kinds are declared in - and by
        /// name inside each kind, so the badge column reads as groups rather than as a shuffle.
        /// </summary>
        private IEnumerable<ModuleRowEVO> Ordered(List<ModuleRowEVO> siblings)
        {
            return siblings
                .OrderBy(row => row.Kind)
                .ThenBy(row => row.Name, StringComparer.Ordinal);
        }
    }
}

#endif
