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
    public class DiagramGroupView : Group
    {
        public DiagramNodeGroup GroupData { get; }
        private Label _titleLabel;
        
        public string Id;
        public string Name;
        public NodeType Type;
        public Color Color;
        public Rect Position;
        public List<string> NodeIds = new();
        
        // Enhanced properties for rendering
        public bool IsExpanded = true;
        public bool IsHighlighted = false;
        public bool IsSequenceGroup => GroupData?.IsSequenceGroup ?? false;
        public bool IsParallelGroup => GroupData?.IsParallelGroup ?? false;
        
        private const float TITLE_HEIGHT = 25f;
        private const float PADDING = 10f;
        private const float MIN_WIDTH = 250f;
        private const float MIN_HEIGHT = 100f;
        private const float BORDER_WIDTH = 2f;
        private const float BADGE_SIZE = 24f;
        
        private static readonly Color SELECTED_COLOR = new(1f, 1f, 0.6f, 0.7f);
        private static readonly Color SEQUENCE_BADGE_COLOR = new(0.8f, 0.2f, 0.2f, 1.0f);
        private static readonly Color PARALLEL_BADGE_COLOR = new(0.2f, 0.2f, 0.8f, 1.0f);
        
        public DiagramGroupView(DiagramNodeGroup groupData)
        {
            try
            {
                Debug.Log($"Creating group view for {groupData.Name} ({groupData.Id})");
                GroupData = groupData;
                
                title = groupData.Name;
                viewDataKey = groupData.Id;
                
                // Make the group visible
                visible = true;
                style.visibility = Visibility.Visible;
                style.display = DisplayStyle.Flex;
                style.opacity = 1;
                
                // Basic group style
                AddToClassList("diagram-group");
                AddToClassList($"{groupData.Type.ToString().ToLower()}-group");
                
                // Set group position
                SetPosition(new Rect(groupData.Position.x, groupData.Position.y, 
                    groupData.Position.width, groupData.Position.height));
                
                // Background color
                Color backgroundColor = groupData.Color;
                backgroundColor.a = 0.2f;
                style.backgroundColor = backgroundColor;
                
                // Border color
                Color borderColor = groupData.Color;
                borderColor.a = 0.8f;
                style.borderBottomColor = borderColor;
                style.borderLeftColor = borderColor;
                style.borderRightColor = borderColor;
                style.borderTopColor = borderColor;
                
                // Border width
                style.borderBottomWidth = 3;
                style.borderLeftWidth = 3;
                style.borderRightWidth = 3;
                style.borderTopWidth = 3;
                
                // Border radius
                style.borderBottomLeftRadius = 12;
                style.borderBottomRightRadius = 12;
                style.borderTopLeftRadius = 12;
                style.borderTopRightRadius = 12;
                
                // Internal padding
                style.paddingBottom = 40;
                style.paddingLeft = 40;
                style.paddingRight = 40;
                style.paddingTop = 50;
                
                // External margin
                style.marginBottom = 15;
                style.marginLeft = 15;
                style.marginRight = 15;
                style.marginTop = 15;
                
                // Title style
                _titleLabel = headerContainer.Q<Label>();
                if (_titleLabel != null)
                {
                    _titleLabel.style.fontSize = 18;
                    _titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                    _titleLabel.style.color = new Color(1f, 1f, 1f, 0.95f);
                    
                    // Title background color
                    var titleContainer = _titleLabel.parent;
                    if (titleContainer != null)
                    {
                        titleContainer.style.backgroundColor = new Color(
                            borderColor.r, 
                            borderColor.g, 
                            borderColor.b, 
                            0.95f);
                        titleContainer.style.paddingLeft = 15;
                        titleContainer.style.paddingRight = 15;
                        titleContainer.style.paddingTop = 8;
                        titleContainer.style.paddingBottom = 8;
                        titleContainer.style.borderTopLeftRadius = 8;
                        titleContainer.style.borderTopRightRadius = 8;
                        titleContainer.style.borderBottomLeftRadius = 8;
                        titleContainer.style.borderBottomRightRadius = 8;
                        titleContainer.style.marginBottom = 10;
                        
                        // Make title visible
                        titleContainer.visible = true;
                        titleContainer.style.visibility = Visibility.Visible;
                        titleContainer.style.display = DisplayStyle.Flex;
                        titleContainer.style.opacity = 1;
                    }
                    
                    // Make title visible
                    _titleLabel.visible = true;
                    _titleLabel.style.visibility = Visibility.Visible;
                    _titleLabel.style.display = DisplayStyle.Flex;
                    _titleLabel.style.opacity = 1;
                }
                
                // Make content container visible
                contentContainer.visible = true;
                contentContainer.style.visibility = Visibility.Visible;
                contentContainer.style.display = DisplayStyle.Flex;
                contentContainer.style.opacity = 1;
                
                // Enable containment - Critical for nodes to be contained
                capabilities |= Capabilities.Groupable;
                
                // Track geometry changes
                RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
                
                Debug.Log($"Group view created for {groupData.Name}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error creating group view for {groupData?.Name}: {ex.Message}\n{ex.StackTrace}");
            }
        }
        
        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            try
            {
                // Grup boyutu değiştiğinde model verisini güncelle
                if (GroupData != null)
                {
                    GroupData.Position = new Rect(layout.x, layout.y, layout.width, layout.height);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Error handling geometry change in group {GroupData?.Name}: {ex.Message}");
            }
        }
        
        public override void SetPosition(Rect newPos)
        {
            try
            {
                base.SetPosition(newPos);
                
                // Modeli güncelle
                if (GroupData != null)
                {
                    GroupData.Position = newPos;
                }
                
                // Görünürlüğü garantile
                visible = true;
                style.visibility = Visibility.Visible;
                style.display = DisplayStyle.Flex;
                style.opacity = 1;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Error setting position for group {GroupData?.Name}: {ex.Message}");
            }
        }
        
        // Yeni metod: Grubun sınırlarını içerisindeki düğümlere göre düzenle
        public void ResizeToFitContent()
        {
            try
            {
                if (containedElements == null || !containedElements.Any())
                {
                    return;
                }
                
                // Minimum ve maksimum kordinatları belirle
                float minX = float.MaxValue;
                float minY = float.MaxValue;
                float maxX = float.MinValue;
                float maxY = float.MinValue;
                
                // Düğüm sayısı ve boyutlarını belirle
                int nodeCount = 0;
                
                foreach (var element in containedElements)
                {
                    if (element is Node node)
                    {
                        nodeCount++;
                        var pos = node.GetPosition();
                        minX = Mathf.Min(minX, pos.x);
                        minY = Mathf.Min(minY, pos.y);
                        maxX = Mathf.Max(maxX, pos.x + pos.width);
                        maxY = Mathf.Max(maxY, pos.y + pos.height);
                    }
                }
                
                // Sınırlar geçerliyse grubu yeniden boyutlandır
                if (minX < maxX && minY < maxY)
                {
                    // Dinamik olarak kenar boşluğunu hesapla
                    // Daha fazla düğüm varsa daha fazla boşluk bırak
                    float horizontalPadding = Mathf.Max(40, 20 + nodeCount * 2);
                    float verticalPadding = Mathf.Max(40, 20 + nodeCount * 1.5f);
                    
                    // Mevcut konumu koru
                    var currentPos = GetPosition();
                    
                    // Yeni boyut - düğüm sayısına göre ölçekle
                    float width = maxX - minX + horizontalPadding * 2;
                    float height = maxY - minY + verticalPadding * 2;
                    
                    // Minimum boyut kısıtlaması
                    width = Mathf.Max(width, 300);
                    height = Mathf.Max(height, 200);
                    
                    // Pozisyonu güncelle
                    SetPosition(new Rect(currentPos.x, currentPos.y, width, height));
                    
                    // Görünürlüğü garantile
                    visible = true;
                    style.visibility = Visibility.Visible;
                    style.display = DisplayStyle.Flex;
                    style.opacity = 1;
                    
                    // Grup data modelini güncelle
                    if (GroupData != null)
                    {
                        GroupData.Position = new Rect(currentPos.x, currentPos.y, width, height);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Error resizing group {GroupData?.Name}: {ex.Message}");
            }
        }
        
        public void Draw(DiagramView diagramView, bool isSelected)
        {
            Color groupColor = Color;
            
            // Adjust color for highlighting
            if (IsHighlighted)
                groupColor = Color.Lerp(groupColor, Color.white, 0.3f);
            
            // Draw background with shadow effect
            Rect shadowRect = new Rect(Position.x + 4, Position.y + 4, Position.width, Position.height);
            EditorGUI.DrawRect(shadowRect, new Color(0, 0, 0, 0.2f));
            
            // Draw group background
            EditorGUI.DrawRect(Position, groupColor);
            
            // Draw selection outline if selected
            if (isSelected)
            {
                Color selectionColor = SELECTED_COLOR;
                Rect outlineRect = new Rect(
                    Position.x - BORDER_WIDTH, 
                    Position.y - BORDER_WIDTH, 
                    Position.width + (BORDER_WIDTH * 2), 
                    Position.height + (BORDER_WIDTH * 2)
                );
                
                DrawRectOutline(outlineRect, selectionColor, BORDER_WIDTH);
            }
            
            // Draw title bar
            Rect titleBarRect = new Rect(Position.x, Position.y, Position.width, TITLE_HEIGHT);
            EditorGUI.DrawRect(titleBarRect, new Color(groupColor.r * 0.8f, groupColor.g * 0.8f, groupColor.b * 0.8f, 1f));
            
            // Draw title text
            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel);
            titleStyle.normal.textColor = GetTextColorForBackground(groupColor);
            titleStyle.alignment = TextAnchor.MiddleLeft;
            
            Rect titleTextRect = new Rect(
                titleBarRect.x + PADDING,
                titleBarRect.y,
                titleBarRect.width - (PADDING * 2) - (IsSequenceGroup || IsParallelGroup ? BADGE_SIZE : 0),
                titleBarRect.height
            );
            
            string displayName = GetFormattedName();
            EditorGUI.LabelField(titleTextRect, displayName, titleStyle);
            
            // Draw special sequence/parallel badge if applicable
            if (IsSequenceGroup || IsParallelGroup)
            {
                Rect badgeRect = new Rect(
                    titleBarRect.xMax - BADGE_SIZE - PADDING, 
                    titleBarRect.y + ((titleBarRect.height - BADGE_SIZE) / 2),
                    BADGE_SIZE, 
                    BADGE_SIZE
                );
                
                Color badgeColor = IsSequenceGroup ? SEQUENCE_BADGE_COLOR : PARALLEL_BADGE_COLOR;
                EditorGUI.DrawRect(badgeRect, badgeColor);
                
                GUIStyle badgeStyle = new GUIStyle(EditorStyles.boldLabel);
                badgeStyle.normal.textColor = Color.white;
                badgeStyle.alignment = TextAnchor.MiddleCenter;
                badgeStyle.fontSize = 10;
                
                string badgeText = IsSequenceGroup ? "SEQ" : "PAR";
                EditorGUI.LabelField(badgeRect, badgeText, badgeStyle);
            }
            
            // Draw group content area border
            Rect contentBorderRect = new Rect(
                Position.x, 
                Position.y + TITLE_HEIGHT, 
                Position.width, 
                Position.height - TITLE_HEIGHT
            );
            
            // Draw divider line between title and content
            EditorGUI.DrawRect(
                new Rect(contentBorderRect.x, contentBorderRect.y, contentBorderRect.width, 1), 
                new Color(0.3f, 0.3f, 0.3f, 0.5f)
            );
            
            // Draw sequence numbers for command nodes if this is a sequence group
            if (IsSequenceGroup && diagramView != null)
            {
                var nodes = NodeIds
                    .Select(diagramView.GetNodeView)
                    .Where(n => n != null)
                    .OrderBy(n => n.NodeData.ExecutionOrder)
                    .ToList();
                
                for (int i = 0; i < nodes.Count; i++)
                {
                    var node = nodes[i];
                    DrawSequenceNumberBadge(node, i + 1);
                }
                
                // Draw sequence arrows
                for (int i = 0; i < nodes.Count - 1; i++)
                {
                    var sourceNode = nodes[i];
                    var targetNode = nodes[i + 1];
                    DrawSequenceArrow(sourceNode, targetNode);
                }
            }
        }
        
        private string GetFormattedName()
        {
            if (GroupData == null) return Name;
            
            string displayName = Name;
            
            if (IsSequenceGroup)
            {
                // Get trigger signal name if available
                if (!string.IsNullOrEmpty(GroupData.TriggerSignalId))
                {
                    string signalName = GroupData.GetMetadata<string>("SignalName", "");
                    if (!string.IsNullOrEmpty(signalName))
                    {
                        displayName = $"Sequence: {signalName}";
                    }
                }
            }
            else if (IsParallelGroup)
            {
                // Get trigger signal name if available
                if (!string.IsNullOrEmpty(GroupData.TriggerSignalId))
                {
                    string signalName = GroupData.GetMetadata<string>("SignalName", "");
                    if (!string.IsNullOrEmpty(signalName))
                    {
                        displayName = $"Parallel: {signalName}";
                    }
                }
            }
            
            return displayName;
        }
        
        private void DrawSequenceNumberBadge(EnhancedDiagramNodeView nodeView, int sequenceNumber)
        {
            if (nodeView == null) return;
            
            const float BADGE_SIZE = 20f;
            
            // Draw at the top-left corner of the node
            Rect badgeRect = new Rect(
                GetNodePosition(nodeView).x - BADGE_SIZE / 2f,
                GetNodePosition(nodeView).y - BADGE_SIZE / 2f,
                BADGE_SIZE,
                BADGE_SIZE
            );
            
            Color badgeColor = SEQUENCE_BADGE_COLOR;
            EditorGUI.DrawRect(badgeRect, badgeColor);
            
            GUIStyle numberStyle = new GUIStyle(EditorStyles.boldLabel);
            numberStyle.normal.textColor = Color.white;
            numberStyle.alignment = TextAnchor.MiddleCenter;
            numberStyle.fontSize = 10;
            
            EditorGUI.LabelField(badgeRect, sequenceNumber.ToString(), numberStyle);
        }
        
        private void DrawSequenceArrow(EnhancedDiagramNodeView sourceNode, EnhancedDiagramNodeView targetNode)
        {
            if (sourceNode == null || targetNode == null) return;
            
            var sourceRect = sourceNode.GetPosition();
            var targetRect = targetNode.GetPosition();
            
            Vector2 start = new Vector2(
                sourceRect.x + sourceRect.width,
                sourceRect.y + (sourceRect.height / 2)
            );
            
            Vector2 end = new Vector2(
                targetRect.x,
                targetRect.y + (targetRect.height / 2)
            );
            
            Color arrowColor = SEQUENCE_BADGE_COLOR;
            DrawArrow(start, end, arrowColor, 2f, 10f);
        }
        
        private void DrawArrow(Vector2 start, Vector2 end, Color color, float width, float arrowSize)
        {
            Handles.BeginGUI();
            Handles.color = color;
            
            // Draw line
            Handles.DrawAAPolyLine(width, new Vector3(start.x, start.y, 0), new Vector3(end.x, end.y, 0));
            
            // Calculate arrow head
            Vector2 direction = (end - start).normalized;
            Vector2 perpendicular = new Vector2(-direction.y, direction.x);
            
            Vector3[] arrowHead = new Vector3[3] {
                new(end.x, end.y, 0),
                new(end.x - (direction.x * arrowSize) + (perpendicular.x * arrowSize * 0.5f), 
                           end.y - (direction.y * arrowSize) + (perpendicular.y * arrowSize * 0.5f), 0),
                new(end.x - (direction.x * arrowSize) - (perpendicular.x * arrowSize * 0.5f), 
                           end.y - (direction.y * arrowSize) - (perpendicular.y * arrowSize * 0.5f), 0)
            };
            
            // Draw arrow head
            Handles.DrawAAConvexPolygon(arrowHead);
            
            Handles.EndGUI();
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
        
        private Color GetTextColorForBackground(Color backgroundColor)
        {
            // Calculate brightness using weighted RGB values
            float brightness = (backgroundColor.r * 0.299f) + (backgroundColor.g * 0.587f) + (backgroundColor.b * 0.114f);
            
            return brightness > 0.5f ? Color.black : Color.white;
        }
        
        public bool Contains(Vector2 point)
        {
            return Position.Contains(point);
        }
        
        public void UpdatePosition(Rect newPosition)
        {
            Position = newPosition;
            GroupData.Position = newPosition;
        }
        
        public void RepositionNodes(DiagramView diagramView)
        {
            try
            {
                Debug.Log($"Repositioning nodes in group {GroupData.Name}");
                
                // Get all nodes in this group
                var groupNodes = GroupData.NodeIds
                    .Select(id => diagramView.GetNodeView(id))
                    .Where(n => n != null)
                    .ToList();
                
                if (groupNodes.Count == 0)
                {
                    Debug.Log($"No nodes found in group {GroupData.Name}");
                    return;
                }
                
                // Get current position
                Rect groupRect = GetPosition();
                
                if (IsSequenceGroup)
                {
                    // For sequence groups, arrange nodes in a vertical sequence
                    ArrangeSequenceNodes(groupNodes, groupRect);
                }
                else if (IsParallelGroup)
                {
                    // For parallel groups, arrange nodes in a horizontal row
                    ArrangeParallelNodes(groupNodes, groupRect);
                }
                else
                {
                    // For regular groups, arrange in a grid
                    ArrangeGridNodes(groupNodes, groupRect);
                }
                
                // Ensure all nodes are visible
                foreach (var node in groupNodes)
                {
                    node.visible = true;
                    node.style.visibility = Visibility.Visible;
                    node.style.display = DisplayStyle.Flex;
                    node.style.opacity = 1;
                    
                    foreach (var container in new[] { 
                        node.titleContainer, 
                        node.inputContainer, 
                        node.outputContainer, 
                        node.extensionContainer,
                        node.mainContainer 
                    })
                    {
                        if (container != null)
                        {
                            container.visible = true;
                            container.style.visibility = Visibility.Visible;
                            container.style.display = DisplayStyle.Flex;
                            container.style.opacity = 1;
                        }
                    }
                    
                    foreach (var port in node._inputPorts.Concat(node._outputPorts))
                    {
                        if (port != null)
                        {
                            port.visible = true;
                            port.style.visibility = Visibility.Visible;
                            port.style.display = DisplayStyle.Flex;
                            port.style.opacity = 1;
                        }
                    }
                    
                    node.BringToFront();
                }
                
                // Resize group to fit contents
                ResizeToFitContent();
                
                // Make sure group is visible
                visible = true;
                style.visibility = Visibility.Visible;
                style.display = DisplayStyle.Flex;
                style.opacity = 1;
                
                if (contentContainer != null)
                {
                    contentContainer.visible = true;
                    contentContainer.style.visibility = Visibility.Visible;
                    contentContainer.style.display = DisplayStyle.Flex;
                    contentContainer.style.opacity = 1;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error in RepositionNodes for group {GroupData?.Name}: {ex.Message}\n{ex.StackTrace}");
            }
        }
        
        private void ArrangeSequenceNodes(List<EnhancedDiagramNodeView> nodes, Rect groupRect)
        {
            try
            {
                Debug.Log($"Arranging {nodes.Count} nodes in sequence group {GroupData.Name}");
                
                // Sort nodes by execution order
                var orderedNodes = nodes.OrderBy(n => n.NodeData.ExecutionOrder).ToList();
                
                if (orderedNodes.Count == 0) return;
                
                float nodeWidth = 180;
                float nodeHeight = 120;
                float verticalSpacing = 120; // Increased from 80 for better spacing
                
                float padding = 60; // Increased from 40
                float startX = groupRect.x + padding;
                float startY = groupRect.y + 90; // Increased from 70 for more space for title
                
                // Use these values to center nodes horizontally
                float centerX = startX + ((groupRect.width - 2 * padding) / 2) - (nodeWidth / 2);
                
                // Ensure node execution order aligns with position in the sequence
                for (int i = 0; i < orderedNodes.Count; i++)
                {
                    var node = orderedNodes[i];
                    node.NodeData.ExecutionOrder = i + 1;
                    
                    // Position the node
                    float posY = startY + (i * (nodeHeight + verticalSpacing));
                    node.SetPosition(new Rect(centerX, posY, nodeWidth, nodeHeight));
                    
                    // Add sequence indicator
                    var label = $"Step {i + 1}";
                    node.NodeData.AddMetadata("SequenceInfo", label);
                    
                    // Add previous/next step info for visualization
                    if (i > 0)
                        node.NodeData.AddMetadata("PreviousStep", orderedNodes[i-1].NodeData.Name);
                    if (i < orderedNodes.Count - 1)
                        node.NodeData.AddMetadata("NextStep", orderedNodes[i+1].NodeData.Name);
                    
                    // Ensure node is visible
                    node.visible = true;
                    node.style.visibility = Visibility.Visible;
                    node.style.display = DisplayStyle.Flex;
                    node.style.opacity = 1;
                    node.BringToFront();
                }
                
                // Recalculate group size to accommodate all nodes
                float requiredWidth = Math.Max(nodeWidth + (padding * 3), 350);
                float requiredHeight = (orderedNodes.Count * nodeHeight) + 
                                      ((orderedNodes.Count - 1) * verticalSpacing) + 
                                      (padding * 2) + 60; // Extra padding for title and footer
                
                // Set group position preserving the original top-left position
                SetPosition(new Rect(groupRect.x, groupRect.y, requiredWidth, requiredHeight));
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error arranging sequence nodes in group {GroupData?.Name}: {ex.Message}\n{ex.StackTrace}");
            }
        }
        
        private void ArrangeParallelNodes(List<EnhancedDiagramNodeView> nodes, Rect groupRect)
        {
            try
            {
                if (nodes == null || nodes.Count == 0) return;
                
                // Fixed layout parameters
                float nodeWidth = 200;
                float nodeHeight = 120;
                float horizontalSpacing = 60; // Increased from 30
                float verticalSpacing = 60; // Increased from 30
                float padding = 50; // Increased from 30
                
                // Fixed layout with a maximum of 3 columns for better readability
                int columns = Math.Min(nodes.Count, 3);
                int rows = (int)Math.Ceiling(nodes.Count / (float)columns);
                
                float startX = groupRect.x + padding;
                float startY = groupRect.y + 50; // Extra space for title
                
                // Sort nodes by name for consistent layout
                var orderedNodes = nodes.OrderBy(n => n.NodeData.Name).ToList();
                
                // Position nodes in grid
                for (int i = 0; i < orderedNodes.Count; i++)
                {
                    int row = i / columns;
                    int col = i % columns;
                    
                    var node = orderedNodes[i];
                    
                    // Calculate position within the group
                    float posX = startX + (col * (nodeWidth + horizontalSpacing));
                    float posY = startY + (row * (nodeHeight + verticalSpacing));
                    
                    node.SetPosition(new Rect(posX, posY, nodeWidth, nodeHeight));
                    
                    // Add visual indicator for parallel execution
                    node.NodeData.AddMetadata("ParallelInfo", "Parallel");
                    
                    // Ensure node is visible
                    node.visible = true;
                    node.style.visibility = Visibility.Visible;
                    node.style.display = DisplayStyle.Flex;
                    node.style.opacity = 1;
                    node.BringToFront();
                    
                    // Make sure ports are visible
                    foreach (var port in node._inputPorts.Concat(node._outputPorts))
                    {
                        port.visible = true;
                        port.style.visibility = Visibility.Visible;
                        port.style.display = DisplayStyle.Flex;
                    }
                }
                
                // Recalculate group size to accommodate all nodes
                float requiredWidth = (columns * nodeWidth) + 
                                     ((columns - 1) * horizontalSpacing) + 
                                     (padding * 2);
                
                float requiredHeight = (rows * nodeHeight) + 
                                      ((rows - 1) * verticalSpacing) + 
                                      (padding * 2) + 30; // Extra for title
                
                // Set group position preserving the original top-left position
                SetPosition(new Rect(groupRect.x, groupRect.y, 
                           Math.Max(requiredWidth, 350), 
                           Math.Max(requiredHeight, 250)));
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error arranging parallel nodes in group {GroupData?.Name}: {ex.Message}\n{ex.StackTrace}");
            }
        }
        
        private void ArrangeGridNodes(List<EnhancedDiagramNodeView> nodes, Rect groupRect)
        {
            try
            {
                float nodeWidth = 180;
                float nodeHeight = 120;
                float horizontalSpacing = 80; // Increased from 40
                float verticalSpacing = 80; // Increased from 40
                float padding = 70; // Increased from 50
                
                // First group and sort nodes by type for better organization
                var nodesByType = nodes.GroupBy(n => n.NodeData.Type)
                                     .OrderBy(g => g.Key)
                                     .ToDictionary(g => g.Key, g => g.ToList());
                
                // Calculate optimal column count based on node count
                int totalNodes = nodes.Count;
                int columns = totalNodes <= 2 ? totalNodes : 
                             totalNodes <= 6 ? 2 : 
                             Math.Min(3, (int)Math.Ceiling(Math.Sqrt(totalNodes)));
                
                float startX = groupRect.x + padding;
                float startY = groupRect.y + padding;
                float currentX = startX;
                float currentY = startY;
                int colCount = 0;
                
                // Process each type of nodes separately
                foreach (var type in nodesByType.Keys)
                {
                    var nodesOfType = nodesByType[type].OrderBy(n => n.NodeData.Name).ToList();
                    
                    // Add a small vertical gap between different node types
                    if (currentX != startX || currentY != startY)
                    {
                        currentY += 40; // Increased from 20
                    }
                    
                    foreach (var node in nodesOfType)
                    {
                        node.SetPosition(new Rect(currentX, currentY, nodeWidth, nodeHeight));
                        
                        // Move to next position
                        colCount++;
                        currentX += nodeWidth + horizontalSpacing;
                        
                        // Check if we need to wrap to next row
                        if (colCount >= columns)
                        {
                            colCount = 0;
                            currentX = startX;
                            currentY += nodeHeight + verticalSpacing;
                        }
                    }
                    
                    // If we're in the middle of a row, move to the next row before starting a new type
                    if (colCount > 0)
                    {
                        colCount = 0;
                        currentX = startX;
                        currentY += nodeHeight + verticalSpacing;
                    }
                }
                
                // Calculate required size based on node positions
                float maxX = startX;
                float maxY = startY;
                
                foreach (var node in nodes)
                {
                    var rect = node.GetPosition();
                    maxX = Math.Max(maxX, rect.x + rect.width);
                    maxY = Math.Max(maxY, rect.y + rect.height);
                }
                
                float requiredWidth = Math.Max(groupRect.width, maxX - groupRect.x + padding);
                float requiredHeight = Math.Max(groupRect.height, maxY - groupRect.y + padding);
                
                // Update group size if needed
                if (requiredWidth > groupRect.width || requiredHeight > groupRect.height)
                {
                    SetPosition(new Rect(groupRect.x, groupRect.y, requiredWidth, requiredHeight));
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error arranging grid nodes: {ex.Message}");
            }
        }
        
        // Helper method to get node positions
        private Vector2 GetNodePosition(EnhancedDiagramNodeView nodeView)
        {
            if (nodeView == null) return Vector2.zero;
            
            return new Vector2(
                nodeView.GetPosition().x,
                nodeView.GetPosition().y
            );
        }
        
        public new void AddElement(GraphElement element)
        {
            try
            {
                // Check if the element is already contained in this group
                if (containedElements.Contains(element))
                {
                    Debug.Log($"Element {element.GetType().Name} is already in group {title}, skipping AddElement");
                    return;
                }
                
                // Call the base implementation
                base.AddElement(element);
                
                // Make sure the element is visible
                element.visible = true;
                element.style.visibility = Visibility.Visible;
                element.style.display = DisplayStyle.Flex;
                element.style.opacity = 1;
                
                // Update NodeIds list if this is a node
                if (element is EnhancedDiagramNodeView nodeView && !NodeIds.Contains(nodeView.NodeData.Id))
                {
                    NodeIds.Add(nodeView.NodeData.Id);
                    
                    // Also update the GroupData
                    if (GroupData != null && !GroupData.NodeIds.Contains(nodeView.NodeData.Id))
                    {
                        GroupData.NodeIds.Add(nodeView.NodeData.Id);
                    }
                }
                
                Debug.Log($"Added element {element.GetType().Name} to group {title}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error adding element to group {title}: {ex.Message}");
            }
        }
    }
}
#endif 