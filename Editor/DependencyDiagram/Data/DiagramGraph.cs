using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FlowIoC.Editor.DependencyDiagram.Data
{
    [Serializable]
    public class DiagramGraph
    {
        public string Id;
        public string Name;
        public DiagramViewType ViewType;
        public List<DiagramNode> Nodes = new List<DiagramNode>();
        public List<DiagramEdge> Edges = new List<DiagramEdge>();
        public Dictionary<string, DiagramNodeGroup> Groups = new Dictionary<string, DiagramNodeGroup>();
        
        // Track command sequences for better organization
        public Dictionary<string, List<string>> SignalToCommandSequences { get; private set; } = new Dictionary<string, List<string>>();

        public DiagramGraph(string id, string name, DiagramViewType viewType = DiagramViewType.CategoryView)
        {
            Id = id;
            Name = name;
            ViewType = viewType;
        }
        
        public DiagramNode AddNode(string id, string name, string typeName, string filePath, NodeType type, Rect position = default)
        {
            var node = new DiagramNode(id, name, typeName, filePath, type, position);
            Nodes.Add(node);
            return node;
        }
        
        public DiagramEdge AddEdge(string id, string sourceNodeId, string targetNodeId, EdgeType type, string label = null)
        {
            if (!NodeExists(sourceNodeId) || !NodeExists(targetNodeId))
            {
                Debug.LogWarning($"Cannot create edge {id}: Source or target node doesn't exist");
                return null;
            }
            
            var sourceNode = GetNode(sourceNodeId);
            var targetNode = GetNode(targetNodeId);
            
            if (sourceNode == null || targetNode == null)
            {
                Debug.LogWarning($"Cannot create edge {id}: Source or target node is null");
                return null;
            }
            
            // Check if there's already an edge between these nodes
            var existingEdge = Edges.FirstOrDefault(e => 
                e.SourceNodeId == sourceNodeId && 
                e.TargetNodeId == targetNodeId);
            
            if (existingEdge != null)
            {
                // If the new edge is a sequential or parallel command, it takes precedence
                if ((type == EdgeType.SequentialCommand || type == EdgeType.ParallelCommand) &&
                    (existingEdge.Type != EdgeType.SequentialCommand && existingEdge.Type != EdgeType.ParallelCommand))
                {
                    // Replace the existing edge's type and keep its ID
                    existingEdge.Type = type;
                    if (!string.IsNullOrEmpty(label))
                    {
                        existingEdge.Label = label;
                    }
                    
                    // Update color based on new type
                    existingEdge.Color = DiagramEdge.GetColorForEdgeType(type);
                    
                    return existingEdge;
                }
                
                // Otherwise, just return the existing edge without creating a duplicate
                return existingEdge;
            }
            
            if ((sourceNode.Type == NodeType.Context || targetNode.Type == NodeType.Context) && 
                (type != EdgeType.InjectionBinding))
            {
                Debug.Log($"Creating special edge {id} for Context node. SourceType: {sourceNode.Type}, TargetType: {targetNode.Type}, EdgeType: {type}");
            }
            
            var edge = new DiagramEdge(id, sourceNodeId, targetNodeId, type, label);
            Edges.Add(edge);
            
            sourceNode.Outputs.Add(edge.Id);
            targetNode.Inputs.Add(edge.Id);
            
            return edge;
        }
        
        public DiagramNodeGroup AddGroup(string id, string name, NodeType type, Color color)
        {
            var group = new DiagramNodeGroup(id, name, type, color);
            Groups[id] = group;
            return group;
        }

        // Add a command sequence group
        public DiagramNodeGroup AddCommandSequenceGroup(string name, string triggerSignalId, bool isSequence, Color baseColor = default)
        {
            string id = $"group_cmdseq_{Guid.NewGuid()}";
            if (baseColor == default)
            {
                baseColor = DiagramNodeGroup.GetColorForNodeType(NodeType.Command);
            }
            
            var group = DiagramNodeGroup.CreateCommandSequenceGroup(id, name, triggerSignalId, isSequence, baseColor);
            Groups[id] = group;
            
            // Track this sequence group by its trigger signal
            if (!string.IsNullOrEmpty(triggerSignalId))
            {
                if (!SignalToCommandSequences.ContainsKey(triggerSignalId))
                {
                    SignalToCommandSequences[triggerSignalId] = new List<string>();
                }
                SignalToCommandSequences[triggerSignalId].Add(id);
            }
            
            return group;
        }
        
        // Add a subgroup to a parent group
        public void AddSubGroup(string parentGroupId, string childGroupId)
        {
            if (!Groups.ContainsKey(parentGroupId) || !Groups.ContainsKey(childGroupId))
            {
                Debug.LogWarning($"Cannot add subgroup: Parent or child group doesn't exist");
                return;
            }
            
            var parentGroup = Groups[parentGroupId];
            var childGroup = Groups[childGroupId];
            
            parentGroup.SubGroupIds.Add(childGroupId);
            childGroup.ParentGroupId = parentGroupId;
        }
        
        public void AddNodeToGroup(string nodeId, string groupId)
        {
            if (!NodeExists(nodeId) || !Groups.ContainsKey(groupId))
            {
                Debug.LogWarning($"Cannot add node {nodeId} to group {groupId}: Node or group doesn't exist");
                return;
            }
            
            Groups[groupId].NodeIds.Add(nodeId);
        }
        
        public DiagramNode GetNode(string id)
        {
            return Nodes.FirstOrDefault(n => n.Id == id);
        }
        
        public DiagramEdge GetEdge(string id)
        {
            return Edges.FirstOrDefault(e => e.Id == id);
        }
        
        public bool NodeExists(string id)
        {
            return Nodes.Any(n => n.Id == id);
        }
        
        public bool EdgeExists(string id)
        {
            return Edges.Any(e => e.Id == id);
        }
        
        public void Clear()
        {
            Nodes.Clear();
            Edges.Clear();
            Groups.Clear();
            SignalToCommandSequences.Clear();
        }

        // Get all nodes in a group, including those in subgroups
        public IEnumerable<DiagramNode> GetAllNodesInGroup(string groupId)
        {
            if (!Groups.ContainsKey(groupId))
                return Enumerable.Empty<DiagramNode>();
            
            var group = Groups[groupId];
            var result = group.NodeIds.Select(GetNode).Where(n => n != null).ToList();
            
            // Add nodes from subgroups recursively
            foreach (var subGroupId in group.SubGroupIds)
            {
                result.AddRange(GetAllNodesInGroup(subGroupId));
            }
            
            return result;
        }

        // Get all command nodes triggered by a signal
        public IEnumerable<DiagramNode> GetCommandsTriggeredBySignal(string signalId)
        {
            if (!SignalToCommandSequences.ContainsKey(signalId))
                return Enumerable.Empty<DiagramNode>();
            
            var result = new List<DiagramNode>();
            foreach (var sequenceGroupId in SignalToCommandSequences[signalId])
            {
                result.AddRange(GetAllNodesInGroup(sequenceGroupId));
            }
            
            return result;
        }
    }
    
    public enum DiagramViewType
    {
        CategoryView,
        ContextDiagram,
        SignalFlow,
        CommandFlow,
        FlowView,
        CompactView
    }
} 