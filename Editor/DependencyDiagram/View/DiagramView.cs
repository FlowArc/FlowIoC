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
    public class DiagramView : GraphView
    {
        private readonly Dictionary<string, EnhancedDiagramNodeView> _nodeViews = new();
        private readonly Dictionary<string, DiagramEdgeView> _edgeViews = new();
        private readonly Dictionary<string, DiagramGroupView> _groupViews = new();
        
        private DiagramGraph _graph;
        private readonly HashSet<string> _signalToSequenceGroups = new();
        private readonly HashSet<string> _signalToParallelGroups = new();

        private DiagramGraph _graphData;
        public Action<EnhancedDiagramNodeView> OnNodeSelected { get; set; }

        private readonly DiagramViewType _viewType = DiagramViewType.CategoryView;
        
        private readonly Dictionary<NodeType, bool> _nodeTypeFilters = new();
        private string _searchFilter = string.Empty;
        
        public DiagramView()
        {
            try
            {
                styleSheets.Add(Resources.Load<StyleSheet>("DependencyDiagram/DiagramStyles"));
                Debug.Log("Loaded style sheet");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to load style sheet: {ex.Message}");
            }
            
            name = "Dependency Diagram Graph";
            
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale * 2);
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());
            
            var gridBackground = new GridBackground();
            gridBackground.StretchToParentSize();
            Insert(0, gridBackground);
            
            style.flexGrow = 1;
            style.flexShrink = 1;
            style.position = Position.Absolute;
            style.left = 0;
            style.top = 0;
            style.right = 0;
            style.bottom = 0;
            style.width = new StyleLength(StyleKeyword.Initial);
            style.height = new StyleLength(StyleKeyword.Initial);
            style.overflow = Overflow.Visible;
            
            style.display = DisplayStyle.Flex;
            style.opacity = 1;
            style.visibility = Visibility.Visible;
            
            _nodeTypeFilters[NodeType.Context] = true;
            _nodeTypeFilters[NodeType.Signal] = true;
            _nodeTypeFilters[NodeType.Command] = true;
            _nodeTypeFilters[NodeType.View] = true;
            _nodeTypeFilters[NodeType.Mediator] = true;
            _nodeTypeFilters[NodeType.Injectable] = true;
        }
        
        public void LoadGraph(DiagramGraph graph, DiagramViewType viewType)
        {
            try
            {
                _graph = graph;
                _graphData = graph;
                _graph.ViewType = viewType;
                
                Debug.Log($"Loading graph with {graph.Nodes.Count} nodes and {graph.Groups.Count} groups");
                
                // Clear the graph completely and force garbage collection
                ClearGraph();
                GC.Collect();
                
                // Step 1: First create all the nodes
                foreach (var node in _graph.Nodes)
                {
                    var nodeView = new EnhancedDiagramNodeView(node);
                    _nodeViews[node.Id] = nodeView;
                    
                    // We'll add nodes to groups later, don't add directly yet
                    // AddElement(nodeView);
                    
                    Debug.Log($"Created node: {node.Name}");
                }
                
                // Step 2: Create the groups
                foreach (var group in _graph.Groups.Values)
                {
                    var groupView = new DiagramGroupView(group);
                    _groupViews[group.Id] = groupView;
                    
                    if (group.IsSequenceGroup || group.IsParallelGroup)
                    {
                        ConfigureCommandSequenceGroup(group, groupView);
                    }
                    
                    if (group.IsSequenceGroup)
                    {
                        groupView.AddToClassList("sequence-group");
                        
                        if (!string.IsNullOrEmpty(group.TriggerSignalId))
                        {
                            _signalToSequenceGroups.Add(group.TriggerSignalId);
                        }
                    }
                    else if (group.IsParallelGroup)
                    {
                        groupView.AddToClassList("parallel-group");
                        
                        if (!string.IsNullOrEmpty(group.TriggerSignalId))
                        {
                            _signalToParallelGroups.Add(group.TriggerSignalId);
                        }
                    }
                    
                    // Add the group to the graph
                    AddElement(groupView);
                    
                    // Ensure the group is visible
                    groupView.visible = true;
                    groupView.style.visibility = Visibility.Visible;
                    groupView.style.display = DisplayStyle.Flex;
                    groupView.style.opacity = 1;
                    
                    Debug.Log($"Created group: {group.Name}");
                }
                
                // Step 3: Assign nodes to their groups
                AssignNodesToGroups();
                
                // Step 4: Create the edges
                foreach (var edge in _graph.Edges)
                {
                    try
                    {
                        if (!_nodeViews.ContainsKey(edge.SourceNodeId) || !_nodeViews.ContainsKey(edge.TargetNodeId))
                        {
                            Debug.LogWarning($"Skipping edge {edge.Id}: Source or target node not found");
                            continue;
                        }
                        
                        var sourceNode = _nodeViews[edge.SourceNodeId];
                        var targetNode = _nodeViews[edge.TargetNodeId];
                        
                        // Skip invalid edges
                        if (sourceNode == null || targetNode == null)
                        {
                            Debug.LogWarning($"Skipping edge {edge.Id}: Source or target node is null");
                            continue;
                        }
                        
                        // Skip if source or target doesn't have ports
                        if (sourceNode._outputPorts == null || sourceNode._outputPorts.Count == 0 ||
                            targetNode._inputPorts == null || targetNode._inputPorts.Count == 0)
                        {
                            Debug.LogWarning($"Skipping edge {edge.Id}: Source or target node has no ports");
                            continue;
                        }
                        
                        var edgeView = new DiagramEdgeView(edge, sourceNode, targetNode, edge.Type.ToString());
                        _edgeViews[edge.Id] = edgeView;
                        
                        // Add appropriate CSS class
                        if (edge.Type == EdgeType.SequentialCommand)
                        {
                            edgeView.AddToClassList("sequence-edge");
                        }
                        else if (edge.Type == EdgeType.ParallelCommand)
                        {
                            edgeView.AddToClassList("parallel-edge");
                        }
                        
                        // Add the edge to the graph after it's fully initialized
                        AddElement(edgeView);
                        
                        // Ensure the edge is visible
                        edgeView.visible = true;
                        edgeView.style.visibility = Visibility.Visible;
                        edgeView.style.display = DisplayStyle.Flex;
                        edgeView.style.opacity = 1;
                        
                        Debug.Log($"Created edge from {sourceNode.NodeData.Name} to {targetNode.NodeData.Name}");
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Error creating edge {edge.Id}: {ex.Message}");
                    }
                }
                
                // Step 5: Perform the layout
                Debug.Log("Starting layout process");
                PerformBasicLayout();
                
                // Step 6: Frame all elements
                EditorApplication.delayCall += FrameAll;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error in LoadGraph: {ex.Message}\n{ex.StackTrace}");
            }
        }
        
        // Simple method to perform basic layout without complex logic
        private void PerformBasicLayout()
        {
            try
            {
                Debug.Log("Performing basic layout");
                
                // Increased spacing between groups for better clarity
                int groupsPerRow = 3; // Reduced from 3 to 2 for better horizontal spacing
                float groupWidth = 550;   // Increased size
                float groupHeight = 450;  // Increased size
                float groupSpacing = 400; // Significantly increased from 250
                float startX = 250;       // Increased from 150
                float startY = 250;       // Increased from 150
                
                // Order groups by type with a more logical flow based on MVC architecture
                var groupOrder = new List<NodeType> {
                    NodeType.Context,  // Start with context
                    NodeType.Signal,   // Signals come next
                    NodeType.Command,  // Then commands that handle signals
                    NodeType.Mediator, // Mediators update models and views
                    NodeType.View,     // Views receive updates
                    NodeType.Injectable // Support classes last
                };
                
                var orderedGroups = _groupViews.Values
                    .OrderBy(g => {
                        int index = groupOrder.IndexOf(g.GroupData.Type);
                        return index >= 0 ? index : groupOrder.Count;
                    })
                    .ToList();
                
                // First pass: position the main groups in a logical order (left-to-right, top-to-bottom)
                for (int i = 0; i < orderedGroups.Count; i++)
                {
                    int row = i / groupsPerRow;
                    int col = i % groupsPerRow;
                    
                    float x = startX + col * (groupWidth + groupSpacing);
                    float y = startY + row * (groupHeight + groupSpacing);
                    
                    var groupView = orderedGroups[i];
                    
                    // Set the group position
                    groupView.SetPosition(new Rect(x, y, groupWidth, groupHeight));
                    
                    // Make sure group is sent to back (nodes will be on top)
                    groupView.SendToBack();
                    
                    Debug.Log($"Positioned group {groupView.GroupData.Name} at {x},{y}");
                }
                
                // Now position nodes inside groups with better spacing
                foreach (var groupView in _groupViews.Values)
                {
                    // Ensure group is visible
                    groupView.visible = true;
                    groupView.style.display = DisplayStyle.Flex;
                    groupView.style.visibility = Visibility.Visible;
                    
                    // Get the node IDs for this group
                    var nodeIds = groupView.GroupData.NodeIds;
                    
                    // Skip if no nodes
                    if (nodeIds.Count == 0) continue;
                    
                    // Calculate optimal grid layout
                    int columns = Mathf.CeilToInt(Mathf.Sqrt(nodeIds.Count));
                    columns = Mathf.Clamp(columns, 1, 4); // Limit to 4 columns max
                    
                    int nodeIndex = 0;
                    foreach (var nodeId in nodeIds)
                    {
                        if (_nodeViews.TryGetValue(nodeId, out var nodeView))
                        {
                            int row = nodeIndex / columns;
                            int col = nodeIndex % columns;
                            
                            // Calculate position with better spacing
                            float posX = col * 200 + 60; // Increased margin
                            float posY = row * 150 + 80; // Increased margin
                            
                            // Set the node position relative to the group
                            nodeView.SetPosition(new Rect(posX, posY, 180, 120));
                            
                            // Ensure the node is in front of the group
                            nodeView.BringToFront();
                            
                            // Make sure node is visible
                            nodeView.visible = true;
                            nodeView.style.display = DisplayStyle.Flex;
                            nodeView.style.visibility = Visibility.Visible;
                            
                            nodeIndex++;
                        }
                    }
                    
                    // Resize group to fit nodes if needed
                    ResizeGroupToFitNodes(groupView);
                }
                
                // Position ungrouped nodes
                var ungroupedNodes = _nodeViews.Values
                    .Where(n => !_groupViews.Values.Any(g => g.GroupData.NodeIds.Contains(n.NodeData.Id)))
                    .OrderBy(n => n.NodeData.Type)
                    .ThenBy(n => n.NodeData.Name)
                    .ToList();
                
                if (ungroupedNodes.Count > 0)
                {
                    Debug.Log($"Positioning {ungroupedNodes.Count} ungrouped nodes");
                    
                    float nodeWidth = 180;
                    float nodeHeight = 120;
                    float nodeSpacing = 60;  // Increased spacing from 40
                    float nodeStartX = 250;  // Increased from 150
                    float nodeStartY = startY + (orderedGroups.Count / groupsPerRow) * (groupHeight + groupSpacing) + 200; // More space
                    int nodesPerRow = 3;  // Reduced from 4 for better spacing
                    
                    for (int i = 0; i < ungroupedNodes.Count; i++)
                    {
                        int row = i / nodesPerRow;
                        int col = i % nodesPerRow;
                        
                        float x = nodeStartX + col * (nodeWidth + nodeSpacing);
                        float y = nodeStartY + row * (nodeHeight + nodeSpacing);
                        
                        ungroupedNodes[i].SetPosition(new Rect(x, y, nodeWidth, nodeHeight));
                        ungroupedNodes[i].BringToFront();
                        
                        // Ensure node is visible
                        ungroupedNodes[i].visible = true;
                        ungroupedNodes[i].style.display = DisplayStyle.Flex;
                        ungroupedNodes[i].style.visibility = Visibility.Visible;
                        
                        Debug.Log($"Positioned ungrouped node {ungroupedNodes[i].NodeData.Name} at {x},{y}");
                    }
                }
                
                // After all nodes and groups are positioned, organize sequence nodes
                OrganizeSequenceNodes();
                
                // Bring edges to front and ensure they're visible
                foreach (var edgeView in _edgeViews.Values)
                {
                    edgeView.visible = true;
                    edgeView.style.display = DisplayStyle.Flex;
                    edgeView.style.visibility = Visibility.Visible;
                    edgeView.BringToFront();
                }
                
                // Mark dirty to force repaint
                MarkDirtyRepaint();
                
                // Schedule additional repaints to ensure everything renders correctly
                schedule.Execute(() => {
                    MarkDirtyRepaint();
                    EditorApplication.QueuePlayerLoopUpdate();
                }).ExecuteLater(200);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error in PerformBasicLayout: {ex.Message}\n{ex.StackTrace}");
            }
        }
        
        private void ResizeGroupToFitNodes(DiagramGroupView groupView)
        {
            try
            {
                if (groupView.GroupData.NodeIds.Count == 0) return;
                
                // Get current position and size
                var groupRect = groupView.GetPosition();
                
                // Calculate bounds based on nodes
                float minX = float.MaxValue;
                float minY = float.MaxValue;
                float maxX = float.MinValue;
                float maxY = float.MinValue;
                
                bool nodesFound = false;
                
                foreach (var nodeId in groupView.GroupData.NodeIds)
                {
                    if (_nodeViews.TryGetValue(nodeId, out var nodeView))
                    {
                        var nodeRect = nodeView.GetPosition();
                        
                        minX = Mathf.Min(minX, nodeRect.x);
                        minY = Mathf.Min(minY, nodeRect.y);
                        maxX = Mathf.Max(maxX, nodeRect.x + nodeRect.width);
                        maxY = Mathf.Max(maxY, nodeRect.y + nodeRect.height);
                        
                        nodesFound = true;
                    }
                }
                
                if (!nodesFound) return;
                
                // Add padding around nodes
                const float padding = 50f;
                minX -= padding;
                minY -= padding;
                maxX += padding;
                maxY += padding;
                
                // Ensure minimum size
                float width = Mathf.Max(maxX - minX, 550);
                float height = Mathf.Max(maxY - minY, 450);
                
                // Update group position and size
                groupView.SetPosition(new Rect(groupRect.x, groupRect.y, width, height));
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error resizing group: {ex.Message}");
            }
        }
        
        public void ClearGraph()
        {
            Debug.Log("Clearing graph completely");
            
            // Remove all elements first
            foreach (var nodeView in _nodeViews.Values)
            {
                RemoveElement(nodeView);
            }
            
            foreach (var edgeView in _edgeViews.Values)
            {
                RemoveElement(edgeView);
            }
            
            foreach (var groupView in _groupViews.Values)
            {
                RemoveElement(groupView);
            }
            
            // Clear all collections
            _nodeViews.Clear();
            _edgeViews.Clear();
            _groupViews.Clear();
            _signalToSequenceGroups.Clear();
            _signalToParallelGroups.Clear();
            
            // Remove any elements that might remain
            var elementsToRemove = graphElements.ToList();
            foreach (var element in elementsToRemove)
            {
                RemoveElement(element);
            }
            
            // Force UI to update
            MarkDirtyRepaint();
        }
        
        public void PerformGraphLayout()
        {
            try
            {
                if (_graphData == null) return;
                
                // Clear existing elements
                DeleteElements(_nodeViews.Values.OfType<GraphElement>().ToList());
                DeleteElements(_edgeViews.Values.OfType<GraphElement>().ToList());
                DeleteElements(_groupViews.Values.OfType<GraphElement>().ToList());
                
                _nodeViews.Clear();
                _edgeViews.Clear();
                _groupViews.Clear();
                
                switch (_viewType)
                {
                    case DiagramViewType.CategoryView:
                        CreateGraphElements();
                        break;
                    case DiagramViewType.SignalFlow:
                        CreateStandardView();
                        OrganizeNodesHierarchically();
                        break;
                    case DiagramViewType.CommandFlow:
                        CreateStandardView();
                        // OrganizeSequenceNodes();
                        OrganizeCommandNodes();
                        break;
                    default:
                        CreateGraphElements();
                        break;
                }
                
                // Handle detailed filtering
                ApplyFilters();
                
                // Schedule a repaint to ensure everything is properly rendered
                MarkDirtyRepaint();
                schedule.Execute(() => {
                    MarkDirtyRepaint();
                    EditorApplication.QueuePlayerLoopUpdate();
                }).ExecuteLater(100);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error performing graph layout: {ex.Message}\n{ex.StackTrace}");
            }
        }
        
        public void ForceElementsVisibility()
        {
            try
            {
                // Force all groups to be visible
                foreach (var groupView in _groupViews.Values)
                {
                    groupView.visible = true;
                    groupView.style.visibility = Visibility.Visible;
                    groupView.style.display = DisplayStyle.Flex;
                    groupView.style.opacity = 1;
                    
                    if (groupView.contentContainer != null)
                    {
                        groupView.contentContainer.visible = true;
                        groupView.contentContainer.style.visibility = Visibility.Visible;
                        groupView.contentContainer.style.display = DisplayStyle.Flex;
                        groupView.contentContainer.style.opacity = 1;
                    }
                    
                    var headerContainer = groupView.Q("header");
                    if (headerContainer != null)
                    {
                        headerContainer.visible = true;
                        headerContainer.style.visibility = Visibility.Visible;
                        headerContainer.style.display = DisplayStyle.Flex;
                        headerContainer.style.opacity = 1;
                    }
                    
                    var titleLabel = groupView.Q<Label>();
                    if (titleLabel != null)
                    {
                        titleLabel.visible = true;
                        titleLabel.style.visibility = Visibility.Visible;
                        titleLabel.style.display = DisplayStyle.Flex;
                        titleLabel.style.opacity = 1;
                    }
                }
                
                // Force all nodes to be visible
                foreach (var nodeView in _nodeViews.Values)
                {
                    nodeView.visible = true;
                    nodeView.style.visibility = Visibility.Visible;
                    nodeView.style.display = DisplayStyle.Flex;
                    nodeView.style.opacity = 1;
                    
                    foreach (var container in new[] { 
                        nodeView.titleContainer, 
                        nodeView.inputContainer, 
                        nodeView.outputContainer, 
                        nodeView.extensionContainer,
                        nodeView.mainContainer })
                    {
                        if (container != null)
                        {
                            container.visible = true;
                            container.style.visibility = Visibility.Visible;
                            container.style.display = DisplayStyle.Flex;
                            container.style.opacity = 1;
                        }
                    }
                    
                    foreach (var port in nodeView._inputPorts.Concat(nodeView._outputPorts))
                    {
                        port.visible = true;
                        port.style.visibility = Visibility.Visible;
                        port.style.display = DisplayStyle.Flex;
                        port.style.opacity = 1;
                    }
                    
                    var titleLabel = nodeView.Q<Label>();
                    if (titleLabel != null)
                    {
                        titleLabel.visible = true;
                        titleLabel.style.visibility = Visibility.Visible;
                        titleLabel.style.display = DisplayStyle.Flex;
                        titleLabel.style.opacity = 1;
                    }
                }
                
                // Force all edges to be visible
                foreach (var edgeView in _edgeViews.Values)
                {
                    edgeView.visible = true;
                    edgeView.style.visibility = Visibility.Visible;
                    edgeView.style.display = DisplayStyle.Flex;
                    float opacity = edgeView.style.opacity.value;
                    edgeView.style.opacity = opacity > 0 ? opacity : 1;
                }
                
                // Force entire graph to update visually
                MarkDirtyRepaint();
                schedule.Execute(() => {
                    MarkDirtyRepaint();
                    EditorApplication.QueuePlayerLoopUpdate();
                }).ExecuteLater(200);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error forcing elements visibility: {ex.Message}\n{ex.StackTrace}");
            }
        }
        
        public EnhancedDiagramNodeView GetNodeView(string nodeId)
        {
            if (_nodeViews.TryGetValue(nodeId, out var nodeView))
            {
                return nodeView;
            }
            return null;
        }
        
        public void SetNodeTypeFilter(NodeType nodeType, bool isVisible)
        {
            _nodeTypeFilters[nodeType] = isVisible;
            ApplyFilters();
        }
        
        public void SetSearchFilter(string searchText)
        {
            _searchFilter = searchText;
            ApplyFilters();
        }
        
        private void ApplyFilters()
        {
            foreach (var nodeView in _nodeViews.Values)
            {
                bool typeVisible = _nodeTypeFilters.ContainsKey(nodeView.NodeData.Type) && 
                                  _nodeTypeFilters[nodeView.NodeData.Type];
                
                bool matchesSearch = string.IsNullOrEmpty(_searchFilter) || 
                                    nodeView.NodeData.Name.IndexOf(_searchFilter, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                    nodeView.NodeData.TypeName.IndexOf(_searchFilter, StringComparison.OrdinalIgnoreCase) >= 0;
                
                bool isVisible = typeVisible && matchesSearch;
                
                nodeView.style.display = isVisible ? DisplayStyle.Flex : DisplayStyle.None;
            }
            
            foreach (var edgeView in _edgeViews.Values)
            {
                bool sourceVisible = false;
                bool targetVisible = false;
                
                if (_nodeViews.TryGetValue(edgeView.EdgeData.SourceNodeId, out var sourceNode))
                {
                    sourceVisible = sourceNode.style.display == DisplayStyle.Flex;
                }
                
                if (_nodeViews.TryGetValue(edgeView.EdgeData.TargetNodeId, out var targetNode))
                {
                    targetVisible = targetNode.style.display == DisplayStyle.Flex;
                }
                
                edgeView.style.display = (sourceVisible && targetVisible) ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }
        
        private void ConfigureCommandSequenceGroup(DiagramNodeGroup group, DiagramGroupView groupView)
        {
            var nodes = group.NodeIds
                .Where(id => _nodeViews.ContainsKey(id))
                .Select(id => _nodeViews[id])
                .ToList();
            
            if (group.IsSequenceGroup)
            {
                nodes = nodes.OrderBy(n => n.NodeData.ExecutionOrder).ToList();
            }
            
            EnhancedDiagramNodeView signalNode = null;
            if (!string.IsNullOrEmpty(group.TriggerSignalId) && _nodeViews.ContainsKey(group.TriggerSignalId))
            {
                signalNode = _nodeViews[group.TriggerSignalId];
                
                var signalName = signalNode.NodeData.Name;
                group.AddMetadata("SignalName", signalName);
            }
            
            foreach (var nodeView in nodes)
            {
                if (group.IsSequenceGroup)
                {
                    nodeView.AddToClassList("sequence-node");
                    
                    int order = nodeView.NodeData.ExecutionOrder;
                    nodeView.tooltip += $"\nSequence Order: {order}";
                }
                else if (group.IsParallelGroup)
                {
                    nodeView.AddToClassList("parallel-node");
                }
            }
        }
        
        private void CreateGraphElements()
        {
            if (_graphData == null) 
            {
                Debug.LogError("GraphData is null, cannot create elements");
                return;
            }
            
            try
            {
                EditorUtility.DisplayProgressBar("Loading Diagram", "Initializing diagram elements", 0.0f);
                
                if (_viewType == DiagramViewType.CategoryView)
                {
                    Debug.Log("Creating category view");
                    
                    EditorUtility.DisplayProgressBar("Loading Diagram", "Creating category groups", 0.1f);
                    List<DiagramNodeGroup> groups = CreateCategoryGroups();
                    Debug.Log($"Created {groups.Count} category groups");
                    
                    EditorUtility.DisplayProgressBar("Loading Diagram", "Adding groups to view", 0.3f);
                    foreach (var group in groups)
                    {
                        var groupView = CreateGroupView(group);
                        if (groupView != null)
                        {
                            Debug.Log($"Added group {group.Name} to view");
                        }
                    }
                    
                    EditorUtility.DisplayProgressBar("Loading Diagram", "Creating nodes", 0.5f);
                    foreach (var node in _graphData.Nodes)
                    {
                        CreateNodeView(node);
                        Debug.Log($"Created node view for {node.Name}");
                    }
                    
                    EditorUtility.DisplayProgressBar("Loading Diagram", "Linking nodes to groups", 0.7f);
                    EditorApplication.delayCall += () => {
                        try {
                            AssignNodesToGroups();
                            Debug.Log("Assigned nodes to groups");
                            
                            EditorUtility.DisplayProgressBar("Loading Diagram", "Creating edges", 0.8f);
                            EditorApplication.delayCall += () => {
                                try {
                                    CreateAllEdges();
                                    Debug.Log("Created all edges");
                                    
                                    EditorUtility.DisplayProgressBar("Loading Diagram", "Arranging layout", 0.9f);
                                    EditorApplication.delayCall += () => {
                                        try {
                                            ArrangeLayout();
                                            Debug.Log("Arranged layout");
                                            
                                            EditorUtility.DisplayProgressBar("Loading Diagram", "Finalizing diagram", 1.0f);
                                            EditorApplication.delayCall += () => {
                                                try {
                                                    EnsureVisibility();
                                                    Debug.Log("Ensured visibility");
                                                    
                                                    MarkDirtyRepaint();
                                                    EditorApplication.delayCall += () => MarkDirtyRepaint();
                                                    
                                                    FrameAll();
                                                    Debug.Log("Framed all elements");
                                                    
                                                    EditorUtility.ClearProgressBar();
                                                }
                                                catch (Exception ex) {
                                                    EditorUtility.ClearProgressBar();
                                                    Debug.LogError($"Error finalizing diagram: {ex.Message}\n{ex.StackTrace}");
                                                }
                                            };
                                        }
                                        catch (Exception ex) {
                                            EditorUtility.ClearProgressBar();
                                            Debug.LogError($"Error arranging layout: {ex.Message}\n{ex.StackTrace}");
                                        }
                                    };
                                }
                                catch (Exception ex) {
                                    EditorUtility.ClearProgressBar();
                                    Debug.LogError($"Error creating edges: {ex.Message}\n{ex.StackTrace}");
                                }
                            };
                        }
                        catch (Exception ex) {
                            EditorUtility.ClearProgressBar();
                            Debug.LogError($"Error assigning nodes: {ex.Message}\n{ex.StackTrace}");
                        }
                    };
                }
                else
                {
                    Debug.Log("Creating standard view");
                    CreateStandardView();
                }
            }
            catch (Exception ex)
            {
                EditorUtility.ClearProgressBar();
                Debug.LogError($"Error creating graph elements: {ex.Message}\n{ex.StackTrace}");
            }
        }
        
        private List<DiagramNodeGroup> CreateCategoryGroups()
        {
            List<DiagramNodeGroup> groups = new List<DiagramNodeGroup>();
            
            try
            {
                var nodesByType = new Dictionary<NodeType, List<DiagramNode>>();
                
                foreach (var node in _graphData.Nodes)
                {
                    if (!nodesByType.ContainsKey(node.Type))
                    {
                        nodesByType[node.Type] = new List<DiagramNode>();
                    }
                    
                    nodesByType[node.Type].Add(node);
                }
                
                var contextNodes = nodesByType.ContainsKey(NodeType.Context) ? nodesByType[NodeType.Context] : new List<DiagramNode>();
                
                EnsureRequiredNodeTypes(nodesByType, contextNodes);
                
                float totalWidth = 1200f; // Total canvas width
                float horizontalSpacing = 50f;
                float verticalSpacing = 50f;
                
                var groupPositions = new Dictionary<NodeType, Vector2>
                {
                    { NodeType.Context, new Vector2(100, 100) },
                    { NodeType.Signal, new Vector2(totalWidth / 2 + horizontalSpacing, 100) },
                    { NodeType.Command, new Vector2(100, 100 + 300 + verticalSpacing) },
                    { NodeType.View, new Vector2(totalWidth / 2 + horizontalSpacing, 100 + 300 + verticalSpacing) },
                    { NodeType.Mediator, new Vector2(100, 100 + 600 + verticalSpacing * 2) },
                    { NodeType.Injectable, new Vector2(totalWidth / 2 + horizontalSpacing, 100 + 600 + verticalSpacing * 2) }
                };
                
                foreach (var entry in nodesByType)
                {
                    var nodeType = entry.Key;
                    var nodes = entry.Value;
                    
                    if (nodes.Count == 0) continue;
                    
                    string groupName = nodeType switch
                    {
                        NodeType.Context => "Contexts",
                        NodeType.Signal => "Signals",
                        NodeType.Command => "Commands",
                        NodeType.View => "Views",
                        NodeType.Mediator => "Mediators",
                        NodeType.Injectable => "Injectables",
                        _ => "Other"
                    };
                    
                    int columns = Math.Min(nodes.Count, 3);
                    int rows = (nodes.Count + columns - 1) / columns;
                    float width = Math.Max(columns * 220 + 100, 350);
                    float height = Math.Max(rows * 180 + 120, 300);
                    
                    var pos = groupPositions.ContainsKey(nodeType) 
                        ? groupPositions[nodeType] 
                        : new Vector2(100, 100);
                    
                    var groupId = $"group_{nodeType}";
                    var group = new DiagramNodeGroup(
                        groupId,
                        groupName,
                        nodeType,
                        DiagramNodeGroup.GetColorForNodeType(nodeType),
                        new Rect(pos.x, pos.y, width, height)
                    );
                    
                    foreach (var node in nodes)
                    {
                        group.NodeIds.Add(node.Id);
                    }
                    
                    _graphData.Groups[groupId] = group;
                    
                    groups.Add(group);
                }
                
                CreateInterGroupConnections(groups);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error creating category groups: {ex.Message}\n{ex.StackTrace}");
            }
            
            return groups;
        }
        
        private void EnsureRequiredNodeTypes(Dictionary<NodeType, List<DiagramNode>> nodesByType, List<DiagramNode> contextNodes)
        {
            try
            {
                var requiredTypes = new NodeType[] { 
                    NodeType.Signal, 
                    NodeType.Command, 
                    NodeType.Mediator, 
                    NodeType.View, 
                    NodeType.Injectable 
                };
                
                foreach (var contextNode in contextNodes)
                {
                    string contextName = contextNode.Name;
                    string contextPrefix = contextName.Replace("Context", "");
                    
                    foreach (var nodeType in requiredTypes)
                    {
                        if (!nodesByType.ContainsKey(nodeType) || nodesByType[nodeType].Count < 1)
                        {
                            if (!nodesByType.ContainsKey(nodeType))
                            {
                                nodesByType[nodeType] = new List<DiagramNode>();
                            }
                            
                            string nodeName = nodeType switch
                            {
                                NodeType.Signal => $"{contextPrefix}Signal",
                                NodeType.Command => $"{contextPrefix}Command",
                                NodeType.Mediator => $"{contextPrefix}Mediator",
                                NodeType.View => $"{contextPrefix}View",
                                NodeType.Injectable => $"{contextPrefix}Service",
                                _ => $"{contextPrefix}Node"
                            };
                            
                            var newNode = new DiagramNode(
                                $"{nodeName.ToLower()}_{Guid.NewGuid().ToString().Substring(0, 8)}",
                                nodeName,
                                nodeName,
                                $"Generated from {contextName}",
                                nodeType
                            );
                            
                            _graphData.Nodes.Add(newNode);
                            nodesByType[nodeType].Add(newNode);
                            
                            string edgeId = $"edge_{contextNode.Id}_{newNode.Id}";
                            var edge = new DiagramEdge(
                                edgeId,
                                contextNode.Id,
                                newNode.Id,
                                EdgeType.Unknown,
                                $"Context to {nodeType}"
                            );
                            
                            _graphData.Edges.Add(edge);
                            
                            if (nodeType == NodeType.Command && nodesByType.ContainsKey(NodeType.Signal))
                            {
                                foreach (var signal in nodesByType[NodeType.Signal])
                                {
                                    string cmdToSignalEdgeId = $"edge_{newNode.Id}_{signal.Id}";
                                    var cmdToSignalEdge = new DiagramEdge(
                                        cmdToSignalEdgeId,
                                        newNode.Id,
                                        signal.Id,
                                        EdgeType.SignalBinding,
                                        "Command triggers Signal"
                                    );
                                    
                                    _graphData.Edges.Add(cmdToSignalEdge);
                                    break;
                                }
                            }
                            else if (nodeType == NodeType.Mediator && nodesByType.ContainsKey(NodeType.Signal))
                            {
                                foreach (var signal in nodesByType[NodeType.Signal])
                                {
                                    string mediatorToSignalEdgeId = $"edge_{signal.Id}_{newNode.Id}";
                                    var mediatorToSignalEdge = new DiagramEdge(
                                        mediatorToSignalEdgeId,
                                        signal.Id,
                                        newNode.Id,
                                        EdgeType.MediatorBinding,
                                        "Signal triggers Mediator"
                                    );
                                    
                                    _graphData.Edges.Add(mediatorToSignalEdge);
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error ensuring required node types: {ex.Message}\n{ex.StackTrace}");
            }
        }
        
        private void CreateInterGroupConnections(List<DiagramNodeGroup> groups)
        {
            try
            {
                var groupsByType = groups.ToDictionary(g => g.Type, g => g);
                
                if (groupsByType.ContainsKey(NodeType.Context) && groupsByType.ContainsKey(NodeType.Command))
                {
                    var contextGroup = groupsByType[NodeType.Context];
                    var commandGroup = groupsByType[NodeType.Command];
                    
                    foreach (var contextNodeId in contextGroup.NodeIds)
                    {
                        bool hasConnection = false;
                        
                        foreach (var edge in _graphData.Edges)
                        {
                            if (edge.SourceNodeId == contextNodeId && 
                                commandGroup.NodeIds.Contains(edge.TargetNodeId))
                            {
                                hasConnection = true;
                                break;
                            }
                        }
                        
                        if (!hasConnection && commandGroup.NodeIds.Count > 0)
                        {
                            var targetNodeId = commandGroup.NodeIds[0];
                            
                            string edgeId = $"edge_{contextNodeId}_{targetNodeId}_auto";
                            var edge = new DiagramEdge(
                                edgeId,
                                contextNodeId,
                                targetNodeId,
                                EdgeType.CommandBinding,
                                "Context to Command"
                            );
                            
                            _graphData.Edges.Add(edge);
                        }
                    }
                }
                
                if (groupsByType.ContainsKey(NodeType.Command) && groupsByType.ContainsKey(NodeType.Signal))
                {
                    var commandGroup = groupsByType[NodeType.Command];
                    var signalGroup = groupsByType[NodeType.Signal];
                    
                    foreach (var commandNodeId in commandGroup.NodeIds)
                    {
                        bool hasConnection = false;
                        
                        foreach (var edge in _graphData.Edges)
                        {
                            if (edge.SourceNodeId == commandNodeId && 
                                signalGroup.NodeIds.Contains(edge.TargetNodeId))
                            {
                                hasConnection = true;
                                break;
                            }
                        }
                        
                        if (!hasConnection && signalGroup.NodeIds.Count > 0)
                        {
                            var targetNodeId = signalGroup.NodeIds[0];
                            
                            string edgeId = $"edge_{commandNodeId}_{targetNodeId}_auto";
                            var edge = new DiagramEdge(
                                edgeId,
                                commandNodeId,
                                targetNodeId,
                                EdgeType.SignalBinding,
                                "Command triggers Signal"
                            );
                            
                            _graphData.Edges.Add(edge);
                        }
                    }
                }
                
                if (groupsByType.ContainsKey(NodeType.Signal) && groupsByType.ContainsKey(NodeType.Mediator))
                {
                    var signalGroup = groupsByType[NodeType.Signal];
                    var mediatorGroup = groupsByType[NodeType.Mediator];
                    
                    foreach (var signalNodeId in signalGroup.NodeIds)
                    {
                        bool hasConnection = false;
                        
                        foreach (var edge in _graphData.Edges)
                        {
                            if (edge.SourceNodeId == signalNodeId && 
                                mediatorGroup.NodeIds.Contains(edge.TargetNodeId))
                            {
                                hasConnection = true;
                                break;
                            }
                        }
                        
                        if (!hasConnection && mediatorGroup.NodeIds.Count > 0)
                        {
                            var targetNodeId = mediatorGroup.NodeIds[0];
                            
                            string edgeId = $"edge_{signalNodeId}_{targetNodeId}_auto";
                            var edge = new DiagramEdge(
                                edgeId,
                                signalNodeId,
                                targetNodeId,
                                EdgeType.MediatorBinding,
                                "Signal triggers Mediator"
                            );
                            
                            _graphData.Edges.Add(edge);
                        }
                    }
                }
                
                if (groupsByType.ContainsKey(NodeType.Mediator) && groupsByType.ContainsKey(NodeType.View))
                {
                    var mediatorGroup = groupsByType[NodeType.Mediator];
                    var viewGroup = groupsByType[NodeType.View];
                    
                    foreach (var mediatorNodeId in mediatorGroup.NodeIds)
                    {
                        bool hasConnection = false;
                        
                        foreach (var edge in _graphData.Edges)
                        {
                            if (edge.SourceNodeId == mediatorNodeId && 
                                viewGroup.NodeIds.Contains(edge.TargetNodeId))
                            {
                                hasConnection = true;
                                break;
                            }
                        }
                        
                        if (!hasConnection && viewGroup.NodeIds.Count > 0)
                        {
                            var targetNodeId = viewGroup.NodeIds[0];
                            
                            string edgeId = $"edge_{mediatorNodeId}_{targetNodeId}_auto";
                            var edge = new DiagramEdge(
                                edgeId,
                                mediatorNodeId,
                                targetNodeId,
                                EdgeType.ViewBinding,
                                "Mediator updates View"
                            );
                            
                            _graphData.Edges.Add(edge);
                        }
                    }
                }
                
                if (groupsByType.ContainsKey(NodeType.Context) && groupsByType.ContainsKey(NodeType.Injectable))
                {
                    var contextGroup = groupsByType[NodeType.Context];
                    var injectableGroup = groupsByType[NodeType.Injectable];
                    
                    foreach (var contextNodeId in contextGroup.NodeIds)
                    {
                        bool hasConnection = false;
                        
                        foreach (var edge in _graphData.Edges)
                        {
                            if (edge.SourceNodeId == contextNodeId && 
                                injectableGroup.NodeIds.Contains(edge.TargetNodeId))
                            {
                                hasConnection = true;
                                break;
                            }
                        }
                        
                        if (!hasConnection && injectableGroup.NodeIds.Count > 0)
                        {
                            var targetNodeId = injectableGroup.NodeIds[0];
                            
                            string edgeId = $"edge_{contextNodeId}_{targetNodeId}_auto";
                            var edge = new DiagramEdge(
                                edgeId,
                                contextNodeId,
                                targetNodeId,
                                EdgeType.InjectableBinding,
                                "Context injects Service"
                            );
                            
                            _graphData.Edges.Add(edge);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error creating inter-group connections: {ex.Message}\n{ex.StackTrace}");
            }
        }
        
        private void AssignNodesToGroups()
        {
            try
            {
                foreach (var groupView in _groupViews.Values)
                {
                    int columns = 3;
                    int nodeIndex = 0;
                    
                    // First remove all nodes from the group
                    var elementsToRemove = groupView.Children().Where(c => c is EnhancedDiagramNodeView).ToList();
                    foreach (var element in elementsToRemove)
                    {
                        element.RemoveFromHierarchy();
                    }
                    
                    // Get a sorted list of nodes for this group
                    var groupNodes = groupView.GroupData.NodeIds
                        .Where(id => _nodeViews.ContainsKey(id))
                        .Select(id => _nodeViews[id])
                        .ToList();
                        
                    // Sort nodes by type first, then by name
                    if (groupView.GroupData.IsSequenceGroup) 
                    {
                        groupNodes = groupNodes.OrderBy(n => n.NodeData.ExecutionOrder).ToList();
                    } 
                    else 
                    {
                        groupNodes = groupNodes
                            .OrderBy(n => n.NodeData.Type)
                            .ThenBy(n => n.NodeData.Name)
                            .ToList();
                    }
                    
                    // Now add each node to the group
                    foreach (var nodeView in groupNodes)
                    {
                        int row = nodeIndex / columns;
                        int col = nodeIndex % columns;
                        
                        // Remove node from current parent
                        nodeView.RemoveFromHierarchy();
                        
                        // First add to the main graph if not already added
                        if (nodeView.parent == null)
                        {
                            AddElement(nodeView);
                        }
                        
                        // Then add to group
                        groupView.AddElement(nodeView);
                        
                        // Position node within group
                        float posX = col * 200 + 40;
                        float posY = row * 150 + 50;
                        nodeView.SetPosition(new Rect(posX, posY, 180, 120));
                        
                        // Ensure the node is visible
                        nodeView.visible = true;
                        nodeView.style.visibility = Visibility.Visible;
                        nodeView.style.display = DisplayStyle.Flex;
                        nodeView.style.opacity = 1;
                        
                        // Make sure all containers inside the node are visible
                        foreach (var container in new[] { 
                            nodeView.titleContainer, 
                            nodeView.inputContainer, 
                            nodeView.outputContainer, 
                            nodeView.extensionContainer,
                            nodeView.mainContainer })
                        {
                            if (container != null)
                            {
                                container.visible = true;
                                container.style.visibility = Visibility.Visible;
                                container.style.display = DisplayStyle.Flex;
                                container.style.opacity = 1;
                            }
                        }
                        
                        // Make sure all ports are visible
                        foreach (var port in nodeView._inputPorts.Concat(nodeView._outputPorts))
                        {
                            port.visible = true;
                            port.style.visibility = Visibility.Visible;
                            port.style.display = DisplayStyle.Flex;
                            port.style.opacity = 1;
                        }
                        
                        nodeView.BringToFront();
                        nodeIndex++;
                    }
                    
                    // Resize the group based on the number of nodes it contains
                    int rows = (nodeIndex + columns - 1) / columns;
                    float width = Math.Max(columns * 200 + 80, 350);
                    float height = Math.Max(rows * 150 + 100, 250);
                    
                    var currentPos = groupView.GetPosition();
                    groupView.SetPosition(new Rect(currentPos.x, currentPos.y, width, height));
                    
                    // Ensure the group is visible
                    groupView.visible = true;
                    groupView.style.visibility = Visibility.Visible;
                    groupView.style.display = DisplayStyle.Flex;
                    groupView.style.opacity = 1;
                    
                    if (groupView.contentContainer != null)
                    {
                        groupView.contentContainer.visible = true;
                        groupView.contentContainer.style.visibility = Visibility.Visible;
                        groupView.contentContainer.style.display = DisplayStyle.Flex;
                        groupView.contentContainer.style.opacity = 1;
                    }
                    
                    var headerContainer = groupView.Q("header");
                    if (headerContainer != null)
                    {
                        headerContainer.visible = true;
                        headerContainer.style.visibility = Visibility.Visible;
                        headerContainer.style.display = DisplayStyle.Flex;
                        headerContainer.style.opacity = 1;
                    }
                    
                    // Have the group reposition its nodes for optimal layout
                    groupView.RepositionNodes(this);
                    
                    // Send group to back so nodes appear on top
                    groupView.SendToBack();
                }
                
                // Handle nodes that don't belong to any group
                var ungroupedNodes = _nodeViews.Values
                    .Where(nv => !_groupViews.Values.Any(g => g.GroupData.NodeIds.Contains(nv.NodeData.Id)))
                    .OrderBy(n => n.NodeData.Type)
                    .ThenBy(n => n.NodeData.Name)
                    .ToList();
                
                int cols = 4;
                for (int i = 0; i < ungroupedNodes.Count; i++)
                {
                    int row = i / cols;
                    int col = i % cols;
                    
                    float x = col * 200 + 50;
                    float y = row * 150 + (_groupViews.Count > 0 ? 700 : 100);
                    
                    ungroupedNodes[i].RemoveFromHierarchy();
                    AddElement(ungroupedNodes[i]);
                    
                    ungroupedNodes[i].SetPosition(new Rect(x, y, 180, 120));
                    
                    ungroupedNodes[i].visible = true;
                    ungroupedNodes[i].style.visibility = Visibility.Visible;
                    ungroupedNodes[i].style.display = DisplayStyle.Flex;
                    ungroupedNodes[i].style.opacity = 1;
                    
                    foreach (var container in new[] { 
                        ungroupedNodes[i].titleContainer, 
                        ungroupedNodes[i].inputContainer, 
                        ungroupedNodes[i].outputContainer, 
                        ungroupedNodes[i].extensionContainer,
                        ungroupedNodes[i].mainContainer })
                    {
                        if (container != null)
                        {
                            container.visible = true;
                            container.style.visibility = Visibility.Visible;
                            container.style.display = DisplayStyle.Flex;
                            container.style.opacity = 1;
                        }
                    }
                    
                    foreach (var port in ungroupedNodes[i]._inputPorts.Concat(ungroupedNodes[i]._outputPorts))
                    {
                        port.visible = true;
                        port.style.visibility = Visibility.Visible;
                        port.style.display = DisplayStyle.Flex;
                        port.style.opacity = 1;
                    }
                    
                    ungroupedNodes[i].BringToFront();
                }
                
                // Force a repaint to update the view
                MarkDirtyRepaint();
                
                // Schedule another repaint to ensure everything is visible
                schedule.Execute(() => {
                    MarkDirtyRepaint();
                    EditorApplication.QueuePlayerLoopUpdate();
                }).ExecuteLater(100);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error assigning nodes to groups: {ex.Message}\n{ex.StackTrace}");
            }
        }
        
        private void ArrangeLayout()
        {
            try
            {
                int groupsPerRow = 3; // Display 3 groups per row as requested
                
                // Significantly increase default spacing between groups
                float horizontalSpacing = 700; 
                float verticalSpacing = 700;   
                float startX = 150;
                float startY = 150;
                
                // Define a specific ordering for group types to maintain consistent layout
                var groupOrder = new List<NodeType> {
                    NodeType.Context, 
                    NodeType.Signal,
                    NodeType.Command, 
                    NodeType.Mediator,
                    NodeType.View, 
                    NodeType.Injectable
                };
                
                // Find the main groups (excluding sequence/parallel groups which are subgroups)
                var mainGroups = _groupViews.Values
                    .Where(g => !g.GroupData.IsSequenceGroup && !g.GroupData.IsParallelGroup)
                    .OrderBy(g => {
                        int index = groupOrder.IndexOf(g.GroupData.Type);
                        return index >= 0 ? index : groupOrder.Count;
                    })
                    .ThenBy(g => g.GroupData.Name)
                    .ToList();
                
                Debug.Log($"Arranging layout for {mainGroups.Count} main groups");
                
                // Calculate proportional spacing based on group counts
                if (mainGroups.Count > 0)
                {
                    // Adjust spacing based on the total number of groups
                    float spacingFactor = Mathf.Clamp(mainGroups.Count / 4f, 1f, 1.8f);
                    horizontalSpacing = Mathf.Max(700, 700 * spacingFactor);
                    verticalSpacing = Mathf.Max(700, 700 * spacingFactor);
                }
                
                // Arrange all main groups in a grid with 3 columns per row
                for (int i = 0; i < mainGroups.Count; i++)
                {
                    int row = i / groupsPerRow;
                    int col = i % groupsPerRow;
                    
                    var group = mainGroups[i];
                    group.RepositionNodes(this);
                    var rect = group.GetPosition();
                    
                    float width = Math.Max(rect.width, 350);
                    float height = Math.Max(rect.height, 300);
                    
                    float x = startX + (col * (width + horizontalSpacing));
                    float y = startY + (row * (height + verticalSpacing));
                    
                    group.SetPosition(new Rect(x, y, width, height));
                    
                    // Ensure the group is visible
                    group.visible = true;
                    group.style.visibility = Visibility.Visible;
                    group.style.display = DisplayStyle.Flex;
                    group.style.opacity = 1;
                    
                    // Make the group appear behind its nodes
                    group.SendToBack();
                }
                
                // Calculate where to place sequence groups (after the main groups)
                int rowCount = (int)Math.Ceiling(mainGroups.Count / (float)groupsPerRow);
                float currentY = startY + (rowCount * (300 + verticalSpacing)) + 150;
                
                // Now process sequence groups
                var sequenceGroups = _groupViews.Values
                    .Where(g => g.GroupData.IsSequenceGroup || g.GroupData.IsParallelGroup)
                    .ToList();
                
                if (sequenceGroups.Count > 0)
                {
                    // Arrange sequence groups in a grid with 3 columns as requested
                    for (int i = 0; i < sequenceGroups.Count; i++)
                    {
                        int row = i / groupsPerRow;
                        int col = i % groupsPerRow;
                        
                        var group = sequenceGroups[i];
                        group.RepositionNodes(this);
                        var rect = group.GetPosition();
                        
                        float width = Math.Max(rect.width, 350);
                        float height = Math.Max(rect.height, 350);
                        
                        float x = startX + (col * (width + horizontalSpacing));
                        float y = currentY + (row * (height + verticalSpacing));
                        
                        group.SetPosition(new Rect(x, y, width, height));
                        
                        // Ensure the group is visible
                        group.visible = true;
                        group.style.visibility = Visibility.Visible;
                        group.style.display = DisplayStyle.Flex;
                        group.style.opacity = 1;
                        
                        // Make the group appear behind its nodes
                        group.SendToBack();
                    }
                }
                
                // Force a repaint to update the view
                MarkDirtyRepaint();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error arranging layout: {ex.Message}\n{ex.StackTrace}");
            }
        }
        
        private void CreateAllEdges()
        {
            try
            {
                if (_graphData?.Edges == null || _graphData.Edges.Count == 0)
                {
                    Debug.Log("No edges to create");
                    return;
                }
                
                Debug.Log($"Creating {_graphData.Edges.Count} edges");
                
                // First, group edges by node pairs to ensure only one edge between each pair of nodes
                var distinctEdges = new Dictionary<string, DiagramEdge>();
                
                foreach (var edge in _graphData.Edges)
                {
                    string key = $"{edge.SourceNodeId}_{edge.TargetNodeId}";
                    
                    // If we already have an edge between these nodes, keep the one with higher priority
                    if (distinctEdges.TryGetValue(key, out var existingEdge))
                    {
                        // Prefer sequence/parallel commands over regular ones
                        bool isSpecialEdge = edge.Type == EdgeType.SequentialCommand || edge.Type == EdgeType.ParallelCommand;
                        bool isExistingSpecial = existingEdge.Type == EdgeType.SequentialCommand || existingEdge.Type == EdgeType.ParallelCommand;
                        
                        if (isSpecialEdge && !isExistingSpecial)
                        {
                            distinctEdges[key] = edge;
                        }
                    }
                    else
                    {
                        distinctEdges[key] = edge;
                    }
                }
                
                // Create the edges using our filtered list
                foreach (var edge in distinctEdges.Values)
                {
                    try
                    {
                        if (!_nodeViews.TryGetValue(edge.SourceNodeId, out var sourceNode) || 
                            !_nodeViews.TryGetValue(edge.TargetNodeId, out var targetNode))
                        {
                            Debug.LogWarning($"Cannot create edge {edge.Id}: Source or target node view not found");
                            continue;
                        }
                        
                        Debug.Log($"Creating edge from {sourceNode.NodeData.Name} to {targetNode.NodeData.Name}");
                        
                        var outputPort = sourceNode._outputPorts.FirstOrDefault();
                        var inputPort = targetNode._inputPorts.FirstOrDefault();
                        
                        if (outputPort == null || inputPort == null)
                        {
                            Debug.LogWarning($"Cannot create edge {edge.Id}: Port is null (output: {outputPort != null}, input: {inputPort != null})");
                            continue;
                        }
                        
                        var edgeView = new DiagramEdgeView();
                        
                        AddElement(edgeView);
                        
                        edgeView.Initialize(edge, outputPort, inputPort);
                        
                        EnhanceEdgeStyle(edgeView, edge.Type);
                        
                        _edgeViews[edge.Id] = edgeView;
                        
                        edgeView.style.visibility = Visibility.Visible;
                        edgeView.style.display = DisplayStyle.Flex;
                        edgeView.style.opacity = 1;
                        
                        edgeView.BringToFront();
                        
                        Debug.Log($"Edge {edge.Id} created successfully");
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"Error creating edge {edge.Id}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error creating all edges: {ex.Message}\n{ex.StackTrace}");
            }
        }
        
        private void EnhanceEdgeStyle(DiagramEdgeView edgeView, EdgeType edgeType)
        {
            try
            {
                if (edgeView == null) return;
                
                edgeView.AddToClassList(edgeType.ToString().ToLower() + "-edge");
                
                SetEdgeStylePriority(edgeView, GetEdgeTypePriority(edgeType));
                
                edgeView.capabilities |= Capabilities.Selectable;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error enhancing edge style: {ex.Message}");
            }
        }
        
        private int GetEdgeTypePriority(EdgeType edgeType)
        {
            switch (edgeType)
            {
                case EdgeType.CommandBinding:
                    return 3;
                case EdgeType.SignalBinding:
                case EdgeType.MediatorBinding:
                    return 2;
                case EdgeType.ViewBinding:
                case EdgeType.InjectableBinding:
                case EdgeType.InjectionBinding:
                    return 1;
                case EdgeType.Unknown:
                default:
                    return 0;
            }
        }
        
        private void SetEdgeStylePriority(DiagramEdgeView edgeView, int priority)
        {
            try
            {
                float opacity = Mathf.Clamp(0.7f + (priority * 0.1f), 0.7f, 1.0f);
                edgeView.style.opacity = opacity;
                
                if (priority > 0)
                {
                    edgeView.BringToFront();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error setting edge style priority: {ex.Message}");
            }
        }
        
        private void EnsureVisibility()
        {
            try
            {
                foreach (var nodeView in _nodeViews.Values)
                {
                    nodeView.visible = true;
                    nodeView.style.visibility = Visibility.Visible;
                    nodeView.style.display = DisplayStyle.Flex;
                    nodeView.style.opacity = 1;
                    
                    foreach (var port in nodeView._inputPorts.Concat(nodeView._outputPorts))
                    {
                        port.visible = true;
                        port.style.visibility = Visibility.Visible;
                        port.style.display = DisplayStyle.Flex;
                        port.style.opacity = 1;
                        
                        foreach (var child in port.Children())
                        {
                            child.visible = true;
                            child.style.visibility = Visibility.Visible;
                            child.style.display = DisplayStyle.Flex;
                            child.style.opacity = 1;
                        }
                    }
                    
                    var titleLabel = nodeView.titleContainer.Q<Label>();
                    if (titleLabel != null)
                    {
                        titleLabel.visible = true;
                        titleLabel.style.visibility = Visibility.Visible;
                        titleLabel.style.display = DisplayStyle.Flex;
                        titleLabel.style.opacity = 1;
                    }
                }
                
                foreach (var edgeView in _edgeViews.Values)
                {
                    edgeView.visible = true;
                    edgeView.style.visibility = Visibility.Visible;
                    edgeView.style.display = DisplayStyle.Flex;
                    edgeView.style.opacity = 1;
                    
                    if (edgeView.edgeControl != null)
                    {
                        edgeView.edgeControl.visible = true;
                        
                        edgeView.edgeControl.inputColor = edgeView.EdgeData.Color;
                        edgeView.edgeControl.outputColor = edgeView.EdgeData.Color;
                    }
                }
                
                foreach (var groupView in _groupViews.Values)
                {
                    groupView.visible = true;
                    groupView.style.visibility = Visibility.Visible;
                    groupView.style.display = DisplayStyle.Flex;
                    groupView.style.opacity = 1;
                    
                    if (groupView.contentContainer != null)
                    {
                        groupView.contentContainer.visible = true;
                        groupView.contentContainer.style.visibility = Visibility.Visible;
                        groupView.contentContainer.style.display = DisplayStyle.Flex;
                        groupView.contentContainer.style.opacity = 1;
                    }
                    
                    var headerContainer = groupView.Q("header");
                    if (headerContainer != null)
                    {
                        headerContainer.visible = true;
                        headerContainer.style.visibility = Visibility.Visible;
                        headerContainer.style.display = DisplayStyle.Flex;
                        headerContainer.style.opacity = 1;
                        
                        var titleLabel = headerContainer.Q<Label>();
                        if (titleLabel != null)
                        {
                            titleLabel.visible = true;
                            titleLabel.style.visibility = Visibility.Visible;
                            titleLabel.style.display = DisplayStyle.Flex;
                            titleLabel.style.opacity = 1;
                        }
                    }
                }
                
                var problematicNodes = _nodeViews.Values
                    .Where(nv => nv.parent == null || !nv.visible || nv.style.visibility != Visibility.Visible)
                    .ToList();
                    
                foreach (var node in problematicNodes)
                {
                    node.RemoveFromHierarchy();
                    AddElement(node);
                    node.visible = true;
                    node.style.visibility = Visibility.Visible;
                    node.style.display = DisplayStyle.Flex;
                    node.style.opacity = 1;
                    node.BringToFront();
                }
                
                var problematicEdges = _edgeViews.Values
                    .Where(ev => ev.parent == null || !ev.visible || ev.style.visibility != Visibility.Visible)
                    .ToList();
                    
                foreach (var edge in problematicEdges)
                {
                    edge.RemoveFromHierarchy();
                    AddElement(edge);
                    edge.visible = true;
                    edge.style.visibility = Visibility.Visible;
                    edge.style.display = DisplayStyle.Flex;
                    edge.style.opacity = 1;
                    edge.BringToFront();
                }
                
                foreach (var groupView in _groupViews.Values)
                {
                    groupView.SendToBack();
                }
                
                foreach (var nodeView in _nodeViews.Values)
                {
                    nodeView.BringToFront();
                }
                
                foreach (var edgeView in _edgeViews.Values)
                {
                    edgeView.BringToFront();
                }
                
                schedule.Execute(() => {
                    MarkDirtyRepaint();
                    EditorApplication.QueuePlayerLoopUpdate();
                    SceneView.RepaintAll();
                }).ExecuteLater(50);
                
                schedule.Execute(() => {
                    FrameAll();
                }).ExecuteLater(100);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error ensuring visibility: {ex.Message}\n{ex.StackTrace}");
            }
        }
        
        private void CreateStandardView()
        {
            try
            {
                foreach (var node in _graphData.Nodes)
                {
                    CreateNodeView(node);
                }
                
                schedule.Execute(() => {
                    CreateAllEdges();
                    
                    schedule.Execute(() => {
                        OrganizeNodesHierarchically();
                        
                        schedule.Execute(() => {
                            FrameAll();
                        }).ExecuteLater(100);
                    }).ExecuteLater(100);
                }).ExecuteLater(100);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error creating standard view: {ex.Message}\n{ex.StackTrace}");
            }
        }
        
        private EnhancedDiagramNodeView CreateNodeView(DiagramNode node)
        {
            try
            {
                Debug.Log($"Creating node view for {node.Name} ({node.Id})");
                
                var nodeView = new EnhancedDiagramNodeView(node);
                _nodeViews[node.Id] = nodeView;
                
                nodeView.OnNodeSelected = OnNodeSelected;
                
                switch (node.Type)
                {
                    case NodeType.Context:
                        nodeView.style.borderTopWidth = 3;
                        nodeView.titleContainer.style.backgroundColor = new Color(0.2f, 0.4f, 0.6f);
                        break;
                    case NodeType.Signal:
                        nodeView.style.borderTopWidth = 3;
                        nodeView.titleContainer.style.backgroundColor = new Color(0.6f, 0.2f, 0.2f);
                        break;
                    case NodeType.Command:
                        nodeView.style.borderTopWidth = 3;
                        nodeView.titleContainer.style.backgroundColor = new Color(0.4f, 0.6f, 0.2f);
                        break;
                    case NodeType.View:
                        nodeView.style.borderTopWidth = 3;
                        nodeView.titleContainer.style.backgroundColor = new Color(0.2f, 0.6f, 0.6f);
                        break;
                    case NodeType.Mediator:
                        nodeView.style.borderTopWidth = 3;
                        nodeView.titleContainer.style.backgroundColor = new Color(0.6f, 0.2f, 0.6f);
                        break;
                    case NodeType.Injectable:
                        nodeView.style.borderTopWidth = 3;
                        nodeView.titleContainer.style.backgroundColor = new Color(0.6f, 0.4f, 0.2f);
                        break;
                }
                
                var titleLabel = nodeView.titleContainer.Q<Label>();
                if (titleLabel != null)
                {
                    titleLabel.style.fontSize = 14;
                    titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                    titleLabel.style.color = Color.white;
                    titleLabel.style.paddingLeft = 5;
                    titleLabel.style.paddingRight = 5;
                    titleLabel.style.paddingTop = 3;
                    titleLabel.style.paddingBottom = 3;
                    titleLabel.style.overflow = Overflow.Hidden;
                    titleLabel.style.textOverflow = TextOverflow.Ellipsis;
                    titleLabel.style.whiteSpace = WhiteSpace.NoWrap;
                    
                    titleLabel.visible = true;
                    titleLabel.style.visibility = Visibility.Visible;
                    titleLabel.style.display = DisplayStyle.Flex;
                    titleLabel.style.opacity = 1;
                }
                
                AddElement(nodeView);
                
                return nodeView;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error creating node view for {node.Name}: {ex.Message}\n{ex.StackTrace}");
                return null;
            }
        }
        
        private DiagramGroupView CreateGroupView(DiagramNodeGroup group)
        {
            try
            {
                Debug.Log($"Creating group view for {group.Name} ({group.Id})");
                
                var groupView = new DiagramGroupView(group);
                _groupViews[group.Id] = groupView;
                
                groupView.visible = true;
                groupView.style.visibility = Visibility.Visible;
                groupView.style.display = DisplayStyle.Flex;
                groupView.style.opacity = 1;
                
                AddElement(groupView);
                
                return groupView;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error creating group view for {group.Name}: {ex.Message}\n{ex.StackTrace}");
                return null;
            }
        }
        
        private void OrganizeNodesHierarchically()
        {
            try
            {
                var rootNodes = _nodeViews.Values
                    .Where(nv => !_graphData.Edges.Any(e => e.TargetNodeId == nv.NodeData.Id))
                    .ToList();
                    
                var nodeLevels = new Dictionary<string, int>();
                var processed = new HashSet<string>();
                
                foreach (var root in rootNodes)
                {
                    nodeLevels[root.NodeData.Id] = 0;
                    processed.Add(root.NodeData.Id);
                }
                
                bool changed = true;
                while (changed && processed.Count < _nodeViews.Count)
                {
                    changed = false;
                    
                    foreach (var edge in _graphData.Edges)
                    {
                        if (processed.Contains(edge.SourceNodeId) && !processed.Contains(edge.TargetNodeId))
                        {
                            nodeLevels[edge.TargetNodeId] = nodeLevels[edge.SourceNodeId] + 1;
                            processed.Add(edge.TargetNodeId);
                            changed = true;
                        }
                    }
                }
                
                foreach (var nodeView in _nodeViews.Values)
                {
                    if (!nodeLevels.ContainsKey(nodeView.NodeData.Id))
                    {
                        nodeLevels[nodeView.NodeData.Id] = 0;
                    }
                }
                
                var nodesByLevel = nodeLevels.GroupBy(p => p.Value)
                    .ToDictionary(g => g.Key, g => g.Select(p => p.Key).ToList());
                    
                int maxNodesPerLevel = nodesByLevel.Values.Max(l => l.Count);
                float levelWidth = Math.Max(maxNodesPerLevel * 350, 1000); // Increased from 220, 800
                
                foreach (var levelEntry in nodesByLevel)
                {
                    int level = levelEntry.Key;
                    var levelNodes = levelEntry.Value;
                    
                    for (int i = 0; i < levelNodes.Count; i++)
                    {
                        if (_nodeViews.TryGetValue(levelNodes[i], out var nodeView))
                        {
                            float x = (i + 0.5f) * (levelWidth / Math.Max(levelNodes.Count, 1)) - 90;
                            float y = level * 300 + 50; // Increased from 200
                            
                            nodeView.SetPosition(new Rect(x, y, 180, 100));
                            nodeView.BringToFront();
                        }
                    }
                }
                
                EnsureVisibility();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error organizing nodes hierarchically: {ex.Message}\n{ex.StackTrace}");
            }
        }
        
        public new void FrameAll()
        {
            try
            {
                Rect boundingRect = new Rect();
                bool isFirst = true;
                
                foreach (var groupView in _groupViews.Values)
                {
                    if (groupView.parent != null)
                    {
                        var groupRect = groupView.GetPosition();
                        if (isFirst)
                        {
                            boundingRect = groupRect;
                            isFirst = false;
                        }
                        else
                        {
                            boundingRect = RectUtils.Encompass(boundingRect, groupRect);
                        }
                    }
                }
                
                var ungroupedNodes = _nodeViews.Values
                    .Where(nv => !_groupViews.Values.Any(g => g.GroupData.NodeIds.Contains(nv.NodeData.Id)))
                    .ToList();
                
                foreach (var nodeView in ungroupedNodes)
                {
                    if (nodeView.parent != null)
                    {
                        var nodeRect = nodeView.GetPosition();
                        if (isFirst)
                        {
                            boundingRect = nodeRect;
                            isFirst = false;
                        }
                        else
                        {
                            boundingRect = RectUtils.Encompass(boundingRect, nodeRect);
                        }
                    }
                }
                
                if (isFirst || boundingRect.width < 1 || boundingRect.height < 1)
                {
                    Debug.LogWarning("No valid elements for framing");
                    return;
                }
                
                float padding = 200f;
                boundingRect.x -= padding;
                boundingRect.y -= padding;
                boundingRect.width += padding * 2;
                boundingRect.height += padding * 2;
                
                Rect viewRect = new Rect(0, 0, layout.width, layout.height);
                if (viewRect.width < 1 || viewRect.height < 1)
                {
                    viewRect = new Rect(0, 0, 1200, 800);
                }
                
                float scaleX = viewRect.width / boundingRect.width;
                float scaleY = viewRect.height / boundingRect.height;
                float scale = Mathf.Min(scaleX, scaleY, 1.0f);
                
                scale = Mathf.Max(scale, 0.3f);
                
                Vector3 center = new Vector3(boundingRect.x + boundingRect.width / 2, 
                                             boundingRect.y + boundingRect.height / 2, 0);
                
                Vector3 position = -center * scale + new Vector3(viewRect.width / 2, viewRect.height / 2, 0);
                
                base.UpdateViewTransform(position, new Vector3(scale, scale, 1));

                MarkDirtyRepaint();
                
                schedule.Execute(() => {
                    MarkDirtyRepaint();
                    EditorApplication.QueuePlayerLoopUpdate();
                    SceneView.RepaintAll();
                }).ExecuteLater(50);
                
                Debug.Log($"Framed with bounds {boundingRect}, scale: {scale}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error framing all elements: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private void FrameSelection(IEnumerable<GraphElement> elements, float padding = 0)
        {
            if (elements == null || !elements.Any())
                return;

            Rect rect = elements.First().GetPosition();
            foreach (var element in elements)
            {
                rect = RectUtils.Encompass(rect, element.GetPosition());
            }

            rect.x -= padding;
            rect.y -= padding;
            rect.width += padding * 2;
            rect.height += padding * 2;

            Rect viewRect = contentRect;
            float xScale = viewRect.width / rect.width;
            float yScale = viewRect.height / rect.height;
            float scale = Mathf.Min(xScale, yScale, 1.0f);
            
            scale = Mathf.Max(scale, 0.3f);

            Vector3 center = new Vector3(rect.x + rect.width / 2, rect.y + rect.height / 2, 0);
            
            Vector3 position = -center * scale + new Vector3(viewRect.width / 2, viewRect.height / 2, 0);
            
            UpdateViewTransform(position, new Vector3(scale, scale, 1));
        }

        private new void UpdateViewTransform(Vector3 position, Vector3 scale)
        {
            base.UpdateViewTransform(position, scale);

            contentViewContainer.MarkDirtyRepaint();
            MarkDirtyRepaint();

            Debug.Log($"View transform updated: Pos={position}, Scale={scale}");
        }

        private static class RectUtils
        {
            public static Rect Encompass(Rect a, Rect b)
            {
                float xMin = Mathf.Min(a.xMin, b.xMin);
                float yMin = Mathf.Min(a.yMin, b.yMin);
                float xMax = Mathf.Max(a.xMax, b.xMax);
                float yMax = Mathf.Max(a.yMax, b.yMax);
                
                return new Rect(xMin, yMin, xMax - xMin, yMax - yMin);
            }
        }

        // Add this method to the DiagramView class to organize sequence command nodes better
        private void OrganizeSequenceNodes()
        {
            // Group command nodes by their trigger signals
            var commandsByTrigger = new Dictionary<string, List<EnhancedDiagramNodeView>>();
            
            // Find all command nodes with sequence information
            var sequenceNodes = _nodeViews.Values
                .Where(n => n.NodeData.Type == NodeType.Command && 
                       (n.NodeData.IsSequenceCommand || n.NodeData.IsParallelCommand))
                .ToList();
                
            // Group sequence nodes by their trigger signals
            foreach (var node in sequenceNodes)
            {
                string triggerKey = null;
                if (node.NodeData.Metadata.TryGetValue("TriggerSignal", out var signal))
                {
                    triggerKey = "Signal:" + signal;
                }
                else if (node.NodeData.Metadata.TryGetValue("TriggerButton", out var button))
                {
                    triggerKey = "Button:" + button;
                }
                else
                {
                    continue; // Skip nodes with no trigger info
                }
                
                if (!commandsByTrigger.ContainsKey(triggerKey))
                {
                    commandsByTrigger[triggerKey] = new List<EnhancedDiagramNodeView>();
                }
                
                commandsByTrigger[triggerKey].Add(node);
            }
            
            // Now organize each group in sequence order
            float yOffset = 100;
            float xStart = 300;
            float nodeWidth = 220;
            float nodeHeight = 120;
            float horizontalSpacing = 80;  // Increased from 40
            float verticalSpacing = 120;   // Increased from 60
            int maxNodesPerRow = 4;
            
            foreach (var group in commandsByTrigger)
            {
                // Sort nodes by execution order
                var orderedNodes = group.Value.OrderBy(n => n.NodeData.ExecutionOrder).ToList();
                
                float currentX = xStart;
                float currentY = yOffset;
                int count = 0;
                
                foreach (var node in orderedNodes)
                {
                    // Position node
                    node.SetPosition(new Rect(currentX, currentY, nodeWidth, nodeHeight));
                    
                    // Update position for next node
                    count++;
                    if (count % maxNodesPerRow == 0)
                    {
                        currentX = xStart;
                        currentY += nodeHeight + verticalSpacing;
                    }
                    else
                    {
                        currentX += nodeWidth + horizontalSpacing;
                    }
                }
                
                // Update Y offset for next group
                yOffset = currentY + nodeHeight + verticalSpacing * 2;
            }
        }

        // New method to organize all command nodes in a grid layout regardless of their type
        public void OrganizeCommandNodes()
        {
            // Get all command nodes regardless of type (InSequence, InParallel, normal)
            var commandNodes = _nodeViews.Values
                .Where(n => n.NodeData.Type == NodeType.Command)
                .ToList();
                
            if (commandNodes.Count == 0)
                return;
                
            // Simple grid layout parameters
            float xStart = 100;
            float yStart = 100;
            float nodeWidth = 200;
            float nodeHeight = 120;
            float horizontalSpacing = 80;  // Increased from 40
            float verticalSpacing = 80;    // Increased from 40
            int maxNodesPerRow = 4;
            
            // Sort nodes by name for consistent layout
            var sortedNodes = commandNodes
                .OrderBy(n => n.NodeData.Name)
                .ToList();
            
            // Position all command nodes in a simple grid layout
            for (int i = 0; i < sortedNodes.Count; i++)
            {
                int row = i / maxNodesPerRow;
                int col = i % maxNodesPerRow;
                
                float x = xStart + (col * (nodeWidth + horizontalSpacing));
                float y = yStart + (row * (nodeHeight + verticalSpacing));
                
                var nodeView = sortedNodes[i];
                nodeView.SetPosition(new Rect(x, y, nodeWidth, nodeHeight));
                
                // Ensure the node is visible
                nodeView.visible = true;
                nodeView.style.visibility = Visibility.Visible;
                nodeView.style.display = DisplayStyle.Flex;
                nodeView.style.opacity = 1;
                
                // Bring node to front
                nodeView.BringToFront();
                
                // Update all ports to be visible
                foreach (var port in nodeView._inputPorts.Concat(nodeView._outputPorts))
                {
                    port.visible = true;
                    port.style.visibility = Visibility.Visible;
                    port.style.display = DisplayStyle.Flex;
                }
            }
            
            // Update the command group size if it exists
            var commandGroup = _groupViews.Values
                .FirstOrDefault(g => g.GroupData.Type == NodeType.Command);
                
            if (commandGroup != null)
            {
                int rows = (int)Math.Ceiling(sortedNodes.Count / (float)maxNodesPerRow);
                float totalWidth = (Math.Min(sortedNodes.Count, maxNodesPerRow) * (nodeWidth + horizontalSpacing)) + 100;
                float totalHeight = (rows * (nodeHeight + verticalSpacing)) + 100;
                
                commandGroup.SetPosition(new Rect(
                    xStart - 50, 
                    yStart - 50, 
                    totalWidth, 
                    totalHeight));
                
                // Send the group to back
                commandGroup.SendToBack();
            }
            
            // Force all edges to update and be visible
            foreach (var edge in _edgeViews.Values)
            {
                edge.UpdateEdgeControl();
                edge.visible = true;
                edge.style.visibility = Visibility.Visible;
                edge.style.display = DisplayStyle.Flex;
                edge.BringToFront();
            }
            
            // Update the view
            MarkDirtyRepaint();
        }
    }
}
#endif 