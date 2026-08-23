#if UNITY_EDITOR

namespace FlowIoC.Editor.Help.Graph
{
    /// <summary>
    /// One stop on the walk through a diagram: the box that lights up, the rule it stands for and
    /// the code that rule produces.
    /// </summary>
    internal class HelpGraphStep
    {
        public HelpGraphStep(string nodeId, string rule, string code)
        {
            NodeId = nodeId;
            Rule = rule;
            Code = code;
        }

        public string NodeId { get; }
        public string Rule { get; }
        public string Code { get; }
    }
}

#endif
