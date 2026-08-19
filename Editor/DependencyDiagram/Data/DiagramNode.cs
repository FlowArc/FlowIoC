using System;
using System.Collections.Generic;
using UnityEngine;

namespace FlowIoC.Editor.DependencyDiagram.Data
{
    [Serializable]
    public class DiagramNode
    {
        public string Id;
        public string Name;
        public string TypeName;
        public string FilePath;
        public NodeType Type;
        public Rect Position;
        public List<string> Inputs = new List<string>();
        public List<string> Outputs = new List<string>();
        
        // Additional metadata for command execution info
        public Dictionary<string, string> Metadata = new Dictionary<string, string>();
        public bool IsSequenceCommand { get; set; }
        public bool IsParallelCommand { get; set; }
        public int ExecutionOrder { get; set; }
        public string GroupName { get; set; }
        
        public DiagramNode(string id, string name, string typeName, string filePath, NodeType type, Rect position = default)
        {
            Id = id;
            Name = name;
            TypeName = typeName;
            FilePath = filePath;
            Type = type;
            Position = position == default ? new Rect(0, 0, 200, 100) : position;
            ExecutionOrder = 0;
            GroupName = string.Empty;
        }
        
        public string GetDisplayName()
        {
            // For command nodes, include sequence information in the display name
            if (Type == NodeType.Command)
            {
                // If name already contains sequence info (added during analysis), use it as is
                if (Name.Contains("[Seq:") || Name.Contains("[Par:") || Name.Contains("[Btn:") || Name.Contains("[BtnPar:"))
                {
                    return Name;
                }
                
                // Otherwise add basic sequence info
                if (IsSequenceCommand)
                {
                    return $"{Name} [Seq:{ExecutionOrder}]";
                }
                else if (IsParallelCommand)
                {
                    return $"{Name} [Par:{ExecutionOrder}]";
                }
            }
            
            return string.IsNullOrEmpty(Name) ? TypeName : Name;
        }
        
        public void AddMetadata(string key, string value)
        {
            if (Metadata.ContainsKey(key))
            {
                Metadata[key] = value;
            }
            else
            {
                Metadata.Add(key, value);
            }
        }
        
        public string GetMetadata(string key)
        {
            return Metadata.TryGetValue(key, out var value) ? value : null;
        }
    }
    
    public enum NodeType
    {
        Context,
        Signal,
        Command,
        View,
        Mediator,
        Injectable,
        Unknown
    }
} 