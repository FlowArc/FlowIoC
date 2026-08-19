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
    public class EnhancedDiagramNodeView : Node
    {
        public DiagramNode NodeData { get; private set; }
        public Action<EnhancedDiagramNodeView> OnNodeSelected { get; set; }
        
        public readonly List<Port> _inputPorts = new List<Port>();
        public readonly List<Port> _outputPorts = new List<Port>();
        
        private Label _descriptionLabel;
        
        public EnhancedDiagramNodeView(DiagramNode nodeData)
        {
            NodeData = nodeData;
            title = nodeData.Name;
            viewDataKey = nodeData.Id;
            
            // Temel stil
            style.width = 180;
            style.minWidth = 160;
            
            // Arkaplan rengini ayarla
            Color bgColor = GetNodeBackgroundColor();
            style.backgroundColor = bgColor;
            
            // Boşlukları ayarla
            style.paddingLeft = 8;
            style.paddingRight = 8;
            style.paddingTop = 8;
            style.paddingBottom = 8;
            
            // Kenar ve arkaplan stili
            Color borderColor = GetNodeBorderColor();
            style.borderBottomColor = borderColor;
            style.borderLeftColor = borderColor;
            style.borderRightColor = borderColor;
            style.borderTopColor = borderColor;
            style.borderBottomWidth = 1;
            style.borderLeftWidth = 1;
            style.borderRightWidth = 1;
            style.borderTopWidth = 2; // Üst kenara vurgu
            style.borderBottomLeftRadius = 8;
            style.borderBottomRightRadius = 8;
            style.borderTopLeftRadius = 8;
            style.borderTopRightRadius = 8;
            
            // Gölge efekti ekle
            style.unityTextAlign = TextAnchor.MiddleCenter;
            style.unityFontStyleAndWeight = FontStyle.Bold;
            
            // Başlık rengi
            var titleLabel = titleContainer.Q<Label>();
            if (titleLabel != null)
            {
                titleLabel.style.fontSize = 14;
                titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                titleLabel.style.color = Color.white;
                
                // Başlık arkaplan rengi
                titleContainer.style.backgroundColor = GetNodeTypeColor();
                titleContainer.style.paddingTop = 5;
                titleContainer.style.paddingBottom = 5;
                titleContainer.style.borderTopLeftRadius = 6;
                titleContainer.style.borderTopRightRadius = 6;
                
                // Taşmayı engelle
                titleLabel.style.overflow = Overflow.Hidden;
                titleLabel.style.textOverflow = TextOverflow.Ellipsis;
                titleLabel.style.whiteSpace = WhiteSpace.NoWrap;
            }
            
            // Görünürlüğü garantile
            visible = true;
            style.visibility = Visibility.Visible;
            style.display = DisplayStyle.Flex;
            style.opacity = 1;
            
            // Port oluştur
            CreatePorts();
            
            // İçerik ekle
            AddNodeContent();
            
            // Tip simgesini ekle
            AddNodeTypeIcon();
            
            // Seçim olayını ekle
            RegisterCallback<MouseDownEvent>(OnNodeMouseDown);
        }
        
        private void CreatePorts()
        {
            // Giriş portu
            var inputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(bool));
            if (inputPort != null)
            {
                inputPort.portName = "In";
                
                // Port etiketi stilini ayarla
                var inputPortLabel = inputPort.Q<Label>("type");
                if (inputPortLabel != null)
                {
                    inputPortLabel.AddToClassList("port-label");
                }
                
                // Port arka plan rengini koyu yap
                var inputConnector = inputPort.Q("connector");
                if (inputConnector != null)
                {
                    inputConnector.style.backgroundColor = new Color(0.25f, 0.25f, 0.25f);
                }
                
                inputContainer.Add(inputPort);
                _inputPorts.Add(inputPort);
                
                // Port görünürlüğünü garantile
                inputPort.visible = true;
                inputPort.style.visibility = Visibility.Visible;
                inputPort.style.display = DisplayStyle.Flex;
                inputPort.style.opacity = 1;
            }
            
            // Çıkış portu
            var outputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(bool));
            if (outputPort != null)
            {
                outputPort.portName = "Out";
                
                // Port etiketi stilini ayarla
                var outputPortLabel = outputPort.Q<Label>("type");
                if (outputPortLabel != null)
                {
                    outputPortLabel.AddToClassList("port-label");
                }
                
                // Port arka plan rengini koyu yap
                var outputConnector = outputPort.Q("connector");
                if (outputConnector != null)
                {
                    outputConnector.style.backgroundColor = new Color(0.25f, 0.25f, 0.25f);
                }
                
                outputContainer.Add(outputPort);
                _outputPorts.Add(outputPort);
                
                // Port görünürlüğünü garantile
                outputPort.visible = true;
                outputPort.style.visibility = Visibility.Visible;
                outputPort.style.display = DisplayStyle.Flex;
                outputPort.style.opacity = 1;
            }
        }
        
        private void AddNodeContent()
        {
            try
            {
                // Açıklama etiketi ekle
                _descriptionLabel = new Label(GetNodeDescription());
                _descriptionLabel.style.fontSize = 11;
                _descriptionLabel.style.unityFontStyleAndWeight = FontStyle.Normal;
                _descriptionLabel.style.color = new Color(0.8f, 0.8f, 0.8f);
                _descriptionLabel.style.whiteSpace = WhiteSpace.Normal;
                _descriptionLabel.style.maxWidth = 160;
                _descriptionLabel.style.marginTop = 5;
                _descriptionLabel.style.marginBottom = 5;
                
                extensionContainer.Add(_descriptionLabel);
                
                // Add sequence/parallel badges for command nodes
                if (NodeData.Type == NodeType.Command)
                {
                    if (NodeData.IsSequenceCommand)
                    {
                        AddChip($"Seq #{NodeData.ExecutionOrder}", new Color(0.9f, 0.1f, 0.1f), "Sequential Command Execution Order");
                    }
                    else if (NodeData.IsParallelCommand)
                    {
                        AddChip($"Par #{NodeData.ExecutionOrder}", new Color(0.8f, 0.1f, 0.8f), "Parallel Command Execution Order");
                    }
                }
                // Add group type badges for signal nodes
                else if (NodeData.Type == NodeType.Signal && NodeData.Name.StartsWith("Group_"))
                {
                    if (NodeData.IsSequenceCommand)
                    {
                        AddChip("Sequence", new Color(0.9f, 0.1f, 0.1f), "Commands execute in sequence");
                    }
                    else if (NodeData.IsParallelCommand)
                    {
                        AddChip("Parallel", new Color(0.8f, 0.1f, 0.8f), "Commands execute in parallel");
                    }
                }
                
                RefreshExpandedState();
                
                // Düğüm tipine göre özel içerik ekle
                switch (NodeData.Type)
                {
                    case NodeType.Context:
                        // Display actual bindings from NodeData metadata as semicolon-separated strings
                        if (NodeData.Metadata.TryGetValue("SignalBindings", out string signalBindingsStr) && !string.IsNullOrEmpty(signalBindingsStr))
                        {
                            string[] signalBindings = signalBindingsStr.Split(';');
                            if (signalBindings.Length > 0)
                            {
                                AddBindingGroup("Signal Bindings", new Color(0.8f, 0.2f, 0.2f, 0.8f), 
                                    "Binds signals to commands and mediators", signalBindings);
                            }
                        }
                        
                        if (NodeData.Metadata.TryGetValue("InjectionBindings", out string injectionBindingsStr) && !string.IsNullOrEmpty(injectionBindingsStr))
                        {
                            string[] injectionBindings = injectionBindingsStr.Split(';');
                            if (injectionBindings.Length > 0)
                            {
                                AddBindingGroup("Injection Bindings", new Color(0.2f, 0.4f, 0.8f, 0.8f), 
                                    "Configures dependency injection setup", injectionBindings);
                            }
                        }
                        
                        if (NodeData.Metadata.TryGetValue("MediationBindings", out string mediationBindingsStr) && !string.IsNullOrEmpty(mediationBindingsStr))
                        {
                            string[] mediationBindings = mediationBindingsStr.Split(';');
                            if (mediationBindings.Length > 0)
                            {
                                AddBindingGroup("Mediation Bindings", new Color(0.6f, 0.2f, 0.6f, 0.8f), 
                                    "Connects views with mediators", mediationBindings);
                            }
                        }
                        
                        if (NodeData.Metadata.TryGetValue("CommandBindings", out string commandBindingsStr) && !string.IsNullOrEmpty(commandBindingsStr))
                        {
                            string[] commandBindings = commandBindingsStr.Split(';');
                            if (commandBindings.Length > 0)
                            {
                                AddBindingGroup("Command Bindings", new Color(0.4f, 0.6f, 0.2f, 0.8f), 
                                    "Maps commands to signals", commandBindings);
                            }
                        }
                        
                        // Lifecycle methods
                        AddChip("Setup", new Color(0.2f, 0.6f, 0.4f, 0.8f), "Initializes the context");
                        AddChip("Launch", new Color(0.6f, 0.4f, 0.2f, 0.8f), "Starts the context execution");
                        break;
                    
                    case NodeType.Signal:
                        AddBindingGroup("Properties", new Color(0.8f, 0.2f, 0.2f, 0.8f), "Signal properties and data", new[]
                        {
                            "EventData",
                            "Timestamp"
                        });
                        break;
                    
                    case NodeType.Command:
                        AddBindingGroup("Methods", new Color(0.4f, 0.8f, 0.2f, 0.8f), "Command methods", new[]
                        {
                            "Execute()",
                            "OnComplete()",
                            "OnError()"
                        });
                        
                        AddBindingGroup("Dependencies", new Color(0.5f, 0.7f, 0.3f, 0.8f), "Required injections", new[]
                        {
                            "Services",
                            "Other Commands"
                        });
                        break;
                    
                    case NodeType.Mediator:
                        AddBindingGroup("Signal Handlers", new Color(0.8f, 0.2f, 0.8f, 0.8f), "Methods that handle signals", new[]
                        {
                            "OnSignalA()",
                            "OnSignalB()"
                        });
                        
                        AddBindingGroup("View Updates", new Color(0.7f, 0.3f, 0.7f, 0.8f), "Methods that update views", new[]
                        {
                            "UpdateViewState()",
                            "RefreshUI()"
                        });
                        break;
                    
                    case NodeType.View:
                        AddBindingGroup("Components", new Color(0.2f, 0.8f, 0.8f, 0.8f), "UI components", new[]
                        {
                            "Buttons",
                            "Panels",
                            "Text Elements"
                        });
                        
                        AddBindingGroup("Events", new Color(0.3f, 0.7f, 0.7f, 0.8f), "User interaction events", new[]
                        {
                            "OnClick()",
                            "OnValueChanged()"
                        });
                        break;
                    
                    case NodeType.Injectable:
                        AddBindingGroup("Services", new Color(0.8f, 0.6f, 0.2f, 0.8f), "Service methods", new[]
                        {
                            "Initialize()",
                            "Process()",
                            "GetData()"
                        });
                        
                        AddBindingGroup("Dependencies", new Color(0.7f, 0.5f, 0.3f, 0.8f), "Required injections", new[]
                        {
                            "Other Services",
                            "Configuration"
                        });
                        break;
                }
                
                // Genişletilmiş durumu uygula
                expanded = true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error adding node content: {e.Message}\n{e.StackTrace}");
            }
        }
        
        private void AddBindingGroup(string title, Color color, string tooltip, string[] items)
        {
            try
            {
                // Ana grup container
                var groupContainer = new VisualElement();
                groupContainer.style.marginBottom = 8;
                
                // Başlık elementi
                var headerContainer = new VisualElement();
                headerContainer.style.flexDirection = FlexDirection.Row;
                headerContainer.style.alignItems = Align.Center;
                headerContainer.style.backgroundColor = color;
                headerContainer.style.borderTopLeftRadius = 8;
                headerContainer.style.borderTopRightRadius = 8;
                headerContainer.style.paddingLeft = 8;
                headerContainer.style.paddingRight = 8;
                headerContainer.style.paddingTop = 4;
                headerContainer.style.paddingBottom = 4;
                
                // Başlık etiketi
                var titleLabel = new Label(title);
                titleLabel.style.fontSize = 11;
                titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                
                // Başlık rengini ayarla
                Color.RGBToHSV(color, out float h, out float s, out float v);
                titleLabel.style.color = v < 0.5f ? new Color(0.9f, 0.9f, 0.9f) : new Color(0.1f, 0.1f, 0.1f);
                
                headerContainer.Add(titleLabel);
                
                // Tooltip ekle
                if (!string.IsNullOrEmpty(tooltip))
                {
                    headerContainer.tooltip = tooltip;
                }
                
                groupContainer.Add(headerContainer);
                
                // İçerik container
                var contentContainer = new VisualElement();
                contentContainer.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.7f);
                contentContainer.style.borderBottomLeftRadius = 8;
                contentContainer.style.borderBottomRightRadius = 8;
                contentContainer.style.paddingLeft = 8;
                contentContainer.style.paddingRight = 8;
                contentContainer.style.paddingTop = 6;
                contentContainer.style.paddingBottom = 6;
                
                // Öğeleri ekle
                foreach (var item in items)
                {
                    var itemLabel = new Label("• " + item);
                    itemLabel.style.fontSize = 10;
                    itemLabel.style.unityFontStyleAndWeight = FontStyle.Normal;
                    itemLabel.style.color = new Color(0.8f, 0.8f, 0.8f);
                    itemLabel.style.marginBottom = 2;
                    contentContainer.Add(itemLabel);
                }
                
                groupContainer.Add(contentContainer);
                extensionContainer.Add(groupContainer);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error adding binding group {title}: {ex.Message}");
            }
        }
        
        private void AddNodeTypeIcon()
        {
            try
            {
                // Tip ikonu oluştur
                var iconContainer = new VisualElement();
                iconContainer.style.position = Position.Absolute;
                iconContainer.style.top = 0;
                iconContainer.style.right = 0;
                iconContainer.style.width = 24;
                iconContainer.style.height = 24;
                iconContainer.style.marginRight = 5;
                iconContainer.style.marginTop = 5;
                
                var icon = new VisualElement();
                icon.style.width = 16;
                icon.style.height = 16;
                icon.style.borderBottomLeftRadius = 8;
                icon.style.borderBottomRightRadius = 8;
                icon.style.borderTopLeftRadius = 8;
                icon.style.borderTopRightRadius = 8;
                
                // Tip rengini ayarla
                Color iconColor = GetNodeTypeColor();
                icon.style.backgroundColor = iconColor;
                
                iconContainer.Add(icon);
                Add(iconContainer);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error adding node type icon for {NodeData.Name}: {ex.Message}");
            }
        }
        
        private void AddChip(string text, Color color, string tooltip = null)
        {
            try
            {
                var chipContainer = new VisualElement();
                chipContainer.style.flexDirection = FlexDirection.Row;
                chipContainer.style.alignItems = Align.Center;
                chipContainer.style.backgroundColor = color;
                chipContainer.style.borderBottomLeftRadius = 12;
                chipContainer.style.borderBottomRightRadius = 12;
                chipContainer.style.borderTopLeftRadius = 12;
                chipContainer.style.borderTopRightRadius = 12;
                chipContainer.style.paddingLeft = 8;
                chipContainer.style.paddingRight = 8;
                chipContainer.style.paddingTop = 3;
                chipContainer.style.paddingBottom = 3;
                chipContainer.style.marginTop = 4;
                chipContainer.style.marginBottom = 4;
                chipContainer.style.alignSelf = Align.FlexStart;
                
                // Add a small shadow for depth
                chipContainer.style.unitySliceLeft = 2;
                chipContainer.style.unitySliceRight = 2;
                chipContainer.style.unitySliceTop = 2;
                chipContainer.style.unitySliceBottom = 2;
                
                var chipLabel = new Label(text);
                chipLabel.style.fontSize = 10;
                chipLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                
                // Arka plana bağlı olarak yazı rengini ayarla (koyu arka planlarda açık yazı, açık arka planlarda koyu yazı)
                Color.RGBToHSV(color, out float h, out float s, out float v);
                chipLabel.style.color = v < 0.5f ? new Color(0.9f, 0.9f, 0.9f) : new Color(0.1f, 0.1f, 0.1f);
                
                chipContainer.Add(chipLabel);
                extensionContainer.Add(chipContainer);
                
                // Tooltip ekle (varsa)
                if (!string.IsNullOrEmpty(tooltip))
                {
                    chipContainer.tooltip = tooltip;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error adding chip for {NodeData.Name}: {ex.Message}");
            }
        }
        
        // Node tipi rengini al
        private Color GetNodeTypeColor()
        {
            switch (NodeData.Type)
            {
                case NodeType.Context:
                    return new Color(0.2f, 0.4f, 0.6f, 0.9f);
                case NodeType.Signal:
                    return new Color(0.6f, 0.2f, 0.2f, 0.9f);
                case NodeType.Command:
                    return new Color(0.4f, 0.6f, 0.2f, 0.9f);
                case NodeType.View:
                    return new Color(0.2f, 0.6f, 0.6f, 0.9f);
                case NodeType.Mediator:
                    return new Color(0.6f, 0.2f, 0.6f, 0.9f);
                case NodeType.Injectable:
                    return new Color(0.6f, 0.4f, 0.2f, 0.9f);
                default:
                    return Color.gray;
            }
        }
        
        // Node açıklamasını al
        private string GetNodeDescription()
        {
            // If it's a command with sequence or parallel execution, show more details
            if (NodeData.Type == NodeType.Command && (NodeData.IsSequenceCommand || NodeData.IsParallelCommand))
            {
                var sequenceType = NodeData.IsSequenceCommand ? "Sequence" : "Parallel";
                
                // Check for detailed sequence info
                if (NodeData.Metadata.TryGetValue("SequenceInfo", out var sequenceInfo))
                {
                    return $"{NodeData.TypeName}\n{sequenceType} Command\n{sequenceInfo}";
                }
                
                // Fallback to basic info
                var orderInfo = $"Order: {NodeData.ExecutionOrder}";
                var triggerInfo = "";
                
                if (NodeData.Metadata.TryGetValue("TriggerSignal", out var signalName))
                {
                    triggerInfo = $"\nTriggered by: {signalName}";
                }
                else if (NodeData.Metadata.TryGetValue("TriggerButton", out var buttonName))
                {
                    triggerInfo = $"\nTriggered by button: {buttonName}";
                }
                else if (NodeData.Metadata.TryGetValue("TriggerGroup", out var groupName))
                {
                    triggerInfo = $"\nTriggered by group: {groupName}";
                }
                
                return $"{NodeData.TypeName}\n{sequenceType} Command\n{orderInfo}{triggerInfo}";
            }
            
            // If it's a signal for a group, show group info
            if (NodeData.Type == NodeType.Signal && NodeData.Name.StartsWith("Group_"))
            {
                var groupName = NodeData.Name.Substring(6); // Remove "Group_" prefix
                string executionInfo = "";
                
                if (NodeData.IsSequenceCommand)
                    executionInfo = "Sequential Execution";
                else if (NodeData.IsParallelCommand)
                    executionInfo = "Parallel Execution";
                    
                if (NodeData.Metadata.TryGetValue("PreviousGroup", out var prevGroup))
                {
                    return $"Command Group: {groupName}\n{executionInfo}\nTriggered after: {prevGroup}";
                }
                
                return $"Command Group: {groupName}\n{executionInfo}";
            }
            
            switch (NodeData.Type)
            {
                case NodeType.Context:
                    return $"{NodeData.TypeName}\nContext Class\nManages application flow";
                case NodeType.Signal:
                    return $"{NodeData.TypeName}\nEvent Signal\nMessage between components";
                case NodeType.Command:
                    return $"{NodeData.TypeName}\nCommand Class\nBusiness logic handler";
                case NodeType.View:
                    return $"{NodeData.TypeName}\nView Component\nUser interface element";
                case NodeType.Mediator:
                    return $"{NodeData.TypeName}\nMediator Class\nConnects views to signals";
                case NodeType.Injectable:
                    return $"{NodeData.TypeName}\nInjectable Service\nShared functionality";
                default:
                    return NodeData.TypeName;
            }
        }
        
        private void OnNodeMouseDown(MouseDownEvent evt)
        {
            OnNodeSelected?.Invoke(this);
        }
        
        // Node arkaplan rengini al
        private Color GetNodeBackgroundColor()
        {
            Color typeColor = GetNodeTypeColor();
            // Daha koyu bir arkaplan için rengi koyulaştır
            return new Color(
                typeColor.r * 0.8f + 0.2f,
                typeColor.g * 0.8f + 0.2f,
                typeColor.b * 0.8f + 0.2f,
                0.9f
            );
        }
        
        // Node kenar rengini al
        private Color GetNodeBorderColor()
        {
            Color typeColor = GetNodeTypeColor();
            // Daha koyu bir kenar için rengi koyulaştır
            return new Color(
                typeColor.r * 0.8f, 
                typeColor.g * 0.8f, 
                typeColor.b * 0.8f, 
                0.9f
            );
        }
    }
}
#endif 