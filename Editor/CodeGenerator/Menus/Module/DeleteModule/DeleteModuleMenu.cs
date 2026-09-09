#if UNITY_EDITOR
using FlowIoC.BaseModule.Attributes;
using FlowIoC.Editor.Inspector;
using System;
using System.Collections.Generic;
using System.Linq;
using FlowIoC.Editor.Modules;
using UnityEditor;
using UnityEngine;

namespace FlowIoC.Editor.CodeGenerator.Menus.Module.DeleteModule
{
    internal class DeleteModuleMenu : EditorWindow
    {
        private Vector2 _scrollPosition;
        private List<ModuleEntry> _modules;
        private string _searchText = "";

        private readonly FlowHeaderBar _bar = new FlowHeaderBar(new FlowPalette(), new FlowHelpPageMap());
        private readonly CoreModules _coreModules = new CoreModules();

        /// <summary>
        /// How far the indent is stepped per level of nesting. The rows are always expanded: what
        /// the reader is here to see is which modules sit inside which, and a foldout would hide
        /// exactly that behind a click.
        /// </summary>
        private const float INDENT_STEP = 14f;

        private const float NAME_WIDTH = 250f;

        private struct ModuleEntry
        {
            public string Name;
            public string Path;
            public string Type;
            public string FolderGuid;

            /// <summary>How many modules this one sits inside. A main module is at zero.</summary>
            public int Depth;

            /// <summary>
            /// The modules inside this one, in the order they are drawn under it. Deleting a module
            /// deletes its folder, so every one of these goes with it - which is what the
            /// confirmation has to say before the reader answers it.
            /// </summary>
            public List<string> Descendants;
        }

        private void OnEnable()
        {
            ScanModules();
        }

        private void OnGUI()
        {
            _bar.DrawWindow("Delete Module", "FlowIoC",
                "Folder, assembly, settings, index entry and log channel", null, null, "Delete Module");

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "Select a module to delete. This will remove the module folder, its assembly "
                + "definition, namespace settings, log type registration, Addressables entry and "
                + "its entry in the module index.\n\n"
                + "A module is drawn under the module it lives in. Deleting one deletes its folder, "
                + "so everything indented under it goes with it - delete those first if you meant "
                + "to keep the module they are in.\n\n"
                + "MainModule, ConnectorModule and ScreenModule have no Delete: the project is "
                + "built on them, and each row says which part.",
                MessageType.Warning);

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Search:", GUILayout.Width(50));
            _searchText = EditorGUILayout.TextField(_searchText);
            if (GUILayout.Button("Refresh", GUILayout.Width(60)))
                ScanModules();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            if (_modules == null || _modules.Count == 0)
            {
                EditorGUILayout.HelpBox("No modules found.", MessageType.Info);
                return;
            }

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            // A module whose name matches brings its whole subtree with it, because that subtree is
            // what deleting it would take - hiding it would answer the search with less than the
            // reader needs. A module that matches nothing itself is still drawn while something
            // inside it matches, so what matched is never left without the module it lives in.
            var matchedDepth = -1;

            foreach (ModuleEntry module in _modules)
            {
                if (matchedDepth >= 0 && module.Depth <= matchedDepth) matchedDepth = -1;

                bool inMatchedSubtree = matchedDepth >= 0;

                if (Matches(module.Name)) matchedDepth = module.Depth;
                else if (!inMatchedSubtree && !SubtreeMatches(module)) continue;

                DrawModuleRow(module);
            }

            EditorGUILayout.EndScrollView();
        }

        private bool Matches(string moduleName) =>
            string.IsNullOrEmpty(_searchText)
            || moduleName.IndexOf(_searchText, StringComparison.OrdinalIgnoreCase) >= 0;

        private bool SubtreeMatches(ModuleEntry module)
        {
            foreach (string descendant in module.Descendants)
            {
                if (Matches(descendant)) return true;
            }

            return false;
        }

        /// <summary>
        /// One module, indented by how deep inside another it sits. The indent is the whole point
        /// of the list: a screen module and the test module inside it used to be two unrelated
        /// rows, so the reader confirming a deletion could not see from the window what the
        /// deletion would actually take.
        /// </summary>
        private void DrawModuleRow(ModuleEntry module)
        {
            float indent = module.Depth * INDENT_STEP;

            // The same row the other module panels draw: the Root's washed violet behind it,
            // and the kind in the colour that kind wears everywhere else.
            GUI.backgroundColor = new ModulePanelTheme().Row;
            EditorGUILayout.BeginHorizontal("box");
            GUI.backgroundColor = Color.white;

            if (indent > 0f) GUILayout.Space(indent);

            EditorGUILayout.LabelField(
                module.Name, EditorStyles.boldLabel, GUILayout.Width(NAME_WIDTH - indent));

            DrawKindLabel(module.Type);
            GUILayout.FlexibleSpace();

            string kept = _coreModules.WhyKept(module.Name);

            if (kept != null) DrawKeptReason(kept);
            else DrawDeleteButton(module);

            EditorGUILayout.EndHorizontal();
        }

        private void DrawDeleteButton(ModuleEntry module)
        {
            GUI.backgroundColor = new ModulePanelTheme().ActionRemove;

            if (GUILayout.Button("Delete", GUILayout.Width(60))) Delete(module);

            GUI.backgroundColor = Color.white;
        }

        /// <summary>
        /// What the row says where the button would be, for one of the three modules the project is
        /// built on. The reason goes in rather than a disabled button, because a button that cannot
        /// be pressed says only that something is wrong and leaves the reader to guess what.
        /// </summary>
        private void DrawKeptReason(string reason)
        {
            var style = new GUIStyle(EditorStyles.miniLabel) {alignment = TextAnchor.MiddleRight};

            Color previous = GUI.color;
            GUI.color = new Color(previous.r, previous.g, previous.b, 0.6f);

            GUILayout.Label(reason, style);

            GUI.color = previous;
        }

        /// <summary>
        /// The confirmation, and the deletion behind it. What goes with the module is named before
        /// the reader answers: deleting a module deletes its folder, so every module inside it goes
        /// too, and a reader who wanted only the parent can close this and delete those first.
        /// </summary>
        private void Delete(ModuleEntry module)
        {
            string alsoGoing = module.Descendants.Count == 0
                ? string.Empty
                : $"These modules are inside it and go with it:\n{Listed(module.Descendants)}\n\n";

            if (!EditorUtility.DisplayDialog(
                    "Delete Module",
                    $"Are you sure you want to delete '{module.Name}'?\n\n"
                    + $"Path: {module.Path}\n\n"
                    + alsoGoing
                    + "This action cannot be undone!",
                    "Delete", "Cancel"))
                return;

            // Before anything is deleted, so that answering "cancel" here leaves the module whole:
            // the assemblies, the settings files and the folder are all still there, and the reader
            // is back where they started.
            if (!UnwireSubContexts(module))
            {
                GUIUtility.ExitGUI();

                return;
            }

            IReadOnlyList<string> deleted =
                ModuleDeleter.DeleteModule(module.Name, module.Path, module.FolderGuid);

            // The deleter reports rather than announces, so the summary dialog belongs here, where
            // there is already a user looking at a window.
            EditorUtility.DisplayDialog(
                "Module Deleted",
                $"'{module.Name}' has been deleted.\n\n{string.Join("\n", deleted)}",
                "OK");

            ScanModules();
            GUIUtility.ExitGUI();
        }

        /// <summary>
        /// What kind of module the row is, in the colour that kind wears in the inspector and in
        /// the module trees. Main and Sub are the ordinary case and say nothing.
        /// </summary>
        private void DrawKindLabel(string kind)
        {
            if (!TryRoleOf(kind, out FlowRole role)) return;

            var content = new GUIContent(kind.ToUpperInvariant());

            var style = new GUIStyle(EditorStyles.miniBoldLabel)
            {
                alignment = TextAnchor.MiddleLeft,
                margin = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(0, 0, 0, 0)
            };

            style.normal.textColor = new FlowPalette().Accent(role, EditorGUIUtility.isProSkin);

            GUILayout.Label(content, style, GUILayout.Width(style.CalcSize(content).x + 10f),
                GUILayout.Height(EditorGUIUtility.singleLineHeight));
        }

        private bool TryRoleOf(string kind, out FlowRole role)
        {
            switch (kind)
            {
                case "Screen":
                    role = FlowRole.Screen;
                    return true;
                case "Test":
                    role = FlowRole.Test;
                    return true;
                default:
                    role = FlowRole.Root;
                    return false;
            }
        }

        /// <summary>
        /// The Roots elsewhere in the project that list this module's sub-contexts, and what to do
        /// about them. Answers false when the reader chose to stop, in which case nothing at all has
        /// been deleted yet - which is why this runs before the deleter rather than inside it.
        ///
        /// Three answers, because there are three honest ones. Take them all out; go through them
        /// one at a time; or take none out, see where they are, and keep the module. A module
        /// nothing lists asks nothing and goes straight through.
        /// </summary>
        private bool UnwireSubContexts(ModuleEntry module)
        {
            string moduleAssetPath = new ModuleAssetPathResolver().ToAssetPath(module.Path);

            var unwirer = new SubContextUnwirer();
            IReadOnlyList<string> found = unwirer.Report(moduleAssetPath);

            if (found.Count == 0) return true;

            int answer = EditorUtility.DisplayDialogComplex(
                "Sub-contexts elsewhere",
                $"'{module.Name}' is listed as a sub-context on Roots outside it:\n\n"
                + Listed(found)
                + "\n\nLeft alone, those Roots list a context that will not exist.",
                "Remove from all",
                "Cancel, just report",
                "Ask me for each");

            // Cancel is the middle button so that a stray Escape or a closed window lands on the
            // answer that changes nothing, rather than on the one that edits every scene.
            if (answer == 1)
            {
                Report(module, found);

                return false;
            }

            bool askEach = answer == 2;

            IReadOnlyList<SubContextUnwireEVO> outcomes = unwirer.Remove(
                moduleAssetPath, entry => !askEach || Asked(module, entry));

            Announce(module, outcomes);

            return true;
        }

        /// <summary>One entry, one question. Skipping is a real answer and is logged as one.</summary>
        private bool Asked(ModuleEntry module, SubContextUnwireEVO entry)
        {
            return EditorUtility.DisplayDialog(
                "Remove sub-context",
                $"{entry.RootName} in {entry.AssetPath}\nlists {entry.ContextName}, which belongs to "
                + $"'{module.Name}'.\n\nRemove it from this Root?",
                "Remove", "Leave it");
        }

        /// <summary>
        /// What the cancelling answer leaves the reader with: every place to look, on the console
        /// where it can be read at leisure and copied, and the module still where it was.
        /// </summary>
        private void Report(ModuleEntry module, IReadOnlyList<string> found)
        {
            Debug.Log($"<color=cyan>[FlowIoC]</color> '{module.Name}' was not deleted. It is listed as a "
                      + $"sub-context in {found.Count} place(s):\n{string.Join("\n", found)}");

            EditorUtility.DisplayDialog(
                "Nothing deleted",
                $"'{module.Name}' is still here.\n\n{Listed(found)}\n\nThe full list is on the console.",
                "OK");
        }

        /// <summary>
        /// What was done, line by line, on the console and in a dialog. A skipped entry and one
        /// removed from an open scene both say so, because both leave something for the reader.
        /// </summary>
        private void Announce(ModuleEntry module, IReadOnlyList<SubContextUnwireEVO> outcomes)
        {
            if (outcomes.Count == 0) return;

            var lines = new List<string>();

            foreach (SubContextUnwireEVO outcome in outcomes)
                lines.Add(outcome.Line());

            Debug.Log($"<color=cyan>[FlowIoC]</color> Sub-contexts of '{module.Name}':\n"
                      + string.Join("\n", lines));

            bool unsaved = outcomes.Any(o => o.Outcome == SubContextUnwireOutcome.RemovedNotSaved);

            EditorUtility.DisplayDialog(
                "Sub-contexts",
                Listed(lines)
                + (unsaved
                    ? "\n\nAn open scene was changed and not saved. Save it to keep the change, or "
                      + "close without saving to keep the entry."
                    : string.Empty),
                "OK");
        }

        /// <summary>
        /// A dialog is not a report. Past a handful the list stops being readable, and the console
        /// line beside it carries the rest.
        /// </summary>
        private string Listed(IReadOnlyList<string> lines)
        {
            const int shown = 6;

            string list = string.Join("\n", lines.Take(shown));

            return lines.Count > shown ? list + $"\n...and {lines.Count - shown} more" : list;
        }

        private void ScanModules()
        {
            var registry = new ModuleRegistryFactory().FromProject();
            var pathResolver = new ModuleAssetPathResolver();

            _modules = new List<ModuleEntry>();

            foreach (ModuleDescriptorEVO module in TopLevel(registry))
                Add(module, 0, registry, pathResolver);
        }

        /// <summary>
        /// The modules that sit in no other module, in the order the index holds them. Everything
        /// else reaches the list under the one it lives in.
        /// </summary>
        private static IEnumerable<ModuleDescriptorEVO> TopLevel(ModuleRegistry registry) =>
            registry.Modules.Where(module => !registry.AncestorsOf(module).Any());

        /// <summary>
        /// One module and then everything inside it, so the list is already in the order it is
        /// drawn. The entry is added before its children are walked and its Descendants list filled
        /// afterwards, because a struct copied into the list would not see a later addition - the
        /// list is what holds them.
        /// </summary>
        private void Add(
            ModuleDescriptorEVO module, int depth, ModuleRegistry registry, ModuleAssetPathResolver pathResolver)
        {
            string path = pathResolver.ToAbsolutePath(registry.PathOf(module));

            if (string.IsNullOrEmpty(path)) return;

            var descendants = new List<string>();

            _modules.Add(new ModuleEntry
            {
                Name = module.Name,
                Path = path,
                Type = module.Kind.ToString(),
                FolderGuid = module.FolderGuid,
                Depth = depth,
                Descendants = descendants
            });

            int first = _modules.Count;

            foreach (ModuleDescriptorEVO child in ChildrenOf(module, registry))
                Add(child, depth + 1, registry, pathResolver);

            for (int index = first; index < _modules.Count; index++)
                descendants.Add(_modules[index].Name);
        }

        /// <summary>
        /// A module's children in the order a module holds them - its sub modules, then its
        /// screens, then the test module that exercises it - and by name within each kind, so the
        /// list does not reorder itself between two scans of an unchanged project.
        /// </summary>
        private static IEnumerable<ModuleDescriptorEVO> ChildrenOf(
            ModuleDescriptorEVO module, ModuleRegistry registry)
        {
            foreach (ModuleKind kind in new[] {ModuleKind.Sub, ModuleKind.Screen, ModuleKind.Test})
            {
                foreach (ModuleDescriptorEVO child in registry.ChildrenOf(module, kind).OrderBy(c => c.Name))
                    yield return child;
            }
        }
    }
}
#endif