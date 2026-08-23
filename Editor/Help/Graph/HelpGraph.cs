#if UNITY_EDITOR

using System.Collections.Generic;

namespace FlowIoC.Editor.Help.Graph
{
    /// <summary>
    /// A whole diagram as data. It holds no Unity types, so a test can assert over it without an
    /// Editor window ever opening.
    /// </summary>
    internal class HelpGraph
    {
        public HelpGraph(IReadOnlyList<HelpGraphNode> nodes, IReadOnlyList<HelpGraphEdge> edges,
            IReadOnlyList<HelpGraphStep> steps, float scale = 1f)
        {
            Nodes = nodes;
            Edges = edges;
            Steps = steps;
            Scale = scale <= 0f ? 1f : scale;
        }

        /// <summary>
        /// How large this diagram is drawn, against the size a stepped diagram uses. A page that
        /// is one map rather than a walk carries the whole story at once and earns more room.
        /// </summary>
        public float Scale { get; }

        public IReadOnlyList<HelpGraphNode> Nodes { get; }
        public IReadOnlyList<HelpGraphEdge> Edges { get; }
        public IReadOnlyList<HelpGraphStep> Steps { get; }

        public bool HasNode(string id) => Node(id) != null;

        public HelpGraphNode Node(string id)
        {
            foreach (HelpGraphNode node in Nodes)
            {
                if (node.Id == id)
                    return node;
            }

            return null;
        }
    }
}

#endif