using System;
using UnityEngine;

namespace FlowIoC.Editor.DependencyDiagram.Data
{
    [Serializable]
    public class DiagramEdge
    {
        public string Id;
        public string SourceNodeId;
        public string TargetNodeId;
        public EdgeType Type;
        public string Label;
        public Color Color;
        
        public DiagramEdge(string id, string sourceNodeId, string targetNodeId, EdgeType type, string label = null)
        {
            Id = id;
            SourceNodeId = sourceNodeId;
            TargetNodeId = targetNodeId;
            Type = type;
            Label = label;
            Color = GetColorForEdgeType(type);
        }
        
        private Color GetColorForType(EdgeType type)
        {
            return type switch
            {
                EdgeType.SignalBinding => new Color(0.05f, 0.9f, 0.05f),     // Parlak yeşil
                EdgeType.CommandBinding => new Color(0.95f, 0.5f, 0.05f),    // Turuncu
                EdgeType.MediatorBinding => new Color(0.7f, 0.05f, 0.95f),   // Mor
                EdgeType.InjectionBinding => new Color(0.05f, 0.6f, 0.95f),  // Mavi
                EdgeType.SequentialCommand => new Color(0.95f, 0.1f, 0.1f),  // Kırmızı
                EdgeType.ParallelCommand => new Color(0.95f, 0.1f, 0.95f),   // Pembe
                EdgeType.ViewBinding => new Color(0.1f, 0.95f, 0.95f),       // Turkuaz
                EdgeType.InjectableBinding => new Color(0.95f, 0.95f, 0.1f), // Sarı
                EdgeType.CommandUsage => new Color(0.8f, 0.4f, 0.0f),        // Koyu turuncu
                EdgeType.InjectionReference => new Color(0.0f, 0.4f, 0.8f),  // Koyu mavi
                EdgeType.InjectionUsage => new Color(0.0f, 0.7f, 0.7f),      // Turkuaz mavi
                _ => new Color(0.7f, 0.7f, 0.7f)                            // Gri
            };
        }
        
        public static Color GetColorForEdgeType(EdgeType type)
        {
            return type switch
            {
                EdgeType.SignalBinding => new Color(0.05f, 0.9f, 0.05f),     // Bright green
                EdgeType.CommandBinding => new Color(0.95f, 0.5f, 0.05f),    // Orange
                EdgeType.MediatorBinding => new Color(0.7f, 0.05f, 0.95f),   // Purple
                EdgeType.InjectionBinding => new Color(0.05f, 0.6f, 0.95f),  // Blue
                EdgeType.SequentialCommand => new Color(0.95f, 0.1f, 0.1f),  // Red
                EdgeType.ParallelCommand => new Color(0.95f, 0.1f, 0.95f),   // Pink
                EdgeType.ViewBinding => new Color(0.1f, 0.95f, 0.95f),       // Turquoise
                EdgeType.InjectableBinding => new Color(0.95f, 0.95f, 0.1f), // Yellow
                EdgeType.CommandUsage => new Color(0.8f, 0.4f, 0.0f),        // Dark orange
                EdgeType.InjectionReference => new Color(0.0f, 0.4f, 0.8f),  // Dark blue
                EdgeType.InjectionUsage => new Color(0.0f, 0.7f, 0.7f),      // Turquoise blue
                _ => new Color(0.7f, 0.7f, 0.7f)                            // Gray
            };
        }
    }
    
    public enum EdgeType
    {
        SignalBinding,
        CommandBinding,
        MediatorBinding,
        InjectionBinding,
        SequentialCommand,
        ParallelCommand,
        ViewBinding,
        InjectableBinding,
        CommandUsage,        // Eklenen: Command kullanım bağlantısı
        InjectionReference,  // Eklenen: Injectable'lar arası referans bağlantısı
        InjectionUsage,      // Eklenen: Injection kullanım bağlantısı
        Unknown
    }
} 