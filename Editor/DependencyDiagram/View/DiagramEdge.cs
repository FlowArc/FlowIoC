#if UNITY_2019_4_OR_NEWER
using System;
using FlowIoC.Editor.DependencyDiagram.Data;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;

namespace FlowIoC.Editor.DependencyDiagram.View
{
    public class DiagramEdgeView : Edge
    {
        public DiagramEdge EdgeData { get; private set; }
        private const float EDGE_BASE_OPACITY = 0.6f; // Reduced from 1.0f
        private const float EDGE_HOVER_OPACITY = 0.9f;
        private const float EDGE_SELECTED_OPACITY = 1.0f;
        
        public DiagramEdgeView()
        {
            // Temel edge stilini ayarla
            AddToClassList("diagram-edge");
            
            // Görünürlüğü garantile ama saydamlık ekle
            style.visibility = Visibility.Visible;
            style.display = DisplayStyle.Flex;
            style.opacity = EDGE_BASE_OPACITY; // Reduced opacity for better clarity
            
            // Edge'e tıklandığında seçili olarak işaretle
            RegisterCallback<MouseDownEvent>(OnMouseDown);
            // Add hover effect
            RegisterCallback<MouseEnterEvent>(OnMouseEnter);
            RegisterCallback<MouseLeaveEvent>(OnMouseExit);
        }
        
        // Add the constructor needed for the DiagramView class
        public DiagramEdgeView(DiagramEdge edgeData, EnhancedDiagramNodeView sourceNode, EnhancedDiagramNodeView targetNode, string edgeLabel = null)
        {
            try
            {
                EdgeData = edgeData;
                
                // Add to CSS class based on edge type
                this.AddToClassList(edgeData.Type.ToString().ToLower() + "-edge");
                
                // Add special styling for sequential and parallel commands
                if (edgeData.Type == EdgeType.SequentialCommand)
                {
                    this.AddToClassList("sequential-command");
                    this.tooltip = $"Sequential Execution: {edgeData.Label} - Commands execute in order";
                }
                else if (edgeData.Type == EdgeType.ParallelCommand)
                {
                    this.AddToClassList("parallel-command");
                    this.tooltip = $"Parallel Execution: {edgeData.Label} - Commands execute simultaneously";
                }
                else
                {
                    this.tooltip = edgeData.Label;
                }
                
                // Ensure source and target nodes and their ports are valid
                if (sourceNode == null || targetNode == null)
                {
                    Debug.LogError($"Invalid edge: source or target node is null for edge {edgeData.Id}");
                    return;
                }
                
                // Ensure nodes have ports
                if (sourceNode._outputPorts == null || sourceNode._outputPorts.Count == 0 ||
                    targetNode._inputPorts == null || targetNode._inputPorts.Count == 0)
                {
                    Debug.LogError($"Invalid edge: source or target node has no ports for edge {edgeData.Id}");
                    return;
                }
                
                // Connect the nodes via ports
                output = sourceNode._outputPorts[0];
                input = targetNode._inputPorts[0];
                
                // Ensure visibility but with some transparency
                style.visibility = Visibility.Visible;
                style.display = DisplayStyle.Flex;
                style.opacity = EDGE_BASE_OPACITY; // Reduced opacity
                
                // Set edge properties only if edgeControl exists
                // This will be properly initialized by Unity later
                EditorApplication.delayCall += () => {
                    if (edgeControl != null)
                    {
                        edgeControl.edgeWidth = EdgeData.Type == EdgeType.SequentialCommand || 
                                              EdgeData.Type == EdgeType.ParallelCommand ? 3 : 2;
                    }
                };
                
                // Edge'e tıklandığında seçili olarak işaretle
                RegisterCallback<MouseDownEvent>(OnMouseDown);
                // Add hover effect
                RegisterCallback<MouseEnterEvent>(OnMouseEnter);
                RegisterCallback<MouseLeaveEvent>(OnMouseExit);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error creating edge view: {ex.Message}\n{ex.StackTrace}");
            }
        }
        
        public void Initialize(DiagramEdge edgeData, Port outputPort, Port inputPort)
        {
            try
            {
                EdgeData = edgeData;
                
                // Edge rengini ayarla - edgeColor yerine stil sınıfına göre renk belirle
                EdgeData.Color.a = EDGE_BASE_OPACITY; // Reduced opacity
                this.AddToClassList(EdgeData.Type.ToString().ToLower() + "-edge");
                
                // Add special styling for sequential and parallel commands
                if (EdgeData.Type == EdgeType.SequentialCommand)
                {
                    this.AddToClassList("sequential-command");
                    this.tooltip = $"Sequential Execution: {EdgeData.Label} - Commands execute in order";
                }
                else if (EdgeData.Type == EdgeType.ParallelCommand)
                {
                    this.AddToClassList("parallel-command");
                    this.tooltip = $"Parallel Execution: {EdgeData.Label} - Commands execute simultaneously";
                }
                else
                {
                    this.tooltip = EdgeData.Label;
                }
                
                // Düğüm bağlantılarını ayarla
                output = outputPort;
                input = inputPort;
                
                // Görünürlüğü garantile ama saydamlık ekle
                style.visibility = Visibility.Visible;
                style.display = DisplayStyle.Flex;
                style.opacity = EDGE_BASE_OPACITY; // Reduced opacity
                
                // Set edge control with better routing
                if (edgeControl != null)
                {
                    edgeControl.edgeWidth = EdgeData.Type == EdgeType.SequentialCommand || 
                                          EdgeData.Type == EdgeType.ParallelCommand ? 3 : 2;
                }
                
                // Etiket ekle (eğer varsa) - SetTitle metodu olmadığı için bu kısmı atlıyoruz
                if (!string.IsNullOrEmpty(EdgeData.Label))
                {
                    try
                    {
                        // Unity 2019'da edge'in title özelliği varsa kullan, yoksa logger ile bir mesaj kaydet
                        // Ancak hata vermesini engelle
                        Debug.Log($"Edge {EdgeData.Id} has label: {EdgeData.Label}");
                    }
                    catch (Exception labelEx)
                    {
                        Debug.LogWarning($"Cannot set edge label: {labelEx.Message}, but this is not critical.");
                    }
                }
                
                // Bağlantının doğru kurulduğunu belirt
                Debug.Log($"Edge {EdgeData.Id} initialized successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error initializing edge {EdgeData?.Id}: {ex.Message}");
            }
        }
        
        private void OnMouseDown(MouseDownEvent evt)
        {
            try
            {
                // Edge seçildi
                selected = true;
                
                // Increase visibility when selected
                style.opacity = EDGE_SELECTED_OPACITY;
                
                // Yeniden çizilmesi için işaretle
                MarkDirtyRepaint();
                
                // Olayın ilerlemesini durdur
                evt.StopPropagation();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Error in edge mouse down: {ex.Message}");
            }
        }
        
        private void OnMouseEnter(MouseEnterEvent evt)
        {
            // Highlight edge on hover
            style.opacity = EDGE_HOVER_OPACITY;
            MarkDirtyRepaint();
        }
        
        private void OnMouseExit(MouseLeaveEvent evt)
        {
            // Return to normal opacity if not selected
            if (!selected)
            {
                style.opacity = EDGE_BASE_OPACITY;
            }
            MarkDirtyRepaint();
        }
        
        public override void OnPortChanged(bool isInput)
        {
            try
            {
                base.OnPortChanged(isInput);
                
                // Port değiştiğinde edge'i yeniden çizdir
                MarkDirtyRepaint();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Error in edge port changed: {ex.Message}");
            }
        }
        
        public override bool ContainsPoint(Vector2 localPoint)
        {
            try
            {
                // Edge'e tıklamayı daha kolaylaştırmak için algılama mesafesini arttır
                const float hitDistance = 10f;
                return base.ContainsPoint(localPoint) || HitTest(localPoint, hitDistance);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Error in edge contains point: {ex.Message}");
                return base.ContainsPoint(localPoint);
            }
        }
        
        private bool HitTest(Vector2 point, float maxDistance)
        {
            try
            {
                // Çizgi üzerindeki en yakın noktanın uzaklığını kontrol et
                if (output?.node != null && input?.node != null)
                {
                    var outputPos = output.node.GetPosition().center;
                    var inputPos = input.node.GetPosition().center;
                    
                    // En yakın noktayı bul
                    var projection = ProjectPointOnLine(point, outputPos, inputPos);
                    var distance = Vector2.Distance(point, projection);
                    
                    return distance <= maxDistance;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Error in edge hit test: {ex.Message}");
                return false;
            }
        }
        
        private Vector2 ProjectPointOnLine(Vector2 point, Vector2 lineStart, Vector2 lineEnd)
        {
            var lineDirection = lineEnd - lineStart;
            var lineLength = lineDirection.magnitude;
            lineDirection /= lineLength;
            
            var fromStartToPoint = point - lineStart;
            var dot = Vector2.Dot(fromStartToPoint, lineDirection);
            
            dot = Mathf.Clamp(dot, 0f, lineLength);
            
            return lineStart + lineDirection * dot;
        }
    }
}
#endif 