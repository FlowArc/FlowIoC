#if UNITY_2019_4_OR_NEWER
using System;
using System.Linq;
using System.Reflection;
using FlowIoC.Editor.DependencyDiagram.Data;
using FlowIoC.Editor.DependencyDiagram.FlowIoC.Editor.DependencyDiagram.Analyzer;
using FlowIoC.Editor.DependencyDiagram.Simulation;
using FlowIoC.Editor.DependencyDiagram.View;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using FlowIoC.BaseModule.Contexts;

namespace FlowIoC.Editor.DependencyDiagram.FlowIoC.Editor.DependencyDiagram
{
    public class DependencyDiagramWindow : EditorWindow
    {
        private DiagramView _diagramView;
        private VisualElement _toolbarContainer;
        private VisualElement _contentContainer;
        private VisualElement _statusBarContainer;
        private VisualElement _sidebarContainer;
        private VisualElement _quickFilterContainer;

        private DetailPanel _detailPanel;
        private DiagramNode _selectedNode;
        private DiagramNodeGroup _selectedGroup;

        private DiagramGraph _currentGraph;
        private ContextDependencyAnalyzer _analyzer;
        private SimulationController _simulationController;

        private Type _selectedContextType;
        private DiagramViewType _selectedViewType = DiagramViewType.CategoryView;

        // New options for enhanced visualization
        private bool _highlightSequences = true;
        private bool _showDetailPanel = true;
        private bool _animateSignalFlow = false;
        private bool _collapseSignalGroups = false;

        // Quick filters
        private bool _filterCommands = true;
        private bool _filterSignals = true;
        private bool _filterMediators = true;
        private bool _filterViews = true;
        private bool _filterInjectables = true;

        [MenuItem("Tools/FlowIoC/Dependency Diagram", false, 150)]
        public static void ShowWindow()
        {
            var window = GetWindow<DependencyDiagramWindow>();
            window.titleContent = new GUIContent("Dependency Diagram");
            window.minSize = new Vector2(800, 600);
        }

        private void OnEnable()
        {
            _analyzer = new ContextDependencyAnalyzer();
            _simulationController = new SimulationController();

            InitializeUI();
        }

        private void InitializeUI()
        {
            // Create root visual container
            rootVisualElement.styleSheets.Add(Resources.Load<StyleSheet>("DependencyDiagram/DiagramStyles"));

            // Create layout
            var rootContainer = new VisualElement();
            rootContainer.style.flexGrow = 1;
            rootContainer.style.flexDirection = FlexDirection.Column;
            rootVisualElement.Add(rootContainer);

            // Toolbar
            _toolbarContainer = new VisualElement();
            _toolbarContainer.AddToClassList("diagram-toolbar");
            _toolbarContainer.style.height = 40;
            _toolbarContainer.style.flexDirection = FlexDirection.Row;

            // Quick filter bar
            _quickFilterContainer = new VisualElement();
            _quickFilterContainer.AddToClassList("diagram-quick-filter");
            _quickFilterContainer.style.height = 30;
            _quickFilterContainer.style.flexDirection = FlexDirection.Row;
            _quickFilterContainer.style.alignItems = Align.Center;

            // Main content area with sidebar
            var mainContainer = new VisualElement();
            mainContainer.style.flexGrow = 1;
            mainContainer.style.flexDirection = FlexDirection.Row;

            // Content container is the parent of DiagramView
            _contentContainer = new VisualElement();
            _contentContainer.AddToClassList("diagram-content");
            _contentContainer.style.flexGrow = 1;
            // Set a name for easier debugging
            _contentContainer.name = "DiagramContentContainer";

            // Optional sidebar
            _sidebarContainer = new VisualElement();
            _sidebarContainer.AddToClassList("diagram-sidebar");
            _sidebarContainer.style.width = 250;
            _sidebarContainer.style.display = DisplayStyle.Flex; // Start visible

            mainContainer.Add(_contentContainer);
            mainContainer.Add(_sidebarContainer);

            // Status bar
            _statusBarContainer = new VisualElement();
            _statusBarContainer.AddToClassList("diagram-status-bar");
            _statusBarContainer.style.height = 25;

            rootContainer.Add(_toolbarContainer);
            rootContainer.Add(_quickFilterContainer);
            rootContainer.Add(mainContainer);
            rootContainer.Add(_statusBarContainer);

            // Make sure any previous DiagramView is properly disposed
            if (_diagramView != null)
            {
                _diagramView.OnNodeSelected = null;
                _contentContainer.Remove(_diagramView);
                _diagramView = null;
            }

            // Initialize diagram view
            _diagramView = new DiagramView();
            _diagramView.style.flexGrow = 1;
            _contentContainer.Add(_diagramView);

            CreateToolbar();
            CreateQuickFilter();
            CreateSidebar();
            CreateStatusBar();

            // Register event handlers
            _diagramView.OnNodeSelected = OnNodeSelected;

            // Set status text
            SetStatusText("Ready. Select a context to analyze dependencies.");
        }

        private void CreateToolbar()
        {
            // Context selector
            var contextSelectorContainer = new VisualElement();
            contextSelectorContainer.style.flexDirection = FlexDirection.Row;
            contextSelectorContainer.style.alignItems = Align.Center;
            contextSelectorContainer.style.marginLeft = 10;

            var contextSelectorLabel = new Label("Context:");
            contextSelectorLabel.style.marginRight = 5;

            var contextSelectorButton = new Button(() => ShowContextSelector());
            contextSelectorButton.text = "Select Context";
            contextSelectorButton.style.width = 150;

            contextSelectorContainer.Add(contextSelectorLabel);
            contextSelectorContainer.Add(contextSelectorButton);

            // View type selector
            var viewTypeSelectorContainer = new VisualElement();
            viewTypeSelectorContainer.style.flexDirection = FlexDirection.Row;
            viewTypeSelectorContainer.style.alignItems = Align.Center;
            viewTypeSelectorContainer.style.marginLeft = 20;

            var viewTypeSelectorLabel = new Label("View Type:");
            viewTypeSelectorLabel.style.marginRight = 5;

            var viewTypeSelector = new EnumField(_selectedViewType);
            viewTypeSelector.style.width = 150;
            viewTypeSelector.RegisterValueChangedCallback(evt =>
            {
                _selectedViewType = (DiagramViewType) evt.newValue;
                RefreshDiagram();
            });

            viewTypeSelectorContainer.Add(viewTypeSelectorLabel);
            viewTypeSelectorContainer.Add(viewTypeSelector);

            // Actions buttons
            var actionsContainer = new VisualElement();
            actionsContainer.style.flexDirection = FlexDirection.Row;
            actionsContainer.style.alignItems = Align.Center;
            actionsContainer.style.marginLeft = 20;

            var refreshButton = new Button(() => RefreshDiagram());
            refreshButton.text = "Refresh";
            refreshButton.style.width = 80;

            var exportButton = new Button(() => ExportDiagram());
            exportButton.text = "Export";
            exportButton.style.width = 80;
            exportButton.style.marginLeft = 10;

            // Enhanced visualization options
            var optionsButton = new Button(() => ShowVisualizationOptions());
            optionsButton.text = "Options";
            optionsButton.style.width = 80;
            optionsButton.style.marginLeft = 10;

            actionsContainer.Add(refreshButton);
            actionsContainer.Add(exportButton);
            actionsContainer.Add(optionsButton);

            // Simulation controls
            var simulationContainer = new VisualElement();
            simulationContainer.style.flexDirection = FlexDirection.Row;
            simulationContainer.style.alignItems = Align.Center;
            simulationContainer.style.marginLeft = 20;

            var playButton = new Button(() => _simulationController.Play());
            playButton.text = "▶";
            playButton.style.width = 30;

            var pauseButton = new Button(() => _simulationController.Pause());
            pauseButton.text = "⏸";
            pauseButton.style.width = 30;
            pauseButton.style.marginLeft = 5;

            var stopButton = new Button(() => _simulationController.Stop());
            stopButton.text = "⏹";
            stopButton.style.width = 30;
            stopButton.style.marginLeft = 5;

            var stepButton = new Button(() => _simulationController.Step());
            stepButton.text = "⏭";
            stepButton.style.width = 30;
            stepButton.style.marginLeft = 5;

            simulationContainer.Add(playButton);
            simulationContainer.Add(pauseButton);
            simulationContainer.Add(stopButton);
            simulationContainer.Add(stepButton);

            // Add all containers to toolbar
            _toolbarContainer.Add(contextSelectorContainer);
            _toolbarContainer.Add(viewTypeSelectorContainer);
            _toolbarContainer.Add(actionsContainer);
            _toolbarContainer.Add(simulationContainer);
        }

        private void CreateQuickFilter()
        {
            _quickFilterContainer.Clear();

            var filterLabel = new Label("Quick Filters:");
            filterLabel.style.marginLeft = 10;
            filterLabel.style.marginRight = 10;
            filterLabel.style.unityTextAlign = TextAnchor.MiddleLeft;

            // Create a container for toggles to better control their layout
            var togglesContainer = new VisualElement();
            togglesContainer.style.flexDirection = FlexDirection.Row;
            togglesContainer.style.alignItems = Align.Center;

            var commandsToggle = new Toggle("Commands");
            commandsToggle.value = _filterCommands;
            commandsToggle.style.marginLeft = 5;
            commandsToggle.style.marginRight = 5;
            commandsToggle.RegisterValueChangedCallback(evt =>
            {
                _filterCommands = evt.newValue;
                ApplyFilters();
            });

            var signalsToggle = new Toggle("Signals");
            signalsToggle.value = _filterSignals;
            signalsToggle.style.marginLeft = 5;
            signalsToggle.style.marginRight = 5;
            signalsToggle.RegisterValueChangedCallback(evt =>
            {
                _filterSignals = evt.newValue;
                ApplyFilters();
            });

            var mediatorsToggle = new Toggle("Mediators");
            mediatorsToggle.value = _filterMediators;
            mediatorsToggle.style.marginLeft = 5;
            mediatorsToggle.style.marginRight = 5;
            mediatorsToggle.RegisterValueChangedCallback(evt =>
            {
                _filterMediators = evt.newValue;
                ApplyFilters();
            });

            var viewsToggle = new Toggle("Views");
            viewsToggle.value = _filterViews;
            viewsToggle.style.marginLeft = 5;
            viewsToggle.style.marginRight = 5;
            viewsToggle.RegisterValueChangedCallback(evt =>
            {
                _filterViews = evt.newValue;
                ApplyFilters();
            });

            var injectablesToggle = new Toggle("Injectables");
            injectablesToggle.value = _filterInjectables;
            injectablesToggle.style.marginLeft = 5;
            injectablesToggle.style.marginRight = 5;
            injectablesToggle.RegisterValueChangedCallback(evt =>
            {
                _filterInjectables = evt.newValue;
                ApplyFilters();
            });

            togglesContainer.Add(commandsToggle);
            togglesContainer.Add(signalsToggle);
            togglesContainer.Add(mediatorsToggle);
            togglesContainer.Add(viewsToggle);
            togglesContainer.Add(injectablesToggle);

            var spacer = new VisualElement();
            spacer.style.flexGrow = 1;

            var searchContainer = new VisualElement();
            searchContainer.style.flexDirection = FlexDirection.Row;
            searchContainer.style.alignItems = Align.Center;

            var searchLabel = new Label("Search:");
            searchLabel.style.marginRight = 5;

            var searchField = new TextField();
            searchField.style.width = 200;
            searchField.style.marginRight = 10;
            searchField.RegisterValueChangedCallback(evt => { ApplySearch(evt.newValue); });

            searchContainer.Add(searchLabel);
            searchContainer.Add(searchField);

            _quickFilterContainer.Add(filterLabel);
            _quickFilterContainer.Add(togglesContainer);
            _quickFilterContainer.Add(spacer);
            _quickFilterContainer.Add(searchContainer);
        }

        private void CreateSidebar()
        {
            _sidebarContainer.Clear();

            var headerContainer = new VisualElement();
            headerContainer.AddToClassList("sidebar-header");
            headerContainer.style.height = 30;
            headerContainer.style.flexDirection = FlexDirection.Row;
            headerContainer.style.alignItems = Align.Center;

            var headerLabel = new Label("Selection Details");
            headerLabel.style.flexGrow = 1;
            headerLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
            headerLabel.style.paddingLeft = 10;

            var closeButton = new Button(() => ToggleSidebar());
            closeButton.text = "×";
            closeButton.style.width = 20;
            closeButton.style.marginRight = 5;

            headerContainer.Add(headerLabel);
            headerContainer.Add(closeButton);

            var contentContainer = new VisualElement();
            contentContainer.style.flexGrow = 1;
            contentContainer.style.paddingLeft = 10;
            contentContainer.style.paddingRight = 10;
            contentContainer.style.paddingTop = 10;

            var noSelectionLabel = new Label("No selection." +
                                             " Click on a node or " +
                                             "group to view details.");
            noSelectionLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            noSelectionLabel.style.marginTop = 20;

            contentContainer.Add(noSelectionLabel);

            _sidebarContainer.Add(headerContainer);
            _sidebarContainer.Add(contentContainer);
        }

        private void CreateStatusBar()
        {
            var statusLabel = new Label("Ready");
            statusLabel.style.paddingLeft = 10;
            statusLabel.style.paddingRight = 10;
            statusLabel.style.unityTextAlign = TextAnchor.MiddleLeft;

            _statusBarContainer.Add(statusLabel);
        }

        private void ToggleSidebar()
        {
            bool isVisible = _sidebarContainer.style.display == DisplayStyle.Flex;
            _sidebarContainer.style.display = isVisible ? DisplayStyle.None : DisplayStyle.Flex;
        }

        private void ShowVisualizationOptions()
        {
            var menu = new GenericMenu();

            menu.AddItem(new GUIContent("Highlight Sequences"), _highlightSequences, () =>
            {
                _highlightSequences = !_highlightSequences;
                ApplyVisualizationOptions();
            });

            menu.AddItem(new GUIContent("Show Detail Panel"), _showDetailPanel, () =>
            {
                _showDetailPanel = !_showDetailPanel;
                _detailPanel?.Hide();
                _sidebarContainer.style.display = _showDetailPanel ? DisplayStyle.Flex : DisplayStyle.None;
            });

            menu.AddItem(new GUIContent("Animate Signal Flow"), _animateSignalFlow, () =>
            {
                _animateSignalFlow = !_animateSignalFlow;
                ApplyVisualizationOptions();
            });

            menu.AddItem(new GUIContent("Collapse Signal Groups"), _collapseSignalGroups, () =>
            {
                _collapseSignalGroups = !_collapseSignalGroups;
                ApplyVisualizationOptions();
            });

            menu.AddSeparator("");

            menu.AddItem(new GUIContent("Reset View"), false, () => { _diagramView.FrameAll(); });

            menu.ShowAsContext();
        }

        private void ApplyVisualizationOptions()
        {
            if (_diagramView == null) return;

            // Force repaint
            _diagramView.MarkDirtyRepaint();
        }

        private void ApplyFilters()
        {
            if (_diagramView == null) return;

            // Apply node type filters
            _diagramView.SetNodeTypeFilter(NodeType.Command, _filterCommands);
            _diagramView.SetNodeTypeFilter(NodeType.Signal, _filterSignals);
            _diagramView.SetNodeTypeFilter(NodeType.Mediator, _filterMediators);
            _diagramView.SetNodeTypeFilter(NodeType.View, _filterViews);
            _diagramView.SetNodeTypeFilter(NodeType.Injectable, _filterInjectables);

            // Force update
            _diagramView.MarkDirtyRepaint();
        }

        private void ApplySearch(string searchText)
        {
            if (_diagramView == null) return;

            // Apply search filter
            _diagramView.SetSearchFilter(searchText);

            // Force update
            _diagramView.MarkDirtyRepaint();
        }

        private void ShowContextSelector()
        {
            var menu = new GenericMenu();

            // Get all Context types
            var contextTypes = new List<Type>();
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    // Skip problematic assemblies like Azure and Microsoft ones
                    if (assembly.FullName.Contains("Azure.") ||
                        assembly.FullName.Contains("Storage") ||
                        assembly.FullName.Contains("Microsoft."))
                    {
                        continue;
                    }

                    // Try to get types from each assembly safely
                    var types = assembly.GetTypes().Where(t =>
                        typeof(IContext).IsAssignableFrom(t) &&
                        !t.IsInterface &&
                        !t.IsAbstract);

                    contextTypes.AddRange(types);
                }
                catch (ReflectionTypeLoadException ex)
                {
                    // Log issue but continue with available types
                    Debug.LogWarning($"Could not load some types from assembly {assembly.FullName}: {ex.Message}");

                    // Try to extract any valid types from the exception
                    if (ex.Types != null)
                    {
                        var validTypes = ex.Types
                            .Where(t => t != null &&
                                        typeof(IContext).IsAssignableFrom(t) &&
                                        !t.IsInterface &&
                                        !t.IsAbstract);

                        contextTypes.AddRange(validTypes);
                    }
                }
                catch (Exception ex)
                {
                    // Log other exceptions but continue
                    Debug.LogWarning($"Error accessing assembly {assembly.FullName}: {ex.Message}");
                }
            }

            // Order the collected types
            contextTypes = contextTypes.OrderBy(t => t.Name).ToList();

            foreach (var contextType in contextTypes)
            {
                menu.AddItem(
                    new GUIContent(contextType.Name),
                    _selectedContextType == contextType,
                    () => SelectContext(contextType)
                );
            }

            menu.ShowAsContext();
        }

        private void SelectContext(Type contextType)
        {
            _selectedContextType = contextType;
            RefreshDiagram();
        }

        private void RefreshDiagram()
        {
            if (_selectedContextType == null)
            {
                EditorUtility.DisplayDialog("Context Required", "Please select a context first.", "OK");
                return;
            }

            SetStatusText($"Analyzing {_selectedContextType.Name}...");

            try
            {
                // Clear existing graph
                if (_diagramView != null)
                {
                    _diagramView.ClearGraph();
                }

                // Analyze the selected context
                _currentGraph = _analyzer.AnalyzeContext(_selectedContextType);

                if (_currentGraph == null || _currentGraph.Nodes.Count == 0)
                {
                    SetStatusText($"Analysis complete, but found no elements in {_selectedContextType.Name}. Check if this is a valid context.");
                    return;
                }

                // Add additional processing for command visualization
                OptimizeCommandGroups(_currentGraph);
                CleanInvalidEdges(_currentGraph);

                // Set view type
                _diagramView.LoadGraph(_currentGraph, _selectedViewType);

                // Always perform graph layout
                _diagramView.PerformGraphLayout();

                // For CommandFlow view, force an additional layout pass after a short delay
                // This ensures all elements are properly positioned
                if (_selectedViewType == DiagramViewType.CommandFlow)
                {
                    // Use a stronger approach to ensure command nodes are properly positioned
                    EditorApplication.delayCall += () =>
                    {
                        Debug.Log("First layout pass for command nodes");
                        _diagramView.OrganizeCommandNodes();
                        EditorApplication.QueuePlayerLoopUpdate();

                        // Schedule one more update to ensure visibility
                        EditorApplication.delayCall += () =>
                        {
                            Debug.Log("Second layout pass for command nodes");
                            _diagramView.ForceElementsVisibility();
                            _diagramView.OrganizeCommandNodes(); // Apply command organization again to ensure it persists

                            // Force a repaint
                            _diagramView.MarkDirtyRepaint();
                            EditorApplication.QueuePlayerLoopUpdate();

                            // One final pass to ensure everything is stable
                            EditorApplication.delayCall += () =>
                            {
                                Debug.Log("Final visibility pass for command nodes");
                                _diagramView.ForceElementsVisibility();
                                _diagramView.MarkDirtyRepaint();
                            };
                        };
                    };
                }

                // Apply visualization settings
                ApplyFilters();
                ApplyVisualizationOptions();

                // Update UI elements
                SetStatusText($"Analysis complete. Found {_currentGraph.Nodes.Count} nodes, {_currentGraph.Edges.Count} edges, and {_currentGraph.Groups.Count} groups.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error in RefreshDiagram: {ex.Message}\n{ex.StackTrace}");
                SetStatusText($"Error analyzing context: {ex.Message}");
            }
        }

        private void CleanInvalidEdges(DiagramGraph graph)
        {
            if (graph == null) return;

            int edgeCountBefore = graph.Edges.Count;
            int nodeCountBefore = graph.Nodes.Count;
            int groupCountBefore = graph.Groups.Count;
            int duplicateEdgesCount = 0;

            try
            {
                // First ensure all referenced nodes exist
                HashSet<string> validNodeIds = new HashSet<string>(graph.Nodes.Select(n => n.Id));

                // 1. Remove edges referencing non-existent nodes
                var invalidEdges = graph.Edges.Where(edge =>
                    string.IsNullOrEmpty(edge.SourceNodeId) ||
                    string.IsNullOrEmpty(edge.TargetNodeId) ||
                    !validNodeIds.Contains(edge.SourceNodeId) ||
                    !validNodeIds.Contains(edge.TargetNodeId)).ToList();

                foreach (var edge in invalidEdges)
                {
                    Debug.LogWarning($"Removing invalid edge {edge.Id}: Source or target node doesn't exist");
                    graph.Edges.Remove(edge);
                }

                // 2. Create a dictionary to hold the preferred edges between nodes (more efficiently)
                var edgesByNodes = new Dictionary<string, DiagramEdge>();

                // Track edges that are being processed to prevent duplicates
                var processedEdgeIds = new HashSet<string>();

                foreach (var edge in graph.Edges)
                {
                    // Skip edges we've already processed
                    if (processedEdgeIds.Contains(edge.Id)) continue;

                    // Create a unique key for the edge between two nodes (source -> target)
                    string key = $"{edge.SourceNodeId}_{edge.TargetNodeId}";

                    if (edgesByNodes.TryGetValue(key, out var existingEdge))
                    {
                        // Duplicate found - decide which to keep based on priority
                        var existingType = existingEdge.Type;
                        var newType = edge.Type;

                        // Prefer special command types (sequential/parallel) over standard types
                        bool replaceExisting = (newType == EdgeType.SequentialCommand || newType == EdgeType.ParallelCommand) &&
                                               (existingType != EdgeType.SequentialCommand && existingType != EdgeType.ParallelCommand);

                        // If the new edge has higher priority, replace the existing one
                        if (replaceExisting)
                        {
                            // Quietly replace existing edge
                            edgesByNodes[key] = edge;
                            processedEdgeIds.Add(existingEdge.Id);
                            duplicateEdgesCount++;
                        }
                        else
                        {
                            // Quietly skip this edge as a duplicate
                            processedEdgeIds.Add(edge.Id);
                            duplicateEdgesCount++;
                        }
                    }
                    else
                    {
                        edgesByNodes[key] = edge;
                        processedEdgeIds.Add(edge.Id);
                    }
                }

                // If duplicates were found, rebuild the edges collection
                if (duplicateEdgesCount > 0)
                {
                    // Use a single summary message instead of individual warnings
                    Debug.LogWarning($"Removed {duplicateEdgesCount} duplicate edges between nodes");

                    // Replace the edges collection with our deduplicated version
                    graph.Edges = edgesByNodes.Values.ToList();
                }

                // 3. Clean up invalid groups
                var invalidGroups = new List<string>();
                foreach (var group in graph.Groups.Values)
                {
                    // Check if group has valid nodes
                    var validNodesInGroup = group.NodeIds.Where(nodeId => validNodeIds.Contains(nodeId)).ToList();
                    if (validNodesInGroup.Count == 0)
                    {
                        invalidGroups.Add(group.Id);
                        Debug.LogWarning($"Removing empty group {group.Name} with no valid nodes");
                    }
                    else if (validNodesInGroup.Count != group.NodeIds.Count)
                    {
                        // Update group to only include valid nodes
                        group.NodeIds = validNodesInGroup;
                        Debug.LogWarning($"Cleaned group {group.Name} - removed {group.NodeIds.Count - validNodesInGroup.Count} invalid nodes");
                    }
                }

                // Remove invalid groups
                foreach (var groupId in invalidGroups)
                {
                    graph.Groups.Remove(groupId);
                }

                int edgeCountAfter = graph.Edges.Count;
                int nodeCountAfter = graph.Nodes.Count;
                int groupCountAfter = graph.Groups.Count;

                if (edgeCountBefore != edgeCountAfter || nodeCountBefore != nodeCountAfter || groupCountBefore != groupCountAfter)
                {
                    Debug.Log($"Cleaned graph: Removed {edgeCountBefore - edgeCountAfter} edges, {nodeCountBefore - nodeCountAfter} nodes, and {groupCountBefore - groupCountAfter} groups");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error during graph cleanup: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private void OptimizeCommandGroups(DiagramGraph graph)
        {
            try
            {
                if (graph == null) return;

                // Focus on command sequence and parallel groups
                var commandGroups = graph.Groups.Values
                    .Where(g => g.IsSequenceGroup || g.IsParallelGroup)
                    .ToList();

                // Get all command nodes, including those not in sequence/parallel groups
                var allCommandNodes = graph.Nodes
                    .Where(n => n.Type == NodeType.Command)
                    .ToList();

                // Ensure all command nodes have execution order set for consistent layout
                foreach (var node in allCommandNodes)
                {
                    // If node doesn't have execution order set, set it based on name for consistent ordering
                    if (node.ExecutionOrder <= 0)
                    {
                        // Extract numeric part from name if it exists, otherwise use hash code
                        if (int.TryParse(new string(node.Name.Where(char.IsDigit).ToArray()), out int numericPart))
                        {
                            node.ExecutionOrder = numericPart;
                        }
                        else
                        {
                            // Use hash code of name for stable ordering if no numeric part
                            node.ExecutionOrder = Math.Abs(node.Name.GetHashCode() % 1000);
                        }
                    }
                }

                foreach (var group in commandGroups)
                {
                    // For sequence groups, ensure execution order is set correctly
                    if (group.IsSequenceGroup)
                    {
                        var sequenceNodes = group.NodeIds
                            .Select(id => graph.Nodes.FirstOrDefault(n => n.Id == id))
                            .Where(n => n != null)
                            .OrderBy(n => n.Name) // Default ordering by name
                            .ToList();

                        // Update execution order to ensure proper visualization
                        for (int i = 0; i < sequenceNodes.Count; i++)
                        {
                            sequenceNodes[i].ExecutionOrder = i + 1;
                        }
                    }

                    // Clean out any duplicate node IDs in the group
                    group.NodeIds = group.NodeIds.Distinct().ToList();
                }

                // Group normal command nodes by metadata to help positioning
                var normalCommandNodes = allCommandNodes
                    .Where(n => !n.IsSequenceCommand && !n.IsParallelCommand)
                    .ToList();

                // Group normal commands by any shared metadata (like signals they respond to)
                var commandsByMetadata = new Dictionary<string, List<DiagramNode>>();

                foreach (var node in normalCommandNodes)
                {
                    string groupKey = "Default";

                    // Try to derive a group key from metadata
                    if (node.Metadata.TryGetValue("TriggerSignal", out var signal))
                    {
                        groupKey = "Signal:" + signal;
                    }
                    else if (node.Metadata.TryGetValue("TriggerButton", out var button))
                    {
                        groupKey = "Button:" + button;
                    }

                    if (!commandsByMetadata.ContainsKey(groupKey))
                    {
                        commandsByMetadata[groupKey] = new List<DiagramNode>();
                    }

                    commandsByMetadata[groupKey].Add(node);
                }

                // Now add group metadata to each node to help with layout
                foreach (var entry in commandsByMetadata)
                {
                    foreach (var node in entry.Value)
                    {
                        node.GroupName = entry.Key;
                    }
                }

                Debug.Log($"Optimized {commandGroups.Count} command groups and {allCommandNodes.Count} command nodes for better layout");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error optimizing command groups: {ex.Message}");
            }
        }

        private void ExportDiagram()
        {
            if (_currentGraph == null)
            {
                EditorUtility.DisplayDialog("Export Error", "No diagram to export.", "OK");
                return;
            }

            var path = EditorUtility.SaveFilePanel(
                "Export Diagram",
                "",
                $"{_selectedContextType?.Name ?? "Diagram"}.png",
                "png"
            );

            if (!string.IsNullOrEmpty(path))
            {
                // Simple implementation - in a complete solution, we would render the diagram to a texture
                EditorUtility.DisplayDialog("Export", "Export functionality not fully implemented yet.", "OK");
            }
        }

        private void OnNodeSelected(EnhancedDiagramNodeView nodeView)
        {
            _selectedNode = nodeView.NodeData;
            _selectedGroup = null;

            SetStatusText($"Selected: {nodeView.NodeData.Name} ({nodeView.NodeData.TypeName})");

            // Show detail panel for selected node
            if (_showDetailPanel && _detailPanel != null)
            {
                _detailPanel.Show(_selectedNode, position);
                UpdateDetailPanel();
            }
        }

        private void OnGroupSelected(DiagramNodeGroup groupData)
        {
            _selectedNode = null;
            _selectedGroup = groupData;

            SetStatusText($"Selected Group: {groupData.Name}");

            // Show detail panel for selected group
            if (_showDetailPanel && _detailPanel != null)
            {
                _detailPanel.Show(_selectedGroup, position);
                UpdateDetailPanel();
            }
        }

        private void UpdateDetailPanel()
        {
            // Clear existing content
            var contentContainer = _sidebarContainer.Q<VisualElement>().ElementAt(1);
            contentContainer.Clear();

            if (_selectedNode != null)
            {
                // Create detail content for node
                var scrollView = new ScrollView();
                scrollView.style.flexGrow = 1;

                var nodeTypeLabel = new Label($"Type: {_selectedNode.Type}");
                nodeTypeLabel.AddToClassList("detail-property");

                var nodeNameLabel = new Label($"Name: {_selectedNode.Name}");
                nodeNameLabel.AddToClassList("detail-property");

                var nodeTypeNameLabel = new Label($"Full Type: {_selectedNode.TypeName}");
                nodeTypeNameLabel.AddToClassList("detail-property");

                scrollView.Add(nodeTypeLabel);
                scrollView.Add(nodeNameLabel);
                scrollView.Add(nodeTypeNameLabel);

                // Command specific information
                if (_selectedNode.Type == NodeType.Command)
                {
                    var executionHeader = new Label("Execution Info");
                    executionHeader.AddToClassList("detail-section-header");
                    scrollView.Add(executionHeader);

                    string executionType = _selectedNode.IsSequenceCommand ? "Sequential" : (_selectedNode.IsParallelCommand ? "Parallel" : "Standard");

                    var executionTypeLabel = new Label($"Execution Type: {executionType}");
                    executionTypeLabel.AddToClassList("detail-property");

                    var executionOrderLabel = new Label($"Execution Order: {_selectedNode.ExecutionOrder}");
                    executionOrderLabel.AddToClassList("detail-property");

                    scrollView.Add(executionTypeLabel);
                    scrollView.Add(executionOrderLabel);
                }

                contentContainer.Add(scrollView);
            }
            else if (_selectedGroup != null)
            {
                // Create detail content for group
                var scrollView = new ScrollView();
                scrollView.style.flexGrow = 1;

                var groupTypeLabel = new Label($"Type: {_selectedGroup.Type}");
                groupTypeLabel.AddToClassList("detail-property");

                var groupNameLabel = new Label($"Name: {_selectedGroup.Name}");
                groupNameLabel.AddToClassList("detail-property");

                var nodeCountLabel = new Label($"Node Count: {_selectedGroup.NodeIds.Count}");
                nodeCountLabel.AddToClassList("detail-property");

                scrollView.Add(groupTypeLabel);
                scrollView.Add(groupNameLabel);
                scrollView.Add(nodeCountLabel);

                // Sequence specific information
                if (_selectedGroup.IsSequenceGroup || _selectedGroup.IsParallelGroup)
                {
                    var executionHeader = new Label("Execution Info");
                    executionHeader.AddToClassList("detail-section-header");
                    scrollView.Add(executionHeader);

                    string executionType = _selectedGroup.IsSequenceGroup ? "Sequential" : "Parallel";
                    var executionTypeLabel = new Label($"Execution Type: {executionType}");
                    executionTypeLabel.AddToClassList("detail-property");

                    scrollView.Add(executionTypeLabel);
                }

                contentContainer.Add(scrollView);
            }
            else
            {
                // No selection
                var noSelectionLabel = new Label("No selection. " +
                                                 "Click on a node or group" +
                                                 " to view details.");
                noSelectionLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                noSelectionLabel.style.marginTop = 20;

                contentContainer.Add(noSelectionLabel);
            }
        }

        private void SetStatusText(string text)
        {
            var statusLabel = _statusBarContainer.Q<Label>();
            if (statusLabel != null)
            {
                statusLabel.text = text;
            }
        }

        private void OnGUI()
        {
            // Draw detail panel if needed
            if (_detailPanel != null && _showDetailPanel)
            {
                _detailPanel.Draw();
            }
        }

        private void OnDisable()
        {
            // Clean up when window is disabled
            CleanupResources();
        }

        private void OnDestroy()
        {
            // Final cleanup when window is destroyed
            CleanupResources();

            // Force GC to clean up any lingering resources
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        private void CleanupResources()
        {
            try
            {
                // Clear diagram view
                if (_diagramView != null)
                {
                    _diagramView.OnNodeSelected = null;

                    if (_contentContainer != null)
                    {
                        _contentContainer.Remove(_diagramView);
                    }

                    _diagramView = null;
                }

                // Clear detail panel
                if (_detailPanel != null)
                {
                    _detailPanel.Hide();
                    _detailPanel = null;
                }

                // Clear graph data
                _currentGraph = null;

                // Clear selections
                _selectedNode = null;
                _selectedGroup = null;

                // Clear analyzer
                _analyzer = null;

                // Clear simulation controller
                _simulationController = null;

                // Set status text to indicate cleanup
                SetStatusText("Resources cleaned up.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error during resource cleanup: {ex.Message}\n{ex.StackTrace}");
            }
        }
    }
}
#endif