#if UNITY_EDITOR

using System.Collections.Generic;

namespace FlowIoC.Editor.Help
{
    /// <summary>
    /// A folder in the layout page's tree, with the one line that says what belongs in it.
    /// </summary>
    internal class HelpTreeNode
    {
        public HelpTreeNode(string name, string comment, params HelpTreeNode[] children)
        {
            Name = name;
            Comment = comment;
            Children = children ?? new HelpTreeNode[0];
        }

        public string Name { get; }
        public string Comment { get; }
        public IReadOnlyList<HelpTreeNode> Children { get; }

        public IEnumerable<HelpTreeNode> Descendants()
        {
            yield return this;

            foreach (HelpTreeNode child in Children)
            {
                foreach (HelpTreeNode descendant in child.Descendants())
                    yield return descendant;
            }
        }
    }
}

#endif
