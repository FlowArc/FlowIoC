#if UNITY_EDITOR
using System.Collections.Generic;
using FlowIoC.ConsoleModule;

namespace FlowIoC.Editor.Console
{
    public class FlowNode
    {
        public int FlowId;
        public readonly List<ConsoleLog> Logs = new();
        public readonly List<FlowNode> Children = new();
    }

    /// <summary>
    /// Turns the flat log list into the tree the window draws, out of the (FlowId, ParentFlowId)
    /// pairs the runtime stamps on every log.
    ///
    /// A silenced command contributes no node - [HideCommandLog] suppresses the framework's lines
    /// and there is nothing left to make a node from - so a log the developer wrote inside one
    /// sits directly under the flow's root. That is deliberate: inventing a node for a command
    /// somebody asked to hide would undo the request.
    /// </summary>
    public class FlowConsoleFlowTreeBuilder
    {
        public List<FlowNode> Build(IReadOnlyList<ConsoleLog> logs)
        {
            var roots = new List<FlowNode>();
            if (logs == null) return roots;

            var byFlowId = new Dictionary<int, FlowNode>();
            var order = new List<FlowNode>();
            var parentOf = new Dictionary<int, int>();

            for (int i = 0; i < logs.Count; i++)
            {
                ConsoleLog log = logs[i];

                // A log outside any flow is its own root, holding only itself. Grouping them all
                // under one node would read as a flow that never happened.
                if (log.FlowId == 0)
                {
                    var loose = new FlowNode {FlowId = 0};
                    loose.Logs.Add(log);
                    order.Add(loose);
                    roots.Add(loose);
                    continue;
                }

                if (!byFlowId.TryGetValue(log.FlowId, out FlowNode node))
                {
                    node = new FlowNode {FlowId = log.FlowId};
                    byFlowId[log.FlowId] = node;
                    order.Add(node);
                    parentOf[log.FlowId] = log.ParentFlowId;
                }

                node.Logs.Add(log);
            }

            for (int i = 0; i < order.Count; i++)
            {
                FlowNode node = order[i];
                if (node.FlowId == 0) continue;

                int parentId = parentOf[node.FlowId];

                // A parent trimmed away by MaxLogCount, or filtered out, leaves an orphan. It is
                // drawn as a root rather than dropped.
                if (parentId != 0 && byFlowId.TryGetValue(parentId, out FlowNode parent))
                    parent.Children.Add(node);
                else
                    roots.Add(node);
            }

            return roots;
        }
    }
}
#endif
