using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using FlowIoC.BaseModule.Contexts;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.Signals;
using FlowIoC.BaseModule.ViewsMediators.Mediator;
using FlowIoC.BaseModule.ViewsMediators.View;
using FlowIoC.Editor.DependencyDiagram.Data;
using UnityEditor;
using UnityEngine;

namespace FlowIoC.Editor.DependencyDiagram.FlowIoC.Editor.DependencyDiagram.Analyzer
{
    public class ContextDependencyAnalyzer
    {
        private readonly Dictionary<Type, DiagramNode> _nodeCache = new Dictionary<Type, DiagramNode>();
        private readonly Dictionary<string, DiagramNode> _signalCache = new Dictionary<string, DiagramNode>();
        private readonly HashSet<string> _processedBindings = new HashSet<string>();
        private readonly Dictionary<string, Type> _commandTypes = new Dictionary<string, Type>();
        
        public DiagramGraph AnalyzeContext(Type contextType)
        {
            if (contextType == null || !typeof(IContext).IsAssignableFrom(contextType))
            {
                Debug.LogError($"Type {contextType?.Name ?? "null"} is not a valid Context type");
                return null;
            }
            
            // Completely reset all caches and state
            _nodeCache.Clear();
            _signalCache.Clear();
            _processedBindings.Clear();
            _commandTypes.Clear();
            
            // Forcefully clean up memory
            GC.Collect();
            
            Debug.Log($"Analyzing context: {contextType.Name}");
            
            // Skip problematic assemblies throughout the analysis
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                // Skip problematic Azure/Microsoft assemblies completely
                if (assembly.FullName.Contains("Azure.") || 
                    assembly.FullName.Contains("Storage") ||
                    assembly.FullName.Contains("Microsoft."))
                {
                    continue;
                }
            }
            
            // Önce Command tipleri bulunsun
            FindAllCommandTypes();
            
            var graph = new DiagramGraph(
                Guid.NewGuid().ToString(),
                $"{contextType.Name} Diagram",
                DiagramViewType.CategoryView
            );
            
            // Add groups
            var contextGroup = graph.AddGroup(
                "group_contexts",
                "Contexts",
                NodeType.Context,
                DiagramNodeGroup.GetColorForNodeType(NodeType.Context)
            );
            
            var signalGroup = graph.AddGroup(
                "group_signals",
                "Signals",
                NodeType.Signal,
                DiagramNodeGroup.GetColorForNodeType(NodeType.Signal)
            );
            
            var commandGroup = graph.AddGroup(
                "group_commands",
                "Commands",
                NodeType.Command,
                DiagramNodeGroup.GetColorForNodeType(NodeType.Command)
            );
            
            var viewGroup = graph.AddGroup(
                "group_views",
                "Views",
                NodeType.View,
                DiagramNodeGroup.GetColorForNodeType(NodeType.View)
            );
            
            var mediatorGroup = graph.AddGroup(
                "group_mediators",
                "Mediators",
                NodeType.Mediator,
                DiagramNodeGroup.GetColorForNodeType(NodeType.Mediator)
            );
            
            var injectableGroup = graph.AddGroup(
                "group_injectables",
                "Injectables",
                NodeType.Injectable,
                DiagramNodeGroup.GetColorForNodeType(NodeType.Injectable)
            );
            
            // Create context node
            var contextNode = CreateNodeForType(graph, contextType, NodeType.Context);
            graph.AddNodeToGroup(contextNode.Id, contextGroup.Id);
            
            // We'll collect the actual bindings during analysis and store them at the end
            
            // Analyze context methods for bindings
            AnalyzeSignalBindings(graph, contextType, contextNode, signalGroup);
            AnalyzeCommandBindings(graph, contextType, contextNode, commandGroup);
            AnalyzeMediationBindings(graph, contextType, contextNode, mediatorGroup, viewGroup);
            AnalyzeInjectionBindings(graph, contextType, contextNode, injectableGroup);
            
            // Analyze and create nodes for sub-contexts if any
            AnalyzeSubContexts(graph, contextType, contextNode, contextGroup);
            
            return graph;
        }
        
        private void FindAllCommandTypes()
        {
            try
            {
                Debug.Log("Starting to find all command types...");
                var commandTypes = new List<Type>();
                int totalCommands = 0;
                
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    try
                    {
                        // Azure Storage hatalarını önlemek için bazı assembly'leri atla
                        if (assembly.FullName.Contains("Azure.") || 
                            assembly.FullName.Contains("Storage") ||
                            assembly.FullName.Contains("Microsoft.") ||
                            assembly.FullName.Contains("System.") ||
                            assembly.FullName.Contains("Unity.") ||
                            assembly.FullName.Contains("Mono."))
                        {
                            continue;
                        }
                        
                        // Tipleri güvenli şekilde al
                        Type[] types = null;
                        try
                        {
                            types = assembly.GetTypes();
                        }
                        catch (ReflectionTypeLoadException ex)
                        {
                            // Yüklenebilen tipleri kullan
                            types = ex.Types.Where(t => t != null).ToArray();
                        }
                        catch (Exception)
                        {
                            // Diğer assembly hatalarını görmezden gel
                            continue;
                        }
                        
                        if (types != null)
                        {
                            int assemblyCommandCount = 0;
                            
                            foreach (var type in types)
                            {
                                try
                                {
                                    if (type != null && 
                                        !type.IsInterface && 
                                        !type.IsAbstract)
                                    {
                                        bool isCommand = false;
                                        
                                        try {
                                            isCommand = typeof(ICommand).IsAssignableFrom(type);
                                        }
                                        catch {
                                            // Type loading error, maybe try by name
                                            isCommand = type.Name.EndsWith("Command");
                                        }
                                        
                                        if (isCommand || (type.Name.EndsWith("Command") && !type.Name.Contains("CommandBinder")))
                                        {
                                            commandTypes.Add(type);
                                            assemblyCommandCount++;
                                            
                                            // Debug.Log için Command tipini daha detaylı göster
                                            Debug.Log($"Found command type: {type.FullName} in assembly {assembly.GetName().Name}");
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    // Bir tip için hata oluşursa sadece o tipi atla
                                    Debug.LogWarning($"Error checking type: {ex.Message}");
                                    continue;
                                }
                            }
                            
                            if (assemblyCommandCount > 0)
                            {
                                Debug.Log($"Found {assemblyCommandCount} command types in assembly {assembly.GetName().Name}");
                                totalCommands += assemblyCommandCount;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // Bir assembly için hata oluşursa devam et
                        Debug.LogWarning($"Error analyzing assembly {assembly.GetName().Name}: {ex.Message}");
                        continue;
                    }
                }
                
                // Topladığımız command tiplerini sözlüğe ekle
                foreach (var commandType in commandTypes)
                {
                    // Hem tip adıyla hem de tam adıyla ekle
                    _commandTypes[commandType.Name] = commandType;
                    _commandTypes[commandType.FullName] = commandType;
                }
                
                Debug.Log($"Command types analysis completed: Found {totalCommands} commands across all assemblies. Dictionary contains {_commandTypes.Count} entries.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error in FindAllCommandTypes: {ex.Message}");
            }
        }
        
        private DiagramNode CreateNodeForType(DiagramGraph graph, Type type, NodeType nodeType)
        {
            if (_nodeCache.TryGetValue(type, out var existingNode))
            {
                return existingNode;
            }
            
            var filePath = GetTypeFilePath(type);
            var node = graph.AddNode(
                Guid.NewGuid().ToString(),
                type.Name,
                type.FullName,
                filePath,
                nodeType
            );
            
            _nodeCache[type] = node;
            return node;
        }
        
        private DiagramNode GetOrCreateSignalNode(DiagramGraph graph, string signalName, DiagramNodeGroup signalGroup)
        {
            if (_signalCache.TryGetValue(signalName, out var existingNode))
            {
                return existingNode;
            }
            
            var signalNode = graph.AddNode(
                Guid.NewGuid().ToString(),
                signalName,
                signalName,
                string.Empty,
                NodeType.Signal
            );
            
            _signalCache[signalName] = signalNode;
            
            // Add to group if provided
            if (signalGroup != null)
            {
                graph.AddNodeToGroup(signalNode.Id, signalGroup.Id);
            }
            else if (graph.Groups.TryGetValue("group_signals", out var defaultSignalGroup))
            {
                // Otherwise try to add to default signal group
                graph.AddNodeToGroup(signalNode.Id, defaultSignalGroup.Id);
            }
            
            return signalNode;
        }
        
        private void AnalyzeSignalBindings(DiagramGraph graph, Type contextType, DiagramNode contextNode, DiagramNodeGroup signalGroup)
        {
            Debug.Log($"Analyzing Signal Bindings for {contextType.Name}");
            
            try
            {
                string methodBody = GetMethodSourceCode(contextType, "SignalBindings");
                if (string.IsNullOrEmpty(methodBody))
                {
                    Debug.Log($"No SignalBindings method found in {contextType.Name}");
                    return;
                }
                
                // Extract actual signal bindings from method body
                var signalBindingRegex = new Regex(@"(\w+)\s*=\s*InjectionBinderCrossContext\.Bind<(\w+)>\(\);", RegexOptions.Multiline);
                var matches = signalBindingRegex.Matches(methodBody);
                
                List<string> signalBindings = new List<string>();
                
                foreach (Match match in matches)
                {
                    if (match.Groups.Count >= 3)
                    {
                        string fieldName = match.Groups[1].Value;
                        string signalType = match.Groups[2].Value;
                        Debug.Log($"Found signal binding: {fieldName} = {signalType}");
                        
                        // Add to the signals list
                        signalBindings.Add($"{signalType}");
                        
                        // Create a signal node if it doesn't exist
                        var signalNode = GetOrCreateSignalNode(graph, signalType, signalGroup);
                        
                        // Connect context to signal
                        graph.AddEdge(
                            Guid.NewGuid().ToString(),
                            contextNode.Id,
                            signalNode.Id,
                            EdgeType.SignalBinding,
                            "Signal Binding"
                        );
                    }
                }
                
                // Store the bindings in context metadata
                if (signalBindings.Count > 0)
                {
                    contextNode.AddMetadata("SignalBindings", string.Join(";", signalBindings));
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error analyzing signal bindings: {ex.Message}");
            }
        }
        
        private void AnalyzeCommandBindings(DiagramGraph graph, Type contextType, DiagramNode contextNode, DiagramNodeGroup commandGroup)
        {
            Debug.Log($"Analyzing Command Bindings for {contextType.Name}");
            
            try
            {
                string methodBody = GetMethodSourceCode(contextType, "CommandBindings");
                if (string.IsNullOrEmpty(methodBody))
                {
                    Debug.Log($"No CommandBindings method found in {contextType.Name}");
                    return;
                }
                
                List<string> commandBindings = new List<string>();
                
                // Extract command bindings
                AnalyzeCommandBinderBindCalls(graph, contextNode, commandGroup, methodBody, commandBindings);
                AnalyzeCommandBinderBindGroupCalls(graph, contextNode, commandGroup, methodBody, commandBindings);
                AnalyzeSubButtonClickEventMap(graph, contextNode, commandGroup, methodBody, commandBindings);
                
                // Store the bindings in context metadata
                if (commandBindings.Count > 0)
                {
                    contextNode.AddMetadata("CommandBindings", string.Join(";", commandBindings));
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error analyzing command bindings: {ex.Message}");
            }
        }
        
        private void AnalyzeCommandUsageInContext(DiagramGraph graph, Type contextType, DiagramNode contextNode, DiagramNodeGroup commandGroup)
        {
            Debug.Log($"Analyzing command usage in context {contextType.Name}");
            
            // Context sınıfının kaynak kodunu al
            string contextCode = GetTypeSourceCode(contextType);
            if (string.IsNullOrEmpty(contextCode))
            {
                Debug.LogWarning($"Could not get source code for context {contextType.Name}");
                return;
            }
            
            // Tüm Command'ları içeren dikkat çekici bir log
            Debug.Log($"Available commands for analysis: {_commandTypes.Count}");
            
            // Command kullanımlarını bul (alan ve property enjeksiyonlar dışında)
            foreach (var commandEntry in _commandTypes)
            {
                var commandType = commandEntry.Value;
                string commandName = commandType.Name;
                
                // Command sınıfı context içinde kullanılıyor mu?
                if (contextCode.Contains(commandName))
                {
                    Debug.Log($"Found reference to command {commandName} in context {contextType.Name}");
                    
                    // Command node oluştur veya varsa getir
                    DiagramNode commandNode = null;
                    if (_nodeCache.TryGetValue(commandType, out var existingNode))
                    {
                        commandNode = existingNode;
                    }
                    else
                    {
                        commandNode = CreateNodeForType(graph, commandType, NodeType.Command);
                    }
                    
                    // Gruba eklenmiş mi kontrol et ve ekle
                    if (!commandGroup.NodeIds.Contains(commandNode.Id))
                    {
                        graph.AddNodeToGroup(commandNode.Id, commandGroup.Id);
                        
                        // Command ve Context arasında bağlantı oluştur
                        graph.AddEdge(
                            Guid.NewGuid().ToString(),
                            contextNode.Id,
                            commandNode.Id,
                            EdgeType.CommandUsage,
                            "Uses Command"
                        );
                    }
                }
            }
            
            // Context'teki Command tipindeki alanları ve property'leri analiz et
            var commandFields = contextType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(f => typeof(ICommand).IsAssignableFrom(f.FieldType) || f.FieldType.Name.EndsWith("Command"));
                
            var commandProperties = contextType.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(p => typeof(ICommand).IsAssignableFrom(p.PropertyType) || p.PropertyType.Name.EndsWith("Command"));
            
            foreach (var field in commandFields)
            {
                Debug.Log($"Found command field {field.Name} of type {field.FieldType.Name} in context {contextType.Name}");
                
                // Command node oluştur veya varsa getir
                if (!_nodeCache.TryGetValue(field.FieldType, out var commandNode))
                {
                    commandNode = CreateNodeForType(graph, field.FieldType, NodeType.Command);
                }
                
                // Gruba ekle
                if (!commandGroup.NodeIds.Contains(commandNode.Id))
                {
                    graph.AddNodeToGroup(commandNode.Id, commandGroup.Id);
                    
                    // Command ve Context arasında bağlantı oluştur
                    graph.AddEdge(
                        Guid.NewGuid().ToString(),
                        contextNode.Id,
                        commandNode.Id,
                        EdgeType.CommandUsage,
                        "Uses Command"
                    );
                }
            }
            
            foreach (var property in commandProperties)
            {
                Debug.Log($"Found command property {property.Name} of type {property.PropertyType.Name} in context {contextType.Name}");
                
                // Command node oluştur veya varsa getir
                if (!_nodeCache.TryGetValue(property.PropertyType, out var commandNode))
                {
                    commandNode = CreateNodeForType(graph, property.PropertyType, NodeType.Command);
                }
                
                // Gruba ekle
                if (!commandGroup.NodeIds.Contains(commandNode.Id))
                {
                    graph.AddNodeToGroup(commandNode.Id, commandGroup.Id);
                    
                    // Command ve Context arasında bağlantı oluştur
                    graph.AddEdge(
                        Guid.NewGuid().ToString(),
                        contextNode.Id,
                        commandNode.Id,
                        EdgeType.CommandUsage,
                        "Uses Command"
                    );
                }
            }
        }
        
        private void AnalyzeCommandBinderBindCalls(DiagramGraph graph, DiagramNode contextNode, DiagramNodeGroup commandGroup, string methodBody, List<string> commandBindings)
        {
            Debug.Log($"Analyzing command binder calls in method body of length: {methodBody.Length}");
            
            // CommandBinder.Bind(...).To<CommandType>() pattern'i için regex
            // Yeni daha güçlü regex: Daha açıklayıcı formatta Command-Signal ilişkilerini yakalar
            
            // 1. Önce CommandBinder.Bind(signal) bölümlerini yakala
            var bindStartPattern = @"CommandBinder\.Bind\s*\(\s*([^)]+)\s*\)";
            var bindStartMatches = Regex.Matches(methodBody, bindStartPattern);
            
            foreach (Match bindStartMatch in bindStartMatches)
            {
                if (bindStartMatch.Groups.Count < 2) continue;
                
                string signalRef = bindStartMatch.Groups[1].Value.Trim();
                string signalName = GetSignalNameFromReference(signalRef);
                
                // Bu bind çağrısının başlangıç pozisyonundan itibaren To<> çağrılarını bul
                int startPos = bindStartMatch.Index + bindStartMatch.Length;
                string remainingCode = methodBody.Substring(startPos);
                
                // To<CommandType> pattern'i için regex
                var commandPattern = @"\.To\s*<\s*([^>]+)\s*>\s*\(\s*\)";
                var commandMatches = Regex.Matches(remainingCode, commandPattern);
                
                DiagramNode previousCommandNode = null;
                DiagramNode signalNode = GetOrCreateSignalNode(graph, signalName, null);
                
                // Connect context to signal
                graph.AddEdge(
                    Guid.NewGuid().ToString(),
                    contextNode.Id,
                    signalNode.Id,
                    EdgeType.SignalBinding,
                    "Triggers Commands"
                );
                
                List<string> sequenceCommands = new List<string>();
                
                for (int i = 0; i < commandMatches.Count; i++)
                {
                    Match commandMatch = commandMatches[i];
                    if (commandMatch.Groups.Count < 2) continue;
                    
                    string commandTypeName = commandMatch.Groups[1].Value.Trim();
                    Debug.Log($"Found command: {commandTypeName} triggered by {signalName}");
                    
                    // Add to the command bindings list
                    sequenceCommands.Add(commandTypeName);
                    
                    // Command sınıfını bul
                    Type commandType = null;
                    if (_commandTypes.TryGetValue(commandTypeName, out var foundType))
                    {
                        commandType = foundType;
                    }
                    else
                    {
                        commandType = FindTypeByPartialName(commandTypeName);
                    }
                    
                    DiagramNode commandNode;
                    if (commandType != null)
                    {
                        commandNode = CreateNodeForType(graph, commandType, NodeType.Command);
                    }
                    else
                    {
                        commandNode = graph.AddNode(
                            Guid.NewGuid().ToString(),
                            commandTypeName,
                            commandTypeName,
                            string.Empty,
                            NodeType.Command
                        );
                    }
                    
                    // Mark as sequence command
                    commandNode.IsSequenceCommand = true;
                    commandNode.ExecutionOrder = i + 1;
                    
                    graph.AddNodeToGroup(commandNode.Id, commandGroup.Id);
                    
                    // Signal -> Command bağlantısı oluştur
                    graph.AddEdge(
                        Guid.NewGuid().ToString(),
                        signalNode.Id,
                        commandNode.Id,
                        EdgeType.CommandBinding,
                        $"Triggers #{i+1}"
                    );
                    
                    // Önceki command ile şimdiki command arasında sıralama bağlantısı
                    if (previousCommandNode != null)
                    {
                        graph.AddEdge(
                            Guid.NewGuid().ToString(),
                            previousCommandNode.Id,
                            commandNode.Id,
                            EdgeType.SequentialCommand,
                            "Next in Sequence"
                        );
                    }
                    
                    previousCommandNode = commandNode;
                }
                
                // Add the sequence to command bindings list
                if (sequenceCommands.Count > 0)
                {
                    commandBindings.Add($"{signalName} → {string.Join(" → ", sequenceCommands)}");
                }
                
                // Check for InSequence or InParallel after commands
                if (remainingCode.Contains(".InSequence()"))
                {
                    // Already handled as sequential
                }
                else if (remainingCode.Contains(".InParallel()"))
                {
                    // Oops, we misidentified as sequential, need to update edges
                    // This is more complex - would need to refactor to handle parallel execution
                }
            }
            
            // Handle SubButtonClickEventMap analysis
            AnalyzeSubButtonClickEventMap(graph, contextNode, commandGroup, methodBody, commandBindings);
        }
        
        private void AnalyzeCommandBinderBindGroupCalls(DiagramGraph graph, DiagramNode contextNode, DiagramNodeGroup commandGroup, string methodBody, List<string> commandBindings)
        {
            // CommandBinder.BindGroup("GroupName") pattern'ini ara
            var bindGroupPattern = @"CommandBinder\.BindGroup\s*\(\s*""([^""]+)""\s*\)";
            var bindGroupMatches = Regex.Matches(methodBody, bindGroupPattern);
            
            foreach (Match bindGroupMatch in bindGroupMatches)
            {
                if (bindGroupMatch.Groups.Count < 2) continue;
                
                string groupName = bindGroupMatch.Groups[1].Value;
                Debug.Log($"Found command bind group: {groupName}");
                
                // Bu grup için To<> çağrılarını bul
                int startPos = bindGroupMatch.Index + bindGroupMatch.Length;
                string remainingCode = methodBody.Substring(startPos);
                
                int endBlockPos = remainingCode.IndexOf(";");
                if (endBlockPos < 0) endBlockPos = remainingCode.Length;
                
                string bindBlock = remainingCode.Substring(0, endBlockPos);
                
                // Group için signal oluştur (visualization amaçlı)
                string signalName = $"Group_{groupName}";
                DiagramNode signalNode = GetOrCreateSignalNode(graph, signalName, null);
                
                // To<CommandType> pattern'i için regex
                var commandPattern = @"\.To\s*<\s*([^>]+)\s*>\s*\(\s*\)";
                var commandMatches = Regex.Matches(bindBlock, commandPattern);
                
                // InSequence/InParallel kontrolü
                bool isParallel = bindBlock.Contains(".InParallel");
                bool isSequence = !isParallel && (bindBlock.Contains(".InSequence") || commandMatches.Count > 1);
                
                // Add group signal to context
                graph.AddEdge(
                    Guid.NewGuid().ToString(),
                    contextNode.Id,
                    signalNode.Id,
                    EdgeType.SignalBinding,
                    $"Command Group: {groupName}"
                );
                
                List<string> groupCommands = new List<string>();
                DiagramNode previousCommandNode = null;
                
                for (int i = 0; i < commandMatches.Count; i++)
                {
                    Match commandMatch = commandMatches[i];
                    if (commandMatch.Groups.Count < 2) continue;
                    
                    string commandTypeName = commandMatch.Groups[1].Value.Trim();
                    Debug.Log($"Found group command: {commandTypeName} in group {groupName}");
                    
                    groupCommands.Add(commandTypeName);
                    
                    // Command tipini bul
                    Type commandType = null;
                    if (_commandTypes.TryGetValue(commandTypeName, out var foundType))
                    {
                        commandType = foundType;
                    }
                    else
                    {
                        commandType = FindTypeByPartialName(commandTypeName);
                    }
                    
                    DiagramNode commandNode;
                    if (commandType != null)
                    {
                        commandNode = CreateNodeForType(graph, commandType, NodeType.Command);
                    }
                    else
                    {
                        commandNode = graph.AddNode(
                            Guid.NewGuid().ToString(),
                            commandTypeName,
                            commandTypeName,
                            string.Empty,
                            NodeType.Command
                        );
                    }
                    
                    // Set node properties
                    if (isSequence)
                    {
                        commandNode.IsSequenceCommand = true;
                        commandNode.IsParallelCommand = false;
                    }
                    else if (isParallel)
                    {
                        commandNode.IsSequenceCommand = false;
                        commandNode.IsParallelCommand = true;
                    }
                    commandNode.ExecutionOrder = i + 1;
                    
                    graph.AddNodeToGroup(commandNode.Id, commandGroup.Id);
                    
                    // Connect signal to command
                    graph.AddEdge(
                        Guid.NewGuid().ToString(),
                        signalNode.Id,
                        commandNode.Id,
                        isSequence ? EdgeType.SequentialCommand : EdgeType.ParallelCommand,
                        $"Group Trigger #{i+1}"
                    );
                    
                    // Connect commands in sequence
                    if (isSequence && previousCommandNode != null)
                    {
                        graph.AddEdge(
                            Guid.NewGuid().ToString(),
                            previousCommandNode.Id,
                            commandNode.Id,
                            EdgeType.SequentialCommand,
                            "Next in Group"
                        );
                    }
                    
                    previousCommandNode = commandNode;
                }
                
                // Add the group command sequence to bindings
                if (groupCommands.Count > 0)
                {
                    string executionType = isSequence ? "Sequential" : (isParallel ? "Parallel" : "");
                    commandBindings.Add($"Group[{groupName}] {executionType} → {string.Join(" → ", groupCommands)}");
                }
            }
        }
        
        private string GetSignalNameFromReference(string signalReference)
        {
            // _signals.Start gibi bir referanstan sadece "Start" kısmını döndür
            var parts = signalReference.Split('.');
            if (parts.Length >= 2)
            {
                return $"{parts[0]}.{parts[1]}";
            }
            return signalReference;
        }
        
        private string GetMethodSourceCode(Type type, string methodName)
        {
            var filePath = GetTypeFilePath(type);
            if (string.IsNullOrEmpty(filePath)) return null;
            
            try
            {
                string fileContent = System.IO.File.ReadAllText(filePath);
                
                // Metod içeriğini regex ile bul
                var pattern = $@"public\s+override\s+void\s+{methodName}\s*\(\)\s*\{{([^}}]*(?:}}[^}}]+)*)\}}";
                var match = Regex.Match(fileContent, pattern, RegexOptions.Singleline);
                
                if (match.Success && match.Groups.Count > 1)
                {
                    return match.Groups[1].Value;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error reading file {filePath}: {ex.Message}");
            }
            
            return null;
        }
        
        private void AnalyzeMediationBindings(DiagramGraph graph, Type contextType, DiagramNode contextNode, DiagramNodeGroup mediatorGroup, DiagramNodeGroup viewGroup)
        {
            Debug.Log($"Analyzing Mediation Bindings for {contextType.Name}");
            
            try
            {
                string methodBody = GetMethodSourceCode(contextType, "MediationBindings");
                if (string.IsNullOrEmpty(methodBody))
                {
                    Debug.Log($"No MediationBindings method found in {contextType.Name}");
                    return;
                }
                
                // Extract mediation bindings: MediationBinder.Bind<ViewType>().To<MediatorType>();
                var mediationBindingRegex = new Regex(@"MediationBinder\.Bind<([^>]+)>\(\)\.To<([^>]+)>\(\);", RegexOptions.Multiline);
                var matches = mediationBindingRegex.Matches(methodBody);
                
                List<string> mediationBindings = new List<string>();
                
                foreach (Match match in matches)
                {
                    if (match.Groups.Count >= 3)
                    {
                        string viewTypeName = match.Groups[1].Value.Trim();
                        string mediatorTypeName = match.Groups[2].Value.Trim();
                        Debug.Log($"Found mediation binding: {viewTypeName} → {mediatorTypeName}");
                        
                        mediationBindings.Add($"{viewTypeName} → {mediatorTypeName}");
                        
                        // Find View type
                        Type viewType = FindTypeByPartialName(viewTypeName);
                        Type mediatorType = FindTypeByPartialName(mediatorTypeName);
                        
                        if (viewType != null && mediatorType != null)
                        {
                            // Create nodes for View and Mediator
                            var viewNode = CreateNodeForType(graph, viewType, NodeType.View);
                            graph.AddNodeToGroup(viewNode.Id, viewGroup.Id);
                            
                            var mediatorNode = CreateNodeForType(graph, mediatorType, NodeType.Mediator);
                            graph.AddNodeToGroup(mediatorNode.Id, mediatorGroup.Id);
                            
                            // Connect Context to View
                            graph.AddEdge(
                                Guid.NewGuid().ToString(),
                                contextNode.Id,
                                viewNode.Id,
                                EdgeType.ViewBinding,
                                "View Binding"
                            );
                            
                            // Connect View to Mediator
                            graph.AddEdge(
                                Guid.NewGuid().ToString(),
                                viewNode.Id,
                                mediatorNode.Id,
                                EdgeType.MediatorBinding,
                                "Mediator Binding"
                            );
                            
                            // Analyze additional references
                            AnalyzeViewReferences(graph, viewType, viewNode);
                            AnalyzeMediatorSignalReferences(graph, mediatorType, mediatorNode);
                        }
                        else
                        {
                            Debug.LogWarning($"Could not find types: View={viewType}, Mediator={mediatorType}");
                        }
                    }
                }
                
                // Store the bindings in context metadata
                if (mediationBindings.Count > 0)
                {
                    contextNode.AddMetadata("MediationBindings", string.Join(";", mediationBindings));
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error analyzing mediation bindings: {ex.Message}");
            }
        }
        
        // Kısmi ad ile tip bulma (örn: sadece "LogInView" verildiğinde tam adını bulup döndürür)
        private Type FindTypeByPartialName(string partialName)
        {
            if (string.IsNullOrEmpty(partialName)) return null;
            
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    // Skip problematic assemblies
                    if (assembly.FullName.Contains("Azure.") || 
                        assembly.FullName.Contains("Storage") ||
                        assembly.FullName.Contains("Microsoft."))
                    {
                        continue;
                    }
                    
                    // Önce tam ad ile dene
                    var type = assembly.GetType(partialName);
                    if (type != null)
                    {
                        return type;
                    }
                    
                    // Bulunamazsa kısmi ad ile ara
                    try
                    {
                        var types = assembly.GetTypes();
                        foreach (var t in types)
                        {
                            if (t.Name == partialName)
                            {
                                return t;
                            }
                            
                            // Ayrıca namespace içinde de kontrol et
                            if (t.FullName != null && t.FullName.EndsWith("." + partialName))
                            {
                                return t;
                            }
                        }
                    }
                    catch
                    {
                        // Tip yükleme hatalarını yok say ve devam et
                        continue;
                    }
                }
                catch
                {
                    // Assembly hatalarını yok say ve devam et
                    continue;
                }
            }
            
            return null;
        }
        
        // View sınıfının içindeki sinyal referanslarını analiz et
        private void AnalyzeViewReferences(DiagramGraph graph, Type viewType, DiagramNode viewNode)
        {
            // Şimdilik boş - gelecekte geliştirilebilir
            // View sınıfının field'larında Signal tipinde değişkenler aranabilir
        }
        
        // Mediator sınıfının içindeki sinyal referanslarını analiz et
        private void AnalyzeMediatorSignalReferences(DiagramGraph graph, Type mediatorType, DiagramNode mediatorNode)
        {
            try
            {
                // Mediator'un kodunu oku
                var sourceCode = GetTypeSourceCode(mediatorType);
                if (string.IsNullOrEmpty(sourceCode)) return;
                
                // Mediator'un sinyal referansları içerip içermediğini kontrol et
                var signalPattern = @"_signals\.([A-Za-z0-9_]+)";
                var matches = Regex.Matches(sourceCode, signalPattern);
                
                foreach (Match match in matches)
                {
                    if (match.Groups.Count >= 2)
                    {
                        string signalName = match.Groups[1].Value;
                        
                        // Bu sinyal adı var mı kontrol et
                        if (_signalCache.TryGetValue($"_signals.{signalName}", out var signalNode))
                        {
                            // Mediator -> Signal bağlantısı oluştur
                            graph.AddEdge(
                                Guid.NewGuid().ToString(),
                                mediatorNode.Id,
                                signalNode.Id,
                                EdgeType.SignalBinding,
                                "Uses Signal"
                            );
                        }
                        else if (_signalCache.TryGetValue($"_leaderboardSignals.{signalName}", out signalNode))
                        {
                            // Mediator -> Signal bağlantısı oluştur
                            graph.AddEdge(
                                Guid.NewGuid().ToString(),
                                mediatorNode.Id,
                                signalNode.Id,
                                EdgeType.SignalBinding,
                                "Uses Signal"
                            );
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Error analyzing mediator signal references: {ex.Message}");
            }
        }
        
        private Type FindType(string typeName)
        {
            if (string.IsNullOrEmpty(typeName)) return null;
            
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    // Skip problematic assemblies
                    if (assembly.FullName.Contains("Azure.") || 
                        assembly.FullName.Contains("Storage") ||
                        assembly.FullName.Contains("Microsoft."))
                    {
                        continue;
                    }
                    
                    var type = assembly.GetType(typeName);
                    if (type != null)
                    {
                        return type;
                    }
                }
                catch
                {
                    // Ignore errors and continue with next assembly
                    continue;
                }
            }
            
            return null;
        }
        
        private void AnalyzeInjectionBindings(DiagramGraph graph, Type contextType, DiagramNode contextNode, DiagramNodeGroup injectableGroup)
        {
            Debug.Log($"Analyzing Injection Bindings for {contextType.Name}");
            
            try
            {
                string methodBody = GetMethodSourceCode(contextType, "InjectionBindings");
                if (string.IsNullOrEmpty(methodBody))
                {
                    Debug.Log($"No InjectionBindings method found in {contextType.Name}");
                    return;
                }

                // Extract single-interface bindings: InjectionBinderCrossContext.Bind<IService, ServiceImpl>();
                var singleBindRegex = new Regex(@"InjectionBinderCrossContext\.Bind<([^,]+),\s*([^>]+)>\(\);", RegexOptions.Multiline);
                var singleMatches = singleBindRegex.Matches(methodBody);
                
                // Extract generic bindings: InjectionBinderCrossContext.Bind<ServiceImpl>();
                var genericBindRegex = new Regex(@"InjectionBinderCrossContext\.Bind<([^>]+)>\(\);", RegexOptions.Multiline);
                var genericMatches = genericBindRegex.Matches(methodBody);
                
                List<string> injectionBindings = new List<string>();
                
                foreach (Match match in singleMatches)
                {
                    if (match.Groups.Count >= 3)
                    {
                        string interfaceType = match.Groups[1].Value.Trim();
                        string implType = match.Groups[2].Value.Trim();
                        Debug.Log($"Found injection binding: {interfaceType} → {implType}");
                        
                        injectionBindings.Add($"{interfaceType} → {implType}");
                        
                        // Create nodes for interface and implementation
                        Type interfaceTypeObj = FindType(interfaceType);
                        if (interfaceTypeObj != null)
                        {
                            var interfaceNode = CreateNodeForType(graph, interfaceTypeObj, NodeType.Injectable);
                            graph.AddNodeToGroup(interfaceNode.Id, injectableGroup.Id);
                            
                            Type implTypeObj = FindType(implType);
                            if (implTypeObj != null)
                            {
                                var implNode = CreateNodeForType(graph, implTypeObj, NodeType.Injectable);
                                graph.AddNodeToGroup(implNode.Id, injectableGroup.Id);
                                
                                // Connect implementation to interface
                                graph.AddEdge(
                                    Guid.NewGuid().ToString(),
                                    contextNode.Id,
                                    interfaceNode.Id,
                                    EdgeType.InjectionBinding,
                                    "Injection Binding"
                                );
                                
                                graph.AddEdge(
                                    Guid.NewGuid().ToString(),
                                    interfaceNode.Id,
                                    implNode.Id,
                                    EdgeType.InjectionReference,
                                    "Implementation"
                                );
                            }
                        }
                    }
                }
                
                foreach (Match match in genericMatches)
                {
                    if (match.Groups.Count >= 2 && !singleBindRegex.IsMatch(match.Value))
                    {
                        string serviceType = match.Groups[1].Value.Trim();
                        Debug.Log($"Found generic injection binding: {serviceType}");
                        
                        injectionBindings.Add(serviceType);
                        
                        // Create a node for the service
                        Type serviceTypeObj = FindType(serviceType);
                        if (serviceTypeObj != null)
                        {
                            var serviceNode = CreateNodeForType(graph, serviceTypeObj, NodeType.Injectable);
                            graph.AddNodeToGroup(serviceNode.Id, injectableGroup.Id);
                            
                            // Connect context to service
                            graph.AddEdge(
                                Guid.NewGuid().ToString(),
                                contextNode.Id,
                                serviceNode.Id,
                                EdgeType.InjectionBinding,
                                "Injection Binding"
                            );
                        }
                    }
                }
                
                // Continue with the existing analysis logic
                string contextCode = GetTypeSourceCode(contextType);
                AnalyzeInjectableUsages(graph, contextType, contextNode, contextCode);
                AnalyzeServiceLocatorUsage(graph, contextType, contextNode, injectableGroup, contextCode);
                AnalyzeInjectionAttributes(graph, contextType, contextNode, injectableGroup);
                
                // Store the bindings in context metadata
                if (injectionBindings.Count > 0)
                {
                    contextNode.AddMetadata("InjectionBindings", string.Join(";", injectionBindings));
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error analyzing injection bindings: {ex.Message}");
            }
        }
        
        private void AnalyzeInjectableUsages(DiagramGraph graph, Type injectableType, DiagramNode injectableNode, string contextCode)
        {
            // İnjectablelar arasındaki bağlantıları bul
            string injectableSourceCode = GetTypeSourceCode(injectableType);
            if (string.IsNullOrEmpty(injectableSourceCode)) return;
            
            // Bu injectable içindeki diğer injectable referanslarını bul
            var injectionFields = injectableType.GetFields()
                .Where(f => 
                {
                    try
                    {
                        var hasInjectAttr = f.GetCustomAttributes(typeof(InjectAttribute), true).Length > 0;
                        return hasInjectAttr || f.Name.Contains("Inject") || f.Name.Contains("Service");
                    }
                    catch
                    {
                        return false;
                    }
                });
            
            foreach (var field in injectionFields)
            {
                Type fieldType = field.FieldType;
                
                Debug.Log($"Injectable {injectableType.Name} has field {field.Name} of type {fieldType.Name} that may be injected");
                
                // Referans edilen injectable node'unu bul veya oluştur
                DiagramNode referencedNode;
                if (_nodeCache.TryGetValue(fieldType, out var existingNode))
                {
                    referencedNode = existingNode;
                }
                else
                {
                    referencedNode = CreateNodeForType(graph, fieldType, NodeType.Injectable);
                    // Oluşturulan node'u injectables grubuna ekle
                    if (graph.Groups.ContainsKey("group_injectables"))
                    {
                        graph.AddNodeToGroup(referencedNode.Id, "group_injectables");
                    }
                }
                
                // İki injectable arasında bağlantı oluştur
                graph.AddEdge(
                    Guid.NewGuid().ToString(),
                    injectableNode.Id,
                    referencedNode.Id,
                    EdgeType.InjectionReference,
                    $"Uses {fieldType.Name}"
                );
            }
        }
        
        private void AnalyzeServiceLocatorUsage(DiagramGraph graph, Type contextType, DiagramNode contextNode, DiagramNodeGroup injectableGroup, string contextCode)
        {
            // ServiceLocator.Get<T>() veya injector.Get<T>() pattern'lerini ara
            var serviceLocatorPattern = @"(ServiceLocator|injector|InjectionBinder)\.Get\s*<\s*([^>]+)\s*>\s*\(\s*\)";
            var matches = Regex.Matches(contextCode, serviceLocatorPattern);
            
            Debug.Log($"Found {matches.Count} ServiceLocator usages in context {contextType.Name}");
            
            foreach (Match match in matches)
            {
                if (match.Groups.Count >= 3)
                {
                    string locatorType = match.Groups[1].Value;
                    string serviceTypeName = match.Groups[2].Value.Trim();
                    
                    Debug.Log($"Found {locatorType}.Get<{serviceTypeName}>() in context {contextType.Name}");
                    
                    // Service tipini bul
                    Type serviceType = FindType(serviceTypeName);
                    
                    if (serviceType != null)
                    {
                        var serviceNode = CreateNodeForType(graph, serviceType, NodeType.Injectable);
                        
                        if (!injectableGroup.NodeIds.Contains(serviceNode.Id))
                        {
                            graph.AddNodeToGroup(serviceNode.Id, injectableGroup.Id);
                            
                            // Context -> Service bağlantısı oluştur
                            graph.AddEdge(
                                Guid.NewGuid().ToString(),
                                contextNode.Id,
                                serviceNode.Id,
                                EdgeType.InjectionUsage,
                                $"Uses via {locatorType}"
                            );
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"Could not find service type: {serviceTypeName}");
                        
                        // Service tipi bulunamadıysa gene de node oluştur
                        var serviceNode = graph.AddNode(
                            Guid.NewGuid().ToString(),
                            serviceTypeName,
                            serviceTypeName,
                            string.Empty,
                            NodeType.Injectable
                        );
                        
                        if (!injectableGroup.NodeIds.Contains(serviceNode.Id))
                        {
                            graph.AddNodeToGroup(serviceNode.Id, injectableGroup.Id);
                            
                            // Context -> Service bağlantısı oluştur
                            graph.AddEdge(
                                Guid.NewGuid().ToString(),
                                contextNode.Id,
                                serviceNode.Id,
                                EdgeType.InjectionUsage,
                                $"Uses via {locatorType}"
                            );
                        }
                    }
                }
            }
        }
        
        private void AnalyzeInjectionAttributes(DiagramGraph graph, Type contextType, DiagramNode contextNode, DiagramNodeGroup injectableGroup)
        {
            // Context'in tüm alt sınıflarını bul ve inject attribute'larını kontrol et
            var typesToCheck = new List<Type>();
            
            try
            {
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    try
                    {
                        foreach (var type in assembly.GetTypes())
                        {
                            try
                            {
                                // Azure.Core tiplerini atla
                                if (type.FullName != null && (type.FullName.Contains("Azure.Core") || type.FullName.Contains("PageableHelpers")))
                                {
                                    continue;
                                }
                                
                                bool hasInjectAttribute = false;
                                
                                try
                                {
                                    hasInjectAttribute = type.GetCustomAttributes(typeof(InjectAttribute), true).Length > 0;
                                }
                                catch
                                {
                                    continue;
                                }
                                
                                if (hasInjectAttribute)
                                {
                                    typesToCheck.Add(type);
                                    continue;
                                }
                                
                                bool hasInjectFields = false;
                                try
                                {
                                    hasInjectFields = type.GetFields().Any(f => 
                                    {
                                        try
                                        {
                                            return f.GetCustomAttributes(typeof(InjectAttribute), true).Length > 0;
                                        }
                                        catch
                                        {
                                            return false;
                                        }
                                    });
                                }
                                catch
                                {
                                    // Hata olursa devam et
                                }
                                
                                if (hasInjectFields)
                                {
                                    typesToCheck.Add(type);
                                    continue;
                                }
                                
                                // Property kontrollerini try-catch içinde yap
                                try
                                {
                                    bool hasInjectProperties = type.GetProperties().Any(p => 
                                    {
                                        try
                                        {
                                            return p.GetCustomAttributes(typeof(InjectAttribute), true).Length > 0;
                                        }
                                        catch
                                        {
                                            return false;
                                        }
                                    });
                                    
                                    if (hasInjectProperties)
                                    {
                                        typesToCheck.Add(type);
                                    }
                                }
                                catch
                                {
                                    // Property'leri alırken hata olursa atla
                                }
                            }
                            catch
                            {
                                // Bir tip içinde hata olursa devam et
                            }
                        }
                    }
                    catch
                    {
                        // Bir assembly içinde hata olursa devam et
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Error gathering types for injection analysis: {ex.Message}");
            }
            
            foreach (var type in typesToCheck)
            {
                if (_nodeCache.ContainsKey(type)) continue;
                
                try
                {
                    // Inject attribute'lu alanları bul
                    var injectFields = type.GetFields()
                        .Where(f => 
                        {
                            try
                            {
                                return f.GetCustomAttributes(typeof(InjectAttribute), true).Length > 0;
                            }
                            catch
                            {
                                return false;
                            }
                        });
                    
                    var injectProperties = new List<PropertyInfo>();
                    try
                    {
                        injectProperties = type.GetProperties()
                            .Where(p => 
                            {
                                try
                                {
                                    return p.GetCustomAttributes(typeof(InjectAttribute), true).Length > 0;
                                }
                                catch
                                {
                                    return false;
                                }
                            }).ToList();
                    }
                    catch
                    {
                        // Property'leri alamadıysak boş liste kullan
                    }
                    
                    if (injectFields.Any() || injectProperties.Any())
                    {
                        // Henüz grafikte olmayan tipler için node oluştur
                        if (!_nodeCache.ContainsKey(type))
                        {
                            var node = CreateNodeForType(graph, type, GetNodeTypeForType(type));
                            var groupId = GetGroupIdForNodeType(node.Type);
                            if (!string.IsNullOrEmpty(groupId))
                            {
                                graph.AddNodeToGroup(node.Id, groupId);
                            }
                        }
                        
                        // Inject edilen tipleri işle
                        ProcessInjectableFields(graph, type, injectFields, injectableGroup);
                        ProcessInjectableProperties(graph, type, injectProperties, injectableGroup);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Type {type.FullName} could not be processed for dependency diagram: {ex.Message}");
                    continue;
                }
            }
        }
        
        private void ProcessInjectableFields(DiagramGraph graph, Type type, IEnumerable<FieldInfo> injectFields, DiagramNodeGroup injectableGroup)
        {
            foreach (var field in injectFields)
            {
                Type fieldType = field.FieldType;
                
                // Field tipine göre node oluştur
                if (!_nodeCache.ContainsKey(fieldType))
                {
                    var injectableNode = CreateNodeForType(graph, fieldType, NodeType.Injectable);
                    graph.AddNodeToGroup(injectableNode.Id, injectableGroup.Id);
                }
                
                // Tip -> Injectable bağlantısı kur
                if (_nodeCache.TryGetValue(type, out var sourceNode) && 
                    _nodeCache.TryGetValue(fieldType, out var targetNode))
                {
                    graph.AddEdge(
                        Guid.NewGuid().ToString(),
                        targetNode.Id,
                        sourceNode.Id,
                        EdgeType.InjectionBinding,
                        field.Name
                    );
                }
            }
        }
        
        private void ProcessInjectableProperties(DiagramGraph graph, Type type, IEnumerable<PropertyInfo> injectProperties, DiagramNodeGroup injectableGroup)
        {
            foreach (var prop in injectProperties)
            {
                Type propType = prop.PropertyType;
                
                // Property tipine göre node oluştur
                if (!_nodeCache.ContainsKey(propType))
                {
                    var injectableNode = CreateNodeForType(graph, propType, NodeType.Injectable);
                    graph.AddNodeToGroup(injectableNode.Id, injectableGroup.Id);
                }
                
                // Tip -> Injectable bağlantısı kur
                if (_nodeCache.TryGetValue(type, out var sourceNode) && 
                    _nodeCache.TryGetValue(propType, out var targetNode))
                {
                    graph.AddEdge(
                        Guid.NewGuid().ToString(),
                        targetNode.Id,
                        sourceNode.Id,
                        EdgeType.InjectionBinding,
                        prop.Name
                    );
                }
            }
        }
        
        private NodeType GetNodeTypeForType(Type type)
        {
            if (typeof(IContext).IsAssignableFrom(type))
                return NodeType.Context;
            if (typeof(ISignalBody).IsAssignableFrom(type))
                return NodeType.Signal;
            if (typeof(ICommandBody).IsAssignableFrom(type))
                return NodeType.Command;
            if (typeof(IView).IsAssignableFrom(type))
                return NodeType.View;
            if (typeof(IMediator).IsAssignableFrom(type))
                return NodeType.Mediator;
            
            return NodeType.Injectable;
        }
        
        private string GetGroupIdForNodeType(NodeType nodeType)
        {
            return nodeType switch
            {
                NodeType.Context => "group_contexts",
                NodeType.Signal => "group_signals",
                NodeType.Command => "group_commands",
                NodeType.View => "group_views",
                NodeType.Mediator => "group_mediators",
                NodeType.Injectable => "group_injectables",
                _ => string.Empty
            };
        }
        
        private void AnalyzeSubContexts(DiagramGraph graph, Type contextType, DiagramNode contextNode, DiagramNodeGroup contextGroup)
        {
            // SubContexts field'ını bulmaya çalış
            var subContextsField = contextType.GetField("SubContexts", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            
            // Start, Launch gibi metotları kontrol et
            var startMethod = GetMethodSourceCode(contextType, "Start");
            var launchMethod = GetMethodSourceCode(contextType, "Launch");
            var initMethod = GetMethodSourceCode(contextType, "Initialize");
            var awakeMethod = GetMethodSourceCode(contextType, "Awake");
            
            // "var subContext = new SubContext()" gibi pattern'leri ara
            var subContextPattern = @"new\s+([A-Za-z0-9_]+Context)\s*\(";
            
            // Tüm olası metotlarda alt context'leri ara
            List<Type> subContextTypes = new List<Type>();
            
            if (!string.IsNullOrEmpty(startMethod))
            {
                var matches = Regex.Matches(startMethod, subContextPattern);
                ProcessSubContextMatches(graph, contextNode, contextGroup, matches, subContextTypes);
            }
            
            if (!string.IsNullOrEmpty(launchMethod))
            {
                var matches = Regex.Matches(launchMethod, subContextPattern);
                ProcessSubContextMatches(graph, contextNode, contextGroup, matches, subContextTypes);
            }
            
            if (!string.IsNullOrEmpty(initMethod))
            {
                var matches = Regex.Matches(initMethod, subContextPattern);
                ProcessSubContextMatches(graph, contextNode, contextGroup, matches, subContextTypes);
            }
            
            if (!string.IsNullOrEmpty(awakeMethod))
            {
                var matches = Regex.Matches(awakeMethod, subContextPattern);
                ProcessSubContextMatches(graph, contextNode, contextGroup, matches, subContextTypes);
            }
            
            // Context'in public/private metotlarını kontrol et
            var methods = contextType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            foreach (var method in methods)
            {
                if (method.Name.Contains("Start") || method.Name.Contains("Launch") || method.Name.Contains("Init") || 
                    method.Name.Contains("Create") || method.Name.Contains("Setup"))
                {
                    var methodCode = GetMethodSourceCode(contextType, method.Name);
                    if (!string.IsNullOrEmpty(methodCode))
                    {
                        var matches = Regex.Matches(methodCode, subContextPattern);
                        ProcessSubContextMatches(graph, contextNode, contextGroup, matches, subContextTypes);
                    }
                }
            }
            
            // Daha agresif bir arama - sınıf içinde herhangi bir Context tipini de bulmaya çalış
            if (subContextTypes.Count == 0)
            {
                try
                {
                    var allTypes = AppDomain.CurrentDomain.GetAssemblies()
                        .SelectMany(a => {
                            try { return a.GetTypes(); } catch { return Type.EmptyTypes; }
                        })
                        .Where(t => typeof(IContext).IsAssignableFrom(t) && !t.IsAbstract && t != contextType)
                        .ToList();
                        
                    // Context sınıfının içeriğini al
                    var classCode = GetTypeSourceCode(contextType);
                    if (!string.IsNullOrEmpty(classCode))
                    {
                        foreach (var possibleSubContext in allTypes)
                        {
                            var typeName = possibleSubContext.Name;
                            if (Regex.IsMatch(classCode, $@"\b{typeName}\b"))
                            {
                                // Muhtemelen bir alt context. Ekle ve analiz et.
                                if (!subContextTypes.Contains(possibleSubContext))
                                {
                                    subContextTypes.Add(possibleSubContext);
                                    
                                    var subContextNode = CreateNodeForType(graph, possibleSubContext, NodeType.Context);
                                    graph.AddNodeToGroup(subContextNode.Id, contextGroup.Id);
                                    
                                    // Context -> SubContext bağlantısı oluştur
                                    graph.AddEdge(
                                        Guid.NewGuid().ToString(),
                                        contextNode.Id,
                                        subContextNode.Id,
                                        EdgeType.InjectionBinding,
                                        "SubContext Reference"
                                    );
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Error during aggressive subcontext search: {ex.Message}");
                }
            }
            
            // Bulunan her alt context için tam analiz yap - recursive
            foreach (var subContextType in subContextTypes)
            {
                // Alt context'in kendisi için analizi başlat
                AnalyzeSubContextType(graph, subContextType, contextGroup);
            }
        }
        
        private void AnalyzeSubContextType(DiagramGraph graph, Type subContextType, DiagramNodeGroup contextGroup)
        {
            try
            {
                // Bu context zaten işlendiyse tekrar işleme
                if (_nodeCache.ContainsKey(subContextType))
                {
                    return;
                }
                
                // Context node'unu oluştur
                var subContextNode = CreateNodeForType(graph, subContextType, NodeType.Context);
                graph.AddNodeToGroup(subContextNode.Id, contextGroup.Id);
                
                // Diğer grup referanslarını al
                var signalGroup = graph.Groups.Values.FirstOrDefault(g => g.Type == NodeType.Signal);
                var commandGroup = graph.Groups.Values.FirstOrDefault(g => g.Type == NodeType.Command);
                var mediatorGroup = graph.Groups.Values.FirstOrDefault(g => g.Type == NodeType.Mediator);
                var viewGroup = graph.Groups.Values.FirstOrDefault(g => g.Type == NodeType.View);
                var injectableGroup = graph.Groups.Values.FirstOrDefault(g => g.Type == NodeType.Injectable);
                
                // Alt context'in binding'lerini analiz et
                AnalyzeSignalBindings(graph, subContextType, subContextNode, signalGroup);
                AnalyzeCommandBindings(graph, subContextType, subContextNode, commandGroup);
                AnalyzeMediationBindings(graph, subContextType, subContextNode, mediatorGroup, viewGroup);
                AnalyzeInjectionBindings(graph, subContextType, subContextNode, injectableGroup);
                
                // Recursive olarak bu alt context'in kendi alt context'lerini analiz et
                AnalyzeSubContexts(graph, subContextType, subContextNode, contextGroup);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Error analyzing subcontext {subContextType.Name}: {ex.Message}");
            }
        }
        
        private void ProcessSubContextMatches(DiagramGraph graph, DiagramNode contextNode, DiagramNodeGroup contextGroup, MatchCollection matches, List<Type> subContextTypes)
        {
            foreach (Match match in matches)
            {
                if (match.Groups.Count >= 2)
                {
                    string subContextTypeName = match.Groups[1].Value.Trim();
                    
                    // SubContext tipini bul
                    Type subContextType = FindType(subContextTypeName);
                    
                    if (subContextType != null && !subContextTypes.Contains(subContextType))
                    {
                        subContextTypes.Add(subContextType);
                        
                        var subContextNode = CreateNodeForType(graph, subContextType, NodeType.Context);
                        graph.AddNodeToGroup(subContextNode.Id, contextGroup.Id);
                        
                        // Context -> SubContext bağlantısı oluştur
                        graph.AddEdge(
                            Guid.NewGuid().ToString(),
                            contextNode.Id,
                            subContextNode.Id,
                            EdgeType.InjectionBinding,
                            "SubContext"
                        );
                    }
                }
            }
        }
        
        private string GetTypeSourceCode(Type type)
        {
            try
            {
                // Tip dosya yolunu bul
                string assetPath = GetTypeFilePath(type);
                if (string.IsNullOrEmpty(assetPath))
                {
                    return string.Empty;
                }
                
                // Dosya içeriğini oku
                string fullPath = System.IO.Path.Combine(Application.dataPath.Replace("Assets", ""), assetPath);
                if (System.IO.File.Exists(fullPath))
                {
                    return System.IO.File.ReadAllText(fullPath);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Error getting type source code for {type.Name}: {ex.Message}");
            }
            
            return string.Empty;
        }
        
        private string GetTypeFilePath(Type type)
        {
            // Try to find the file path of the type
            foreach (var script in AssetDatabase.FindAssets("t:MonoScript").Select(AssetDatabase.GUIDToAssetPath))
            {
                var assetType = AssetDatabase.LoadAssetAtPath<MonoScript>(script)?.GetClass();
                if (assetType == type)
                {
                    return script;
                }
            }
            
            return string.Empty;
        }
        
        // SubButtonClickEventMap gibi özel Map notasyonlarını analiz eden yeni metot
        private void AnalyzeSubButtonClickEventMap(DiagramGraph graph, DiagramNode contextNode, DiagramNodeGroup commandGroup, string methodBody, List<string> commandBindings)
        {
            // _leaderboardSignals.SubButtonClickEventMap[LeaderboardSubButtonType.XXX] pattern'ini bul
            var subButtonPattern = @"(\w+)\.SubButtonClickEventMap\s*\[\s*(\w+)\.(\w+)\s*\]";
            var subButtonMatches = Regex.Matches(methodBody, subButtonPattern);
            
            foreach (Match subBtnMatch in subButtonMatches)
            {
                if (subBtnMatch.Groups.Count < 4) continue;
                
                string signalVarName = subBtnMatch.Groups[1].Value.Trim();
                string enumTypeName = subBtnMatch.Groups[2].Value.Trim();
                string enumValueName = subBtnMatch.Groups[3].Value.Trim();
                
                string signalName = $"{signalVarName}.SubButtonClickEventMap[{enumTypeName}.{enumValueName}]";
                Debug.Log($"Found SubButtonClickEventMap signal: {signalName}");
                
                // Bu signal için sonraki To<> komutlarını bul
                int startPos = subBtnMatch.Index + subBtnMatch.Length;
                string remainingCode = methodBody.Substring(startPos);
                
                var endBlockPos = remainingCode.IndexOf(";");
                if (endBlockPos < 0) endBlockPos = remainingCode.Length;
                
                string bindBlock = remainingCode.Substring(0, endBlockPos);
                
                // To<CommandType> pattern'i için regex
                var commandPattern = @"\.To\s*<\s*([^>]+)\s*>\s*\(\s*\)";
                var commandMatches = Regex.Matches(bindBlock, commandPattern);
                
                if (commandMatches.Count > 0)
                {
                    DiagramNode signalNode = GetOrCreateSignalNode(graph, signalName, null);
                    DiagramNode previousCommandNode = null;
                    
                    // Connect context to signal
                    graph.AddEdge(
                        Guid.NewGuid().ToString(),
                        contextNode.Id,
                        signalNode.Id,
                        EdgeType.SignalBinding,
                        "Triggers Button Commands"
                    );
                    
                    List<string> buttonCommands = new List<string>();
                    
                    // Process command chain
                    for (int i = 0; i < commandMatches.Count; i++)
                    {
                        Match commandMatch = commandMatches[i];
                        if (commandMatch.Groups.Count < 2) continue;
                        
                        string commandTypeName = commandMatch.Groups[1].Value.Trim();
                        Debug.Log($"Found button command: {commandTypeName} triggered by {signalName}");
                        
                        buttonCommands.Add(commandTypeName);
                        
                        // Command sınıfını bul
                        Type commandType = null;
                        if (_commandTypes.TryGetValue(commandTypeName, out var foundType))
                        {
                            commandType = foundType;
                        }
                        else
                        {
                            commandType = FindTypeByPartialName(commandTypeName);
                        }
                        
                        DiagramNode commandNode;
                        if (commandType != null)
                        {
                            commandNode = CreateNodeForType(graph, commandType, NodeType.Command);
                        }
                        else
                        {
                            commandNode = graph.AddNode(
                                Guid.NewGuid().ToString(),
                                commandTypeName,
                                commandTypeName,
                                string.Empty,
                                NodeType.Command
                            );
                        }
                        
                        // Set command sequence properties
                        commandNode.IsSequenceCommand = true;
                        commandNode.ExecutionOrder = i + 1;
                        
                        graph.AddNodeToGroup(commandNode.Id, commandGroup.Id);
                        
                        // Signal -> Command bağlantısı oluştur
                        graph.AddEdge(
                            Guid.NewGuid().ToString(),
                            signalNode.Id,
                            commandNode.Id,
                            EdgeType.CommandBinding,
                            $"Triggers #{i+1}"
                        );
                        
                        // Önceki command ile şimdiki command arasında sıralama bağlantısı
                        if (previousCommandNode != null)
                        {
                            graph.AddEdge(
                                Guid.NewGuid().ToString(),
                                previousCommandNode.Id,
                                commandNode.Id,
                                EdgeType.SequentialCommand,
                                "Next in Sequence"
                            );
                        }
                        
                        previousCommandNode = commandNode;
                    }
                    
                    // Add the button command sequence to bindings
                    if (buttonCommands.Count > 0)
                    {
                        commandBindings.Add($"Button[{enumValueName}] → {string.Join(" → ", buttonCommands)}");
                    }
                }
            }
        }
    }
} 