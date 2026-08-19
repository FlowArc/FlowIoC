#if UNITY_2019_4_OR_NEWER
using System;
using System.Collections.Generic;
using System.Linq;
using FlowIoC.Editor.DependencyDiagram.Data;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace FlowIoC.Editor.DependencyDiagram.View
{
    public class DiagramNodeView : Node
    {
        public DiagramNode NodeData { get; private set; }
        public Action<DiagramNodeView> OnNodeSelected;
        
        private readonly Label _titleLabel;
        private readonly Label _typeLabel;
        private readonly VisualElement _headerContainer;
        
        // Port listelerini public yapıyorum
        public readonly List<Port> _inputPorts = new List<Port>();
        public readonly List<Port> _outputPorts = new List<Port>();
        
        public DiagramNodeView(DiagramNode nodeData)
        {
            try
            {
                Debug.Log($"Creating node view for {nodeData.Name} ({nodeData.Type})");
                NodeData = nodeData;
                
                title = nodeData.Name;
                userData = nodeData;
                viewDataKey = nodeData.Id;
                
                // Temel node stilini ayarla
                AddToClassList("diagram-node");
                AddToClassList($"{NodeData.Type.ToString().ToLower()}-node");
                
                // Style the node header
                _headerContainer = new VisualElement();
                _headerContainer.AddToClassList("diagram-node-header");
                _headerContainer.style.backgroundColor = GetNodeColor(nodeData.Type);
                _headerContainer.style.paddingTop = 8;
                _headerContainer.style.paddingBottom = 8;
                _headerContainer.style.paddingLeft = 10;
                _headerContainer.style.paddingRight = 10;
                _headerContainer.style.borderTopLeftRadius = 5;
                _headerContainer.style.borderTopRightRadius = 5;
                
                // Set a minimum size for the node
                style.minWidth = 150;
                style.minHeight = 80;
                style.width = 180;
                style.height = 100;
                
                // Belirli bir renk şeması ayarla
                style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.95f);
                style.borderLeftColor = GetNodeColor(NodeData.Type);
                style.borderRightColor = GetNodeColor(NodeData.Type);
                style.borderTopColor = GetNodeColor(NodeData.Type);
                style.borderBottomColor = GetNodeColor(NodeData.Type);
                style.borderLeftWidth = 2;
                style.borderRightWidth = 2;
                style.borderTopWidth = 2;
                style.borderBottomWidth = 2;
                style.borderTopLeftRadius = 5;
                style.borderTopRightRadius = 5;
                style.borderBottomLeftRadius = 5;
                style.borderBottomRightRadius = 5;
                
                // Başlık etiketi
                _titleLabel = new Label(nodeData.Name);
                _titleLabel.style.fontSize = 14;
                _titleLabel.style.color = Color.white;
                _titleLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                _titleLabel.style.overflow = Overflow.Hidden;
                _titleLabel.style.textOverflow = TextOverflow.Ellipsis;
                _titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                
                _headerContainer.Add(_titleLabel);
                
                mainContainer.Insert(0, _headerContainer);
                
                // Type label
                _typeLabel = new Label(nodeData.TypeName);
                _typeLabel.AddToClassList("diagram-node-type");
                _typeLabel.style.fontSize = 12;
                _typeLabel.style.color = new Color(0.9f, 0.9f, 0.9f);
                _typeLabel.style.paddingTop = 5;
                _typeLabel.style.paddingBottom = 5;
                _typeLabel.style.paddingLeft = 10;
                _typeLabel.style.paddingRight = 10;
                _typeLabel.style.overflow = Overflow.Hidden;
                _typeLabel.style.textOverflow = TextOverflow.Ellipsis;
                _typeLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                _typeLabel.style.backgroundColor = new Color(0.15f, 0.15f, 0.15f, 0.8f);
                
                extensionContainer.Add(_typeLabel);
                RefreshExpandedState();
                
                // Port konteynırlarını temizle
                inputContainer.Clear();
                outputContainer.Clear();
                
                // Input port ekle
                var inputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(bool));
                if (inputPort != null)
                {
                    Debug.Log($"Creating input port for {nodeData.Name}");
                    inputPort.portName = "In";
                    inputPort.viewDataKey = $"{NodeData.Id}_input";
                    
                    // Port stilini ayarla
                    inputPort.style.flexDirection = FlexDirection.Row;
                    inputPort.style.alignItems = Align.Center;
                    inputPort.style.justifyContent = Justify.FlexStart;
                    
                    // Görünürlüğü garantile
                    inputPort.visible = true;
                    inputPort.style.visibility = Visibility.Visible;
                    inputPort.style.display = DisplayStyle.Flex;
                    inputPort.style.opacity = 1;
                    
                    // Port etiketini özelleştir
                    var inputLabel = inputPort.Q<Label>("type");
                    if (inputLabel != null)
                    {
                        inputLabel.style.color = new Color(0.7f, 0.7f, 0.7f);
                        inputLabel.style.fontSize = 10;
                    }
                    
                    // Port düğmesini özelleştir
                    var inputConnector = inputPort.Q("connector");
                    if (inputConnector != null)
                    {
                        inputConnector.style.width = 12;
                        inputConnector.style.height = 12;
                        inputConnector.style.backgroundColor = GetNodeColor(NodeData.Type);
                    }
                    
                    inputContainer.Add(inputPort);
                    _inputPorts.Add(inputPort);
                }
                
                // Output port ekle
                var outputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(bool));
                if (outputPort != null)
                {
                    Debug.Log($"Creating output port for {nodeData.Name}");
                    outputPort.portName = "Out";
                    outputPort.viewDataKey = $"{NodeData.Id}_output";
                    
                    // Port stilini ayarla
                    outputPort.style.flexDirection = FlexDirection.Row;
                    outputPort.style.alignItems = Align.Center;
                    outputPort.style.justifyContent = Justify.FlexEnd;
                    
                    // Görünürlüğü garantile
                    outputPort.visible = true;
                    outputPort.style.visibility = Visibility.Visible;
                    outputPort.style.display = DisplayStyle.Flex;
                    outputPort.style.opacity = 1;
                    
                    // Port etiketini özelleştir
                    var outputLabel = outputPort.Q<Label>("type");
                    if (outputLabel != null)
                    {
                        outputLabel.style.color = new Color(0.7f, 0.7f, 0.7f);
                        outputLabel.style.fontSize = 10;
                    }
                    
                    // Port düğmesini özelleştir
                    var outputConnector = outputPort.Q("connector");
                    if (outputConnector != null)
                    {
                        outputConnector.style.width = 12;
                        outputConnector.style.height = 12;
                        outputConnector.style.backgroundColor = GetNodeColor(NodeData.Type);
                    }
                    
                    outputContainer.Add(outputPort);
                    _outputPorts.Add(outputPort);
                }
                
                // Portları hemen güncelle
                RefreshPorts();
                
                // Diğer stil ayarları
                style.marginBottom = 5;
                style.marginLeft = 5;
                style.marginRight = 5;
                style.marginTop = 5;
                
                // Görünürlüğü garantile
                visible = true;
                style.visibility = Visibility.Visible;
                style.display = DisplayStyle.Flex;
                style.opacity = 1;
                
                // Register events
                RegisterCallback<MouseDownEvent>(OnMouseDown);
                RegisterCallback<MouseUpEvent>(OnMouseUp);
                RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
                
                Debug.Log($"Node view created for {nodeData.Name}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error creating node view for {nodeData?.Name}: {ex.Message}\n{ex.StackTrace}");
            }
        }
        
        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            try
            {
                // Düğüm geometrisi değiştiğinde pozisyonu güncelle
                NodeData.Position = GetPosition();
                
                // Portları yenile
                RefreshPorts();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Error handling geometry change: {ex.Message}");
            }
        }
        
        private void OnMouseDown(MouseDownEvent evt)
        {
            try
            {
                // Select this node
                selected = true;
                
                // Let others know this node was selected
                OnNodeSelected?.Invoke(this);
                
                // Bring to front
                BringToFront();
                
                // Force repaint
                MarkDirtyRepaint();
                
                evt.StopPropagation();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Error in mouse down: {ex.Message}");
            }
        }
        
        private void OnMouseUp(MouseUpEvent evt)
        {
            try
            {
                evt.StopPropagation();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Error in mouse up: {ex.Message}");
            }
        }
        
        private Color GetNodeColor(NodeType type)
        {
            switch (type)
            {
                case NodeType.Context:
                    return new Color(0.2f, 0.4f, 0.6f);
                case NodeType.Signal:
                    return new Color(0.6f, 0.2f, 0.2f);
                case NodeType.Command:
                    return new Color(0.4f, 0.6f, 0.2f);
                case NodeType.View:
                    return new Color(0.2f, 0.6f, 0.6f);
                case NodeType.Mediator:
                    return new Color(0.6f, 0.2f, 0.6f);
                case NodeType.Injectable:
                    return new Color(0.6f, 0.4f, 0.2f);
                default:
                    return new Color(0.5f, 0.5f, 0.5f);
            }
        }
    }
}
#endif 