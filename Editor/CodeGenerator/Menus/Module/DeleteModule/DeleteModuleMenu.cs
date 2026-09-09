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

        private struct ModuleEntry
        {
            public string Name;
            public string Path;
            public string Type;
            public string FolderGuid;
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
                "Select a module to delete. This will remove the module folder, " +
                "its assembly definition, namespace settings, log type registration, " +
                "and its entry in the module index.",
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

            bool hasSearch = !string.IsNullOrEmpty(_searchText);

            for (int i = 0; i < _modules.Count; i++)
            {
                var module = _modules[i];
                if (hasSearch && module.Name.IndexOf(_searchText, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                // The same row the other module panels draw: the Root's washed violet behind it,
                // and the kind in the colour that kind wears everywhere else.
                GUI.backgroundColor = new ModulePanelTheme().Row;
                EditorGUILayout.BeginHorizontal("box");
                GUI.backgroundColor = Color.white;

                EditorGUILayout.LabelField(module.Name, EditorStyles.boldLabel, GUILayout.Width(250));
                DrawKindLabel(module.Type);
                GUILayout.FlexibleSpace();

                GUI.backgroundColor = new ModulePanelTheme().ActionRemove;
                if (GUILayout.Button("Delete", GUILayout.Width(60)))
                {
                    if (EditorUtility.DisplayDialog(
                            "Delete Module",
                            $"Are you sure you want to delete '{module.Name}'?\n\n" +
                            $"Path: {module.Path}\n\n" +
                            "This action cannot be undone!",
                            "Delete", "Cancel"))
                    {
                        // Before anything is deleted, so that answering "cancel" here leaves the
                        // module whole: the assemblies, the settings files and the folder are all
                        // still there, and the reader is back where they started.
                        if (!UnwireSubContexts(module))
                        {
                            GUIUtility.ExitGUI();

                            return;
                        }

                        IReadOnlyList<string> deleted =
                            ModuleDeleter.DeleteModule(module.Name, module.Path, module.FolderGuid);

                        // The deleter reports rather than announces, so the summary dialog belongs
                        // here, where there is already a user looking at a window.
                        EditorUtility.DisplayDialog(
                            "Module Deleted",
                            $"'{module.Name}' has been deleted.\n\n{string.Join("\n", deleted)}",
                            "OK");

                        ScanModules();
                        GUIUtility.ExitGUI();
                    }
                }

                GUI.backgroundColor = Color.white;

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
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

            _modules = registry.Modules
                .Select(module => ToEntry(module, registry, pathResolver))
                .Where(entry => !string.IsNullOrEmpty(entry.Path))
                .ToList();
        }

        private static ModuleEntry ToEntry(ModuleDescriptorEVO module, ModuleRegistry registry, ModuleAssetPathResolver pathResolver)
        {
            return new ModuleEntry
            {
                Name = module.Name,
                Path = pathResolver.ToAbsolutePath(registry.PathOf(module)),
                Type = module.Kind.ToString(),
                FolderGuid = module.FolderGuid
            };
        }
    }
}
#endif