#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using FlowIoC.Editor.CodeGenerator.Menus.Module;
using FlowIoC.Editor.Config.ModuleConfig;
using FlowIoC.Editor.Modules;

namespace FlowIoC.Editor.CodeGenerator.Menus
{
    public class AssemblyCreatorWindow : EditorWindow
    {
        private const string ASMDEF_EXT = ".asmdef";

        private ModuleNode _rootNode;
        private Vector2 _scrollPos;
        private bool _selectAllToggle;

        [MenuItem("Tools/FlowIoC/Assembly Creator Window")]
        public static void ShowWindow()
        {
            var window = GetWindow<AssemblyCreatorWindow>();
            window.titleContent = new GUIContent("Assembly Creator");
            window.minSize = new Vector2(600, 400);
            window.Show();
        }

        private void OnEnable()
        {
            BuildModuleTree();
        }

        private void OnGUI()
        {
            DrawHeader();

            if (_rootNode == null || _rootNode.Children.Count == 0)
            {
                EditorGUILayout.HelpBox("No modules found (or no .asmdef missing)!", MessageType.Info);
                if (GUILayout.Button("Refresh", GUILayout.Height(25)))
                {
                    BuildModuleTree();
                }

                return;
            }

            EditorGUILayout.BeginHorizontal();
            {
                bool newToggle = GUILayout.Toggle(_selectAllToggle, "Select All", GUILayout.Width(100));
                if (newToggle != _selectAllToggle)
                {
                    _selectAllToggle = newToggle;
                    SetAllSelection(_rootNode, _selectAllToggle);
                }

                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Refresh", GUILayout.Width(70)))
                {
                    BuildModuleTree();
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.ExpandHeight(true));
            {
                foreach (var child in _rootNode.Children)
                {
                    DrawModuleNode(child, 0);
                }
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(10);

            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("Create Assembly & Update Namespace for Selected Modules", GUILayout.Height(30)))
            {
                CreateAssemblyForSelected(_rootNode);
            }

            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(8);
        }

        private void DrawHeader()
        {
            EditorGUILayout.Space(6);

            var headerBoxStyle = new GUIStyle("box")
            {
                normal =
                {
                    background = MakeColorTexture(new Color(0.6f, 0.4f, 1f)),
                }
            };

            var titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter,
            };

            EditorGUILayout.BeginVertical(headerBoxStyle);
            {
                GUILayout.Label("Assembly Creator", titleStyle, GUILayout.ExpandWidth(true));
                GUILayout.Space(4);

                var infoStyle = new GUIStyle(EditorStyles.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 10,
                    normal = {textColor = Color.white}
                };
                GUILayout.Label("Create .asmdef & update namespaces for modules (hierarchical view)", infoStyle);

                GUILayout.Space(6);
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(6);
        }

        private void DrawModuleNode(ModuleNode node, int indentLevel)
        {
            EditorGUILayout.BeginHorizontal("box");
            {
                GUILayout.Space(indentLevel * 16);

                node.IsExpanded = EditorGUILayout.Foldout(
                    node.IsExpanded,
                    node.DisplayName,
                    true,
                    GetFoldoutStyle()
                );

                using (new EditorGUI.DisabledScope(node.HasAsmdef))
                {
                    bool newSelected = GUILayout.Toggle(node.Selected, "CreateAssembly", GUILayout.Width(110));
                    if (newSelected != node.Selected)
                    {
                        node.Selected = newSelected;
                    }
                }
            }
            EditorGUILayout.EndHorizontal();

            if (node.IsExpanded && node.Children.Count > 0)
            {
                foreach (var child in node.Children)
                {
                    DrawModuleNode(child, indentLevel + 1);
                }
            }
        }

        private void BuildModuleTree()
        {
            _rootNode = new ModuleNode
            {
                Name = "Root",
                DisplayName = "Root",
                FullPath = "",
                HasAsmdef = true,
                Selected = false,
                IsExpanded = true
            };

            var registry = new ModuleRegistryFactory().FromProject();
            var pathResolver = new ModuleAssetPathResolver();

            LoadModulesRecursive(registry, pathResolver, null, _rootNode);
        }

        /// <summary>
        /// Walks the registry rather than the filesystem: the top-level call (parentModule null)
        /// takes every module with no module ancestor, and each recursive call takes the direct
        /// Sub/Screen/Test children of the module just added, mirroring ModuleHierarchyDrawer.
        /// </summary>
        private void LoadModulesRecursive(
            ModuleRegistry registry,
            ModuleAssetPathResolver pathResolver,
            ModuleDescriptor parentModule,
            ModuleNode parentNode)
        {
            ModuleKind[] nestedModuleKinds = {ModuleKind.Sub, ModuleKind.Test, ModuleKind.Screen};

            IEnumerable<ModuleDescriptor> modules = parentModule == null
                ? registry.Modules.Where(module => !registry.AncestorsOf(module).Any())
                : nestedModuleKinds.SelectMany(kind => registry.ChildrenOf(parentModule, kind));

            foreach (ModuleDescriptor module in modules)
            {
                string fullPath = pathResolver.ToAbsolutePath(registry.PathOf(module));
                if (string.IsNullOrEmpty(fullPath))
                {
                    Debug.LogWarning($"[AssemblyCreatorWindow] Skipping '{module.Name}': its folder GUID no longer resolves to a path.");
                    continue;
                }

                bool hasAsmdef = Directory.GetFiles(fullPath, "*.asmdef", SearchOption.TopDirectoryOnly).Length > 0;

                var node = new ModuleNode
                {
                    Name = module.Name,
                    DisplayName = module.Name,
                    FullPath = fullPath,
                    Kind = module.Kind,
                    HasAsmdef = hasAsmdef,
                    Selected = false,
                    IsExpanded = false,
                    Children = new List<ModuleNode>()
                };

                LoadModulesRecursive(registry, pathResolver, module, node);

                parentNode.Children.Add(node);
            }
        }

        private List<ModuleNode> GetAllNodes(ModuleNode root)
        {
            var result = new List<ModuleNode>();
            foreach (var child in root.Children)
            {
                result.Add(child);
                if (child.Children.Count > 0)
                {
                    result.AddRange(GetAllNodes(child));
                }
            }

            return result;
        }

        private void SetAllSelection(ModuleNode node, bool selected)
        {
            foreach (var child in node.Children)
            {
                if (!child.HasAsmdef)
                {
                    child.Selected = selected;
                }

                if (child.Children.Count > 0)
                {
                    SetAllSelection(child, selected);
                }
            }
        }

        private void CreateAssemblyForSelected(ModuleNode root)
        {
            int createdCount = 0;
            var nodes = GetAllNodes(root);

            foreach (var node in nodes)
            {
                if (node.Selected && !node.HasAsmdef)
                {
                    string asmdefPath = Path.Combine(node.FullPath, node.DisplayName + ASMDEF_EXT);
                    if (!File.Exists(asmdefPath))
                    {
                        CreateAssemblyDefinitionFile(asmdefPath, node.DisplayName);
                        node.HasAsmdef = true;
                    }
                    else
                    {
                        Debug.LogWarning($".asmdef already exists at: {asmdefPath}");
                    }

                    UpdateDotSettingsAndNamespace(node.FullPath, node.DisplayName, node.Kind);

                    createdCount++;
                    Debug.Log($"Assembly + Namespace updated for: {node.DisplayName}");
                }
            }

            if (createdCount > 0)
            {
                EditorUtility.DisplayDialog("Assembly Creator", $"Created {createdCount} assemblies.", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Assembly Creator", "No assemblies created.", "OK");
            }

            AssetDatabase.Refresh();
        }

        private void UpdateDotSettingsAndNamespace(string moduleFullPath, string moduleName, ModuleKind moduleKind)
        {
            CreateDotSettingsFileInNewFormat(moduleFullPath, moduleName);

            DirectoryStructureConfig config = new DirectoryStructureConfigProvider().ConfigFor(moduleKind);
            if (config != null)
            {
                var finalAssemblyName = GetParsedAssemblyName(moduleName);
                string newDotSettingsPath = Path.Combine(moduleFullPath, finalAssemblyName + ".csproj.DotSettings");

                TraverseFoldersAndSetProviders(moduleFullPath, config.RootFolders, newDotSettingsPath);
                NamespaceUtility.SetNamespaceProvider(moduleFullPath, true, newDotSettingsPath);
            }

            UpdateAllScriptsNamespaceInFolder(moduleFullPath);
        }

        private void UpdateAllScriptsNamespaceInFolder(string folderPath)
        {
            var csFiles = Directory.GetFiles(folderPath, "*.cs", SearchOption.AllDirectories);
            Regex regex = new Regex(@"(^|\r?\n)\s*namespace\s+([A-Za-z0-9_.]+)");

            foreach (var file in csFiles)
            {
                if (file.EndsWith(".meta")) continue;

                string fileDir = Path.GetDirectoryName(file);
                if (!NamespaceUtility.IsFolderNamespaceProvider(fileDir))
                    continue;

                string finalNamespace = NamespaceUtility.GetFullNamespaceForFile(file);

                string content = File.ReadAllText(file);
                if (regex.IsMatch(content))
                {
                    string modified = regex.Replace(content, $"$1namespace {finalNamespace}");
                    if (modified != content)
                    {
                        File.WriteAllText(file, modified);
                        Debug.Log($"Updated namespace in: {file} => {finalNamespace}");
                    }
                }
            }
        }

        private void TraverseFoldersAndSetProviders(string basePath, List<FolderConfig> folders, string dotSettingsPath)
        {
            foreach (var folder in folders)
            {
                string folderPath = Path.Combine(basePath, folder.FolderName);
                if (Directory.Exists(folderPath))
                {
                    bool isProvider = folder.IsNamespaceProvider;
                    NamespaceUtility.SetNamespaceProvider(folderPath, isProvider, dotSettingsPath);

                    if (folder.SubFolders != null && folder.SubFolders.Count > 0)
                    {
                        TraverseFoldersAndSetProviders(folderPath, folder.SubFolders, dotSettingsPath);
                    }
                }
            }
        }

        private Texture2D MakeColorTexture(Color color)
        {
            Texture2D texture = new Texture2D(2, 2);
            for (int y = 0; y < 2; y++)
            {
                for (int x = 0; x < 2; x++)
                    texture.SetPixel(x, y, color);
            }

            texture.Apply();
            return texture;
        }

        private GUIStyle GetFoldoutStyle()
        {
            var style = new GUIStyle(EditorStyles.foldout)
            {
                richText = true,
                fontStyle = FontStyle.Bold,
                fontSize = 11
            };
            return style;
        }

        private static string GetParsedAssemblyName(string rawAssemblyName) =>
            new ModuleAssemblyName().From(rawAssemblyName);

        private static void CreateAssemblyDefinitionFile(string oldFilePath, string rawAssemblyName)
        {
            var finalAssemblyName = GetParsedAssemblyName(rawAssemblyName);

            var asmdefContent = $@"{{
  ""name"": ""{finalAssemblyName}"",
  ""references"": [
    ""FlowIoC""
  ],
  ""includePlatforms"": [],
  ""excludePlatforms"": [],
  ""allowUnsafeCode"": false,
  ""overrideReferences"": false,
  ""precompiledReferences"": [],
  ""autoReferenced"": true,
  ""defineConstraints"": [],
  ""versionDefines"": [],
  ""noEngineReferences"": false
}}";

            string directory = Path.GetDirectoryName(oldFilePath) ?? "";
            string newFileName = finalAssemblyName + ".asmdef";
            string newFilePath = Path.Combine(directory, newFileName);

            File.WriteAllText(newFilePath, asmdefContent);
            Debug.LogError($"Assembly Definition created at: {newFilePath} (Name: {finalAssemblyName}, references: FlowIoC)");

            if (!oldFilePath.Equals(newFilePath, StringComparison.OrdinalIgnoreCase) && File.Exists(oldFilePath))
            {
                File.Delete(oldFilePath);
                string oldAssetPath = oldFilePath;
                AssetDatabase.DeleteAsset(oldAssetPath);
                Debug.Log($"Deleted old Assembly Definition file: {oldFilePath}");
            }

            AssetDatabase.Refresh();
        }

        private static void CreateDotSettingsFileInNewFormat(string moduleFullPath, string rawAssemblyName)
        {
            string oldDotSettingsPath = Path.Combine(moduleFullPath, rawAssemblyName + ".csproj.DotSettings");

            var finalAssemblyName = GetParsedAssemblyName(rawAssemblyName);
            string newDotSettingsPath = Path.Combine(moduleFullPath, finalAssemblyName + ".csproj.DotSettings");

            if (!File.Exists(newDotSettingsPath))
            {
                NamespaceUtility.CreateDotSettingsFile(newDotSettingsPath);
                Debug.Log($".DotSettings created => {newDotSettingsPath}");
            }
            else
            {
                Debug.LogWarning($"DotSettings file already exists: {newDotSettingsPath}");
            }

            if (!oldDotSettingsPath.Equals(newDotSettingsPath, StringComparison.OrdinalIgnoreCase) &&
                File.Exists(oldDotSettingsPath))
            {
                File.Delete(oldDotSettingsPath);
                Debug.Log($"Deleted old .DotSettings file: {oldDotSettingsPath}");
            }
        }

        private class ModuleNode
        {
            public string Name;
            public string DisplayName;
            public string FullPath;
            public ModuleKind Kind;
            public bool HasAsmdef;
            public bool Selected;
            public bool IsExpanded;
            public List<ModuleNode> Children = new List<ModuleNode>();
        }
    }
}
#endif