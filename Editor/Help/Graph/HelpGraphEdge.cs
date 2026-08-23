#if UNITY_EDITOR

namespace FlowIoC.Editor.Help.Graph
{
    internal enum HelpGraphEdgeKind
    {
        Normal,
        Forbidden
    }

    /// <summary>
    /// An arrow between two boxes. A Forbidden edge is drawn in the warning colour and crossed
    /// out: it stands for a route the architecture rules out, such as a signal reaching straight
    /// into a Model.
    /// </summary>
    internal class HelpGraphEdge
    {
        public HelpGraphEdge(string fromId, string toId, string label,
            HelpGraphEdgeKind kind = HelpGraphEdgeKind.Normal)
        {
            FromId = fromId;
            ToId = toId;
            Label = label;
            Kind = kind;
        }

        public string FromId { get; }
        public string ToId { get; }
        public string Label { get; }
        public HelpGraphEdgeKind Kind { get; }
    }
}

#endif
