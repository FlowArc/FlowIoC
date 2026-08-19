using System.Linq;
using System.Text;
using FlowIoC.Editor.DependencyDiagram.Data;
using UnityEditor;
using UnityEngine;

namespace FlowIoC.Editor.DependencyDiagram.View
{
    public class DetailPanel
    {
        private const float PANEL_WIDTH = 300f;
        private const float MIN_HEIGHT = 200f;
        private const float PADDING = 10f;
        private const float HEADER_HEIGHT = 30f;
        private const float PROPERTY_HEIGHT = 20f;
        
        private Rect _position;
        private DiagramNode _selectedNode;
        private DiagramNodeGroup _selectedGroup;
        private DiagramGraph _graph;
        private Vector2 _scrollPosition;
        private bool _isVisible = false;
        
        private GUIStyle _headerStyle;
        private GUIStyle _propertyNameStyle;
        private GUIStyle _propertyValueStyle;
        private GUIStyle _sectionHeaderStyle;
        
        public DetailPanel(DiagramGraph graph)
        {
            _graph = graph;
            InitializeStyles();
        }
        
        private void InitializeStyles()
        {
            _headerStyle = new GUIStyle(EditorStyles.boldLabel);
            _headerStyle.fontSize = 14;
            _headerStyle.alignment = TextAnchor.MiddleLeft;
            
            _propertyNameStyle = new GUIStyle(EditorStyles.label);
            _propertyNameStyle.fontStyle = FontStyle.Bold;
            _propertyNameStyle.alignment = TextAnchor.MiddleLeft;
            
            _propertyValueStyle = new GUIStyle(EditorStyles.label);
            _propertyValueStyle.wordWrap = true;
            _propertyValueStyle.alignment = TextAnchor.MiddleLeft;
            
            _sectionHeaderStyle = new GUIStyle(EditorStyles.boldLabel);
            _sectionHeaderStyle.fontSize = 12;
            _sectionHeaderStyle.alignment = TextAnchor.MiddleLeft;
        }
        
        public void Show(DiagramNode node, Rect parentPosition)
        {
            _selectedNode = node;
            _selectedGroup = null;
            _isVisible = true;
            CalculatePosition(parentPosition);
        }
        
        public void Show(DiagramNodeGroup group, Rect parentPosition)
        {
            _selectedNode = null;
            _selectedGroup = group;
            _isVisible = true;
            CalculatePosition(parentPosition);
        }
        
        public void Hide()
        {
            _isVisible = false;
            _selectedNode = null;
            _selectedGroup = null;
        }
        
        private void CalculatePosition(Rect parentPosition)
        {
            // Position panel on the right side of the parent window
            _position = new Rect(
                parentPosition.width - PANEL_WIDTH - PADDING,
                PADDING + HEADER_HEIGHT,
                PANEL_WIDTH,
                Mathf.Min(parentPosition.height - PADDING * 2 - HEADER_HEIGHT, 600)
            );
        }
        
        public void Draw()
        {
            if (!_isVisible) return;
            
            // Draw panel background
            EditorGUI.DrawRect(_position, new Color(0.2f, 0.2f, 0.2f, 0.9f));
            
            // Draw panel border
            DrawRectOutline(_position, new Color(0.5f, 0.5f, 0.5f, 1.0f), 1f);
            
            // Draw close button
            Rect closeButtonRect = new Rect(
                _position.xMax - 25,
                _position.y + 5,
                20,
                20
            );
            
            if (GUI.Button(closeButtonRect, "×"))
            {
                Hide();
                return;
            }
            
            // Draw header
            Rect headerRect = new Rect(
                _position.x + PADDING,
                _position.y + PADDING,
                _position.width - PADDING * 2 - 25,
                HEADER_HEIGHT
            );
            
            string headerText = "";
            if (_selectedNode != null)
            {
                headerText = $"{_selectedNode.Type}: {_selectedNode.Name}";
            }
            else if (_selectedGroup != null)
            {
                headerText = $"Group: {_selectedGroup.Name}";
            }
            
            EditorGUI.LabelField(headerRect, headerText, _headerStyle);
            
            // Draw content
            Rect contentRect = new Rect(
                _position.x + PADDING,
                _position.y + PADDING + HEADER_HEIGHT,
                _position.width - PADDING * 2,
                _position.height - PADDING * 2 - HEADER_HEIGHT
            );
            
            // Begin scroll view
            _scrollPosition = GUI.BeginScrollView(
                contentRect,
                _scrollPosition,
                new Rect(0, 0, contentRect.width - 20, CalculateContentHeight())
            );
            
            // Draw content based on selection
            if (_selectedNode != null)
            {
                DrawNodeDetails(contentRect.width - 20);
            }
            else if (_selectedGroup != null)
            {
                DrawGroupDetails(contentRect.width - 20);
            }
            
            GUI.EndScrollView();
        }
        
        private float CalculateContentHeight()
        {
            float height = 0;
            
            if (_selectedNode != null)
            {
                // Basic properties
                height += PROPERTY_HEIGHT * 4; // Type, Name, TypeName, FilePath
                
                // Metadata
                height += PROPERTY_HEIGHT; // Section header
                height += PROPERTY_HEIGHT * _selectedNode.Metadata.Count;
                
                // Command specific
                if (_selectedNode.Type == NodeType.Command)
                {
                    height += PROPERTY_HEIGHT * 3; // Execution info
                    
                    // Dependencies
                    height += PROPERTY_HEIGHT; // Section header
                    height += PROPERTY_HEIGHT * 2; // Inputs/Outputs headers
                    height += PROPERTY_HEIGHT * _selectedNode.Inputs.Count;
                    height += PROPERTY_HEIGHT * _selectedNode.Outputs.Count;
                }
                
                // Signal specific
                if (_selectedNode.Type == NodeType.Signal)
                {
                    height += PROPERTY_HEIGHT; // Section header
                    
                    // Connected Commands
                    var connectedCommands = GetCommandsTriggeredBySignal(_selectedNode.Id);
                    height += PROPERTY_HEIGHT * (connectedCommands.Count() + 1);
                }
            }
            else if (_selectedGroup != null)
            {
                // Basic properties
                height += PROPERTY_HEIGHT * 3; // Type, Name, Node Count
                
                // Sequence specific
                if (_selectedGroup.IsSequenceGroup || _selectedGroup.IsParallelGroup)
                {
                    height += PROPERTY_HEIGHT * 2; // Execution type, Trigger signal
                    
                    // Nodes in sequence
                    height += PROPERTY_HEIGHT; // Section header
                    height += PROPERTY_HEIGHT * _selectedGroup.NodeIds.Count;
                }
                
                // Metadata
                height += PROPERTY_HEIGHT; // Section header
                height += PROPERTY_HEIGHT * _selectedGroup.Metadata.Count;
            }
            
            return Mathf.Max(height + PADDING * 2, MIN_HEIGHT);
        }
        
        private void DrawNodeDetails(float width)
        {
            float y = 0;
            
            // Type
            DrawProperty("Type", _selectedNode.Type.ToString(), 0, y, width);
            y += PROPERTY_HEIGHT;
            
            // Name
            DrawProperty("Name", _selectedNode.Name, 0, y, width);
            y += PROPERTY_HEIGHT;
            
            // TypeName
            DrawProperty("Full Type", _selectedNode.TypeName, 0, y, width);
            y += PROPERTY_HEIGHT;
            
            // FilePath
            DrawProperty("File Path", string.IsNullOrEmpty(_selectedNode.FilePath) ? "N/A" : _selectedNode.FilePath, 0, y, width);
            y += PROPERTY_HEIGHT;
            
            // Command specific information
            if (_selectedNode.Type == NodeType.Command)
            {
                // Sequence info
                DrawSectionHeader("Execution Info", 0, y, width);
                y += PROPERTY_HEIGHT;
                
                if (_selectedNode.IsSequenceCommand)
                {
                    DrawProperty("Execution Type", "Sequential", 0, y, width);
                }
                else if (_selectedNode.IsParallelCommand)
                {
                    DrawProperty("Execution Type", "Parallel", 0, y, width);
                }
                else
                {
                    DrawProperty("Execution Type", "Standard", 0, y, width);
                }
                y += PROPERTY_HEIGHT;
                
                DrawProperty("Execution Order", _selectedNode.ExecutionOrder.ToString(), 0, y, width);
                y += PROPERTY_HEIGHT;
                
                DrawProperty("Trigger Signal", _selectedNode.GetMetadata("TriggerSignal") ?? "N/A", 0, y, width);
                y += PROPERTY_HEIGHT;
                
                // Dependencies
                DrawSectionHeader("Dependencies", 0, y, width);
                y += PROPERTY_HEIGHT;
                
                // Inputs
                DrawProperty("Inputs", $"({_selectedNode.Inputs.Count})", 0, y, width);
                y += PROPERTY_HEIGHT;
                
                foreach (var inputId in _selectedNode.Inputs)
                {
                    var edge = _graph.GetEdge(inputId);
                    if (edge != null)
                    {
                        var sourceNode = _graph.GetNode(edge.SourceNodeId);
                        if (sourceNode != null)
                        {
                            DrawProperty("  →", $"{sourceNode.Name} ({edge.Type})", 0, y, width);
                            y += PROPERTY_HEIGHT;
                        }
                    }
                }
                
                // Outputs
                DrawProperty("Outputs", $"({_selectedNode.Outputs.Count})", 0, y, width);
                y += PROPERTY_HEIGHT;
                
                foreach (var outputId in _selectedNode.Outputs)
                {
                    var edge = _graph.GetEdge(outputId);
                    if (edge != null)
                    {
                        var targetNode = _graph.GetNode(edge.TargetNodeId);
                        if (targetNode != null)
                        {
                            DrawProperty("  →", $"{targetNode.Name} ({edge.Type})", 0, y, width);
                            y += PROPERTY_HEIGHT;
                        }
                    }
                }
            }
            
            // Signal specific information
            if (_selectedNode.Type == NodeType.Signal)
            {
                DrawSectionHeader("Triggered Commands", 0, y, width);
                y += PROPERTY_HEIGHT;
                
                var connectedCommands = GetCommandsTriggeredBySignal(_selectedNode.Id).ToList();
                
                if (connectedCommands.Count == 0)
                {
                    DrawProperty("Commands", "None", 0, y, width);
                    y += PROPERTY_HEIGHT;
                }
                else
                {
                    DrawProperty("Commands", $"({connectedCommands.Count})", 0, y, width);
                    y += PROPERTY_HEIGHT;
                    
                    foreach (var command in connectedCommands)
                    {
                        string executionType = "";
                        if (command.IsSequenceCommand)
                            executionType = "[SEQ]";
                        else if (command.IsParallelCommand)
                            executionType = "[PAR]";
                        
                        string orderInfo = command.ExecutionOrder > 0 ? $" #{command.ExecutionOrder}" : "";
                        
                        DrawProperty("  →", $"{command.Name} {executionType}{orderInfo}", 0, y, width);
                        y += PROPERTY_HEIGHT;
                    }
                }
            }
            
            // Metadata
            if (_selectedNode.Metadata.Count > 0)
            {
                DrawSectionHeader("Metadata", 0, y, width);
                y += PROPERTY_HEIGHT;
                
                foreach (var kvp in _selectedNode.Metadata)
                {
                    DrawProperty(kvp.Key, kvp.Value, 0, y, width);
                    y += PROPERTY_HEIGHT;
                }
            }
        }
        
        private void DrawGroupDetails(float width)
        {
            float y = 0;
            
            // Type
            DrawProperty("Type", _selectedGroup.Type.ToString(), 0, y, width);
            y += PROPERTY_HEIGHT;
            
            // Name
            DrawProperty("Name", _selectedGroup.Name, 0, y, width);
            y += PROPERTY_HEIGHT;
            
            // Node Count
            DrawProperty("Node Count", _selectedGroup.NodeIds.Count.ToString(), 0, y, width);
            y += PROPERTY_HEIGHT;
            
            // Sequence specific information
            if (_selectedGroup.IsSequenceGroup || _selectedGroup.IsParallelGroup)
            {
                DrawSectionHeader("Execution Info", 0, y, width);
                y += PROPERTY_HEIGHT;
                
                DrawProperty("Execution Type", _selectedGroup.IsSequenceGroup ? "Sequential" : "Parallel", 0, y, width);
                y += PROPERTY_HEIGHT;
                
                if (!string.IsNullOrEmpty(_selectedGroup.TriggerSignalId))
                {
                    var signalNode = _graph.GetNode(_selectedGroup.TriggerSignalId);
                    DrawProperty("Trigger Signal", signalNode != null ? signalNode.Name : "Unknown", 0, y, width);
                }
                else
                {
                    DrawProperty("Trigger Signal", "None", 0, y, width);
                }
                y += PROPERTY_HEIGHT;
                
                // Nodes in sequence
                DrawSectionHeader("Commands in Order", 0, y, width);
                y += PROPERTY_HEIGHT;
                
                var nodes = _selectedGroup.NodeIds
                    .Select(_graph.GetNode)
                    .Where(n => n != null)
                    .OrderBy(n => n.ExecutionOrder)
                    .ToList();
                
                if (nodes.Count == 0)
                {
                    DrawProperty("Commands", "None", 0, y, width);
                    y += PROPERTY_HEIGHT;
                }
                else
                {
                    foreach (var node in nodes)
                    {
                        DrawProperty($"{node.ExecutionOrder}.", node.Name, 0, y, width);
                        y += PROPERTY_HEIGHT;
                    }
                }
            }
            
            // Metadata
            if (_selectedGroup.Metadata.Count > 0)
            {
                DrawSectionHeader("Metadata", 0, y, width);
                y += PROPERTY_HEIGHT;
                
                foreach (var kvp in _selectedGroup.Metadata)
                {
                    DrawProperty(kvp.Key, kvp.Value?.ToString() ?? "null", 0, y, width);
                    y += PROPERTY_HEIGHT;
                }
            }
        }
        
        private void DrawProperty(string name, string value, float x, float y, float width)
        {
            float nameWidth = width * 0.4f;
            
            Rect nameRect = new Rect(x, y, nameWidth, PROPERTY_HEIGHT);
            Rect valueRect = new Rect(x + nameWidth, y, width - nameWidth, PROPERTY_HEIGHT);
            
            EditorGUI.LabelField(nameRect, name, _propertyNameStyle);
            EditorGUI.LabelField(valueRect, value, _propertyValueStyle);
        }
        
        private void DrawSectionHeader(string title, float x, float y, float width)
        {
            Rect rect = new Rect(x, y, width, PROPERTY_HEIGHT);
            
            // Draw line above section header
            Rect lineRect = new Rect(x, y - 2, width, 1);
            EditorGUI.DrawRect(lineRect, new Color(0.5f, 0.5f, 0.5f, 0.5f));
            
            EditorGUI.LabelField(rect, title, _sectionHeaderStyle);
        }
        
        private void DrawRectOutline(Rect rect, Color color, float thickness)
        {
            // Top
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, thickness), color);
            // Left
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, thickness, rect.height), color);
            // Right
            EditorGUI.DrawRect(new Rect(rect.x + rect.width - thickness, rect.y, thickness, rect.height), color);
            // Bottom
            EditorGUI.DrawRect(new Rect(rect.x, rect.y + rect.height - thickness, rect.width, thickness), color);
        }
        
        private System.Collections.Generic.IEnumerable<DiagramNode> GetCommandsTriggeredBySignal(string signalId)
        {
            // Find direct connections from the signal to commands
            var directCommands = _graph.Edges
                .Where(e => e.SourceNodeId == signalId && _graph.GetNode(e.TargetNodeId)?.Type == NodeType.Command)
                .Select(e => _graph.GetNode(e.TargetNodeId))
                .Where(n => n != null);
            
            // Find commands through sequence groups
            var sequenceCommands = _graph.GetCommandsTriggeredBySignal(signalId);
            
            return directCommands.Concat(sequenceCommands).Distinct();
        }
    }
} 