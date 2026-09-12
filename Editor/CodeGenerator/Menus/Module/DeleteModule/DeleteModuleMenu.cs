#if UNITY_EDITOR
using FlowIoC.Editor.Inspector;
using System.Collections.Generic;
using System.Linq;
using FlowIoC.Editor.Modules;
using UnityEditor;
using UnityEngine;

namespace FlowIoC.Editor.CodeGenerator.Menus.Module.DeleteModule
{
    internal class DeleteModuleMenu : EditorWindow
    {
        /// <summary>The Delete button at the end of a row, and the kind badge beside it.</summary>
        private const float BUTTON_WIDTH = 60f;

        private const float BADGE_WIDTH = 78f;

        /// <summary>The row over the whole tree: the modules folder, which every top level module hangs from.</summary>
        private const string ROOT_LABEL = "Modules";

        private const string ROOT_BADGE = "PROJECT";

        /// <summary>What the bar over the tree says. Nothing is picked here - every row has its own Delete.</summary>
        private const string MODULES_LABEL = "Modules:";

        private Vector2 _scrollPosition;
        private string _searchText = "";

        /// <summary>
        /// The rows are always expanded: what the reader is here to see is which modules sit
        /// inside which, and a foldout would hide exactly that behind a click.
        /// </summary>
        private List<ModuleTreeRowEVO<ModulePickEVO>> _modules;

        private readonly FlowHeaderBar _bar = new FlowHeaderBar(new FlowPalette(), new FlowHelpPageMap());
        private readonly CoreModules _coreModules = new CoreModules();
        private readonly FlowRowPainter _rows = new FlowRowPainter();
        private readonly ModuleRoleBadge _badge = new ModuleRoleBadge();
        private readonly FlowPalette _palette = new FlowPalette();
        private readonly ModuleTreeSearch _search = new ModuleTreeSearch();
        private readonly ModuleTreeBar _treeBar = new ModuleTreeBar();

        private readonly object _rootKey = new object();

        private FlowTreePainter _tree;

        private void OnEnable()
        {
            _tree = new FlowTreePainter(_rows);

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

            if (_modules == null || _modules.Count == 0)
            {
                EditorGUILayout.HelpBox("No modules found.", MessageType.Info);
                return;
            }

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            // Inside the scroll, not over it: the bar is the heading of the module list, and a heading
            // that stayed put while its list scrolled away read as a toolbar of the window instead.
            _searchText = _treeBar.Draw(MODULES_LABEL, null, _searchText);

            EditorGUILayout.Space();

            _tree.Begin();

            DrawRootRow();

            // A match brings its whole subtree with it, because that subtree is what deleting it
            // would take, and a module that matches nothing is still drawn while something inside
            // it matches - the one search every module tree runs, so the lists agree.
            foreach (ModuleTreeRowEVO<ModulePickEVO> module in _search.Filter(_modules, _searchText))
                DrawModuleRow(module);

            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// One module, under the module it lives in and hung from it by the same guide line the
        /// other module lists draw. The indent is the whole point of the list: a screen module and
        /// the test module inside it used to be two unrelated rows, so the reader confirming a
        /// deletion could not see from the window what the deletion would actually take.
        ///
        /// The row itself takes no click - only the Delete button does anything - so it does not
        /// light up under the pointer either.
        /// </summary>
        /// <summary>
        /// The modules folder itself, the row every top level module hangs from. Nothing to press
        /// on it: the folder is not a module, and it is not for deleting.
        /// </summary>
        private void DrawRootRow()
        {
            Rect rect = _rows.Row();
            Color accent = _palette.Chrome(EditorGUIUtility.isProSkin);

            _rows.Paint(rect, accent, FlowRowPainter.QUIET_ALPHA);
            _tree.Hang(_rootKey, rect, 0, null);

            GUI.Label(new Rect(_tree.TextX(rect, 0), rect.y, rect.width, rect.height), ROOT_LABEL, _rows.Name(false));

            // The same word Module Scanner and the pickers put on this row, in the badge column the
            // module rows use, so the folder reads as one thing wherever it heads a tree.
            float right = rect.xMax - BUTTON_WIDTH - 12f;

            GUI.Label(new Rect(right - BADGE_WIDTH, rect.y, BADGE_WIDTH, rect.height), ROOT_BADGE, _rows.BadgeIn(accent));
        }

        private void DrawModuleRow(ModuleTreeRowEVO<ModulePickEVO> entry)
        {
            ModulePickEVO module = entry.Row;
            int depth = entry.Depth + 1;
            Rect rect = _rows.Row();

            _rows.Paint(rect, _palette.Chrome(EditorGUIUtility.isProSkin), FlowRowPainter.QUIET_ALPHA);
            _tree.Hang(entry, rect, depth, entry.Parent ?? _rootKey);

            float x = _tree.TextX(rect, depth);
            float right = rect.xMax - BUTTON_WIDTH - 12f;

            GUI.Label(new Rect(x, rect.y, right - BADGE_WIDTH - 6f - x, rect.height), module.Name, _rows.Name(false));

            GUI.Label(new Rect(right - BADGE_WIDTH, rect.y, BADGE_WIDTH, rect.height),
                _badge.Text(module), _badge.Style(module, _rows, false));

            string kept = _coreModules.WhyKept(module.Name);

            if (kept != null) DrawKeptReason(kept, new Rect(x, rect.y, right - BADGE_WIDTH - 6f - x, rect.height));
            else DrawDeleteButton(entry, new Rect(rect.xMax - BUTTON_WIDTH - 6f, rect.y + 1f, BUTTON_WIDTH, rect.height - 2f));
        }

        private void DrawDeleteButton(ModuleTreeRowEVO<ModulePickEVO> entry, Rect rect)
        {
            Color background = GUI.backgroundColor;
            GUI.backgroundColor = new ModulePanelTheme().ActionRemove;

            if (GUI.Button(rect, "Delete", EditorStyles.miniButton)) Delete(entry);

            GUI.backgroundColor = background;
        }

        /// <summary>
        /// What the row says where the button would be, for one of the three modules the project is
        /// built on. The reason goes in rather than a disabled button, because a button that cannot
        /// be pressed says only that something is wrong and leaves the reader to guess what.
        /// </summary>
        private void DrawKeptReason(string reason, Rect rect)
        {
            var style = new GUIStyle(_rows.Mini(false)) {alignment = TextAnchor.MiddleRight};

            GUI.Label(rect, reason, style);
        }

        /// <summary>
        /// The confirmation, and the deletion behind it. What goes with the module is named before
        /// the reader answers: deleting a module deletes its folder, so every module inside it goes
        /// too, and a reader who wanted only the parent can close this and delete those first.
        /// </summary>
        private void Delete(ModuleTreeRowEVO<ModulePickEVO> entry)
        {
            ModulePickEVO module = entry.Row;
            List<string> descendants = entry.Descendants.Select(descendant => descendant.Row.Name).ToList();

            string alsoGoing = descendants.Count == 0
                ? string.Empty
                : $"These modules are inside it and go with it:\n{Listed(descendants)}\n\n";

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
                ModuleDeleter.DeleteModule(module.Name, module.Path, module.Descriptor.FolderGuid);

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
        /// The Roots elsewhere in the project that list this module's sub-contexts, and what to do
        /// about them. Answers false when the reader chose to stop, in which case nothing at all has
        /// been deleted yet - which is why this runs before the deleter rather than inside it.
        ///
        /// Three answers, because there are three honest ones. Take them all out; go through them
        /// one at a time; or take none out, see where they are, and keep the module. A module
        /// nothing lists asks nothing and goes straight through.
        /// </summary>
        private bool UnwireSubContexts(ModulePickEVO module)
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
        private bool Asked(ModulePickEVO module, SubContextUnwireEVO entry)
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
        private void Report(ModulePickEVO module, IReadOnlyList<string> found)
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
        private void Announce(ModulePickEVO module, IReadOnlyList<SubContextUnwireEVO> outcomes)
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
            _modules = new ModuleTree().Build(new ModulePickFactory().From(new ModuleRegistryFactory().FromProject()));
        }
    }
}
#endif