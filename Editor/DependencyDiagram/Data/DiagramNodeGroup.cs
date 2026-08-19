using System;
using System.Collections.Generic;
using UnityEngine;

namespace FlowIoC.Editor.DependencyDiagram.Data
{
    [Serializable]
    public class DiagramNodeGroup
    {
        public string Id;
        public string Name;
        public NodeType Type;
        public Color Color;
        public Rect Position;
        public List<string> NodeIds = new List<string>();

        // Enhanced properties for command sequences
        public bool IsSequenceGroup { get; set; }
        public bool IsParallelGroup { get; set; }
        public string TriggerSignalId { get; set; }
        public string ParentGroupId { get; set; }
        public List<string> SubGroupIds { get; set; } = new List<string>();
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
        
        public DiagramNodeGroup(string id, string name, NodeType type, Color color, Rect position = default)
        {
            Id = id;
            Name = name;
            Type = type;
            Color = color;
            Position = position == default ? new Rect(0, 0, 400, 300) : position;
        }

        public void AddMetadata(string key, object value)
        {
            if (Metadata.ContainsKey(key))
                Metadata[key] = value;
            else
                Metadata.Add(key, value);
        }

        public T GetMetadata<T>(string key, T defaultValue = default)
        {
            if (Metadata.TryGetValue(key, out var value) && value is T typedValue)
                return typedValue;
            return defaultValue;
        }
        
        public static Color GetColorForNodeType(NodeType type)
        {
            return type switch
            {
                NodeType.Context => new Color(0.2f, 0.4f, 0.6f),
                NodeType.Signal => new Color(0.6f, 0.2f, 0.2f),
                NodeType.Command => new Color(0.4f, 0.6f, 0.2f),
                NodeType.View => new Color(0.2f, 0.6f, 0.6f),
                NodeType.Mediator => new Color(0.6f, 0.2f, 0.6f),
                NodeType.Injectable => new Color(0.6f, 0.4f, 0.2f),
                _ => new Color(0.5f, 0.5f, 0.5f)
            };
        }

        // Helper method to create sequence/parallel group
        public static DiagramNodeGroup CreateCommandSequenceGroup(string id, string name, string triggerSignalId, bool isSequence, Color baseColor)
        {
            var group = new DiagramNodeGroup(
                id,
                name,
                NodeType.Command,
                new Color(baseColor.r, baseColor.g, baseColor.b, 0.8f) // Slightly transparent
            );
            
            group.IsSequenceGroup = isSequence;
            group.IsParallelGroup = !isSequence;
            group.TriggerSignalId = triggerSignalId;
            
            return group;
        }
    }
} 