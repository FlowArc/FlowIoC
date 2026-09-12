#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FlowIoC.BaseModule.Attributes;
using FlowIoC.Editor.CodeGenerator.Menus.Module.DeleteModule;
using FlowIoC.Editor.Inspector;
using FlowIoC.Editor.Modules;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FlowIoC.Editor.CodeGenerator.Menus.Module.RenameModule
{
    /// <summary>
    /// Pick a module, type its new name, read what the press will do, press.
    ///
    /// The tree is Delete Module's, with a click on the row as the pick the way ModulePicker takes
    /// it. Under it the name field and the preview: every folder, assembly, settings file, class,
    /// asset and channel the plan will change, each carrier of the name with a tick, and a reason on
    /// every line the plan leaves alone. The plan is rebuilt on every keystroke - it reads folder
    /// listings and a handful of files, not the project - and the scene and prefab scan, which does
    /// read the project, runs when the button is pressed, where the confirmation can say what it
    /// found.
    ///
    /// The tree and the preview scroll; the button does not. It stands at the bottom of the window
    /// the way the other generators' buttons do, so a long preview never pushes it out of reach.
    /// </summary>
    internal class RenameModuleMenu : EditorWindow
    {
        private const string TITLE = "Rename Module";

        /// <summary>What the bar over the tree says, with the pick after it once there is one.</summary>
        private const string MODULE_LABEL = "Module:";

        private const float BADGE_WIDTH = 78f;
        private const float TICK_WIDTH = 18f;
        private const float SUFFIX_WIDTH = 96f;

        /// <summary>The name bar: the icon column the other bars use, and the room the label takes before the field.</summary>
        private const float BAR_ICON_WIDTH = 35f;

        private const float NAME_LABEL_WIDTH = 78f;

        /// <summary>Taller than the tree bar: a field to type in wants more air around it than a label does.</summary>
        private const float NAME_BAR_HEIGHT = 40f;
        private const string NAME_LABEL = "New Name:";

        /// <summary>The yellow triangle, at the bar size and at the button size.</summary>
        private const string WARN_ICON = "console.warnicon";

        private const string WARN_ICON_SMALL = "console.warnicon.sml";

        /// <summary>The row over the whole tree: the modules folder, which every top level module hangs from.</summary>
        private const string ROOT_LABEL = "Modules";

        private const string ROOT_BADGE = "PROJECT";

        /// <summary>The dot at the row's edge that says this is the pick, at the size ModulePicker draws it.</summary>
        private const string PICK_MARK = "●";

        private const float LEAD_WIDTH = 8f;
        private const int PICK_MARK_SIZE = 7;

        /// <summary>
        /// What a preview line cannot use of the window's width: the scroll bar, the box the body
        /// sits in, and the row's own margins. Over-counted a little on purpose - a line that wraps
        /// a word early is read, a line that runs under the scroll bar is not.
        /// </summary>
        private const float LINE_RESERVE = 44f;

        /// <summary>Past this many lines a group folds the rest into one line, the way Delete Module's dialog does.</summary>
        private const int SHOWN = 12;

        private string _searchText = "";
        private string _typedStem = "";

        private List<ModuleTreeRowEVO<ModulePickEVO>> _modules;
        private ModuleTreeRowEVO<ModulePickEVO> _picked;
        private ModuleRenamePlanEVO _plan;
        private ModuleRenamePlan _planner;

        private readonly HashSet<string> _unticked = new HashSet<string>(StringComparer.Ordinal);

        private readonly FlowHeaderBar _bar = new FlowHeaderBar(new FlowPalette(), new FlowHelpPageMap());
        private readonly GeneratorWindowBody _body = new GeneratorWindowBody();
        private readonly CoreModules _coreModules = new CoreModules();
        private readonly FlowRowPainter _rows = new FlowRowPainter();
        private readonly ModuleRoleBadge _badge = new ModuleRoleBadge();
        private readonly FlowPalette _palette = new FlowPalette();
        private readonly ModuleStems _stems = new ModuleStems();
        private readonly ModuleTreeSearch _search = new ModuleTreeSearch();
        private readonly ModuleTreeBar _treeBar = new ModuleTreeBar();
        private readonly ModuleTreeScroll _treeScroll = new ModuleTreeScroll();

        private readonly object _rootKey = new object();

        private FlowTreePainter _tree;
        private GUIStyle _mark;
        private GUIStyle _barTitle;

        [MenuItem("Tools/FlowIoC/" + TITLE, false, -1299)]
        private static void Open()
        {
            var window = GetWindow<RenameModuleMenu>(TITLE);
            window.minSize = new Vector2(560f, 560f);
        }

        private void OnEnable()
        {
            _tree = new FlowTreePainter(_rows, LEAD_WIDTH);
            _planner = new ModuleRenamePlan();

            // Without this the window is sent no MouseMove events at all, and a row would only
            // light up under the pointer when something else happened to repaint it.
            wantsMouseMove = true;

            ScanModules();
        }

        private void OnGUI()
        {
            if (Event.current.type == EventType.MouseMove) Repaint();

            _bar.DrawWindow(TITLE, "FlowIoC",
                "Folder, assemblies, namespaces, settings, channel, and the classes named after it", null, null, TITLE);

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "Pick a module and type its new name. The panel renames the folder, the assemblies and "
                + "every asmdef that references them, the namespaces, the settings files, the Flow Console "
                + "channel, the Root, Context and signal holders named after the module, a screen's prefab "
                + "and address, and the Roots in scenes and prefabs that list its contexts. A module inside "
                + "it that carries its name follows, unless you untick it.\n\n"
                + "What the press will do is listed under the name before you press it, with a reason on "
                + "every line that stays as it is. Classes you named yourself, and the card's own text, "
                + "are not touched.",
                MessageType.Info);

            _body.Begin(this);

            // Inside the scroll, not over it: the bar is the heading of the module list, and a heading
            // that stayed put while its list scrolled away read as a toolbar of the window instead.
            _searchText = _treeBar.Draw(MODULE_LABEL, _picked?.Row.Name, _searchText);

            ModuleRenamePlanEVO plan = null;

            if (_modules == null || _modules.Count == 0)
            {
                EditorGUILayout.HelpBox("No modules found.", MessageType.Info);
            }
            else
            {
                DrawTree();

                if (_picked != null)
                {
                    DrawName();

                    // Held for the rest of the frame: a tick taken off further down clears the field
                    // for the next frame, and everything drawn after it still reads this one.
                    plan = _plan ??= _planner.Build(_picked, _typedStem, name => !_unticked.Contains(name));

                    DrawPreview(plan);
                    DrawWhyOff(plan);
                }
            }

            _body.End();

            DrawButton(plan);
        }

        /// <summary>The same search Delete Module runs: a match brings its subtree, a matched descendant keeps its ancestors.</summary>
        private void DrawTree()
        {
            List<ModuleTreeRowEVO<ModulePickEVO>> shown = _search.Filter(_modules, _searchText);

            _treeScroll.Begin(1 + shown.Count);
            _tree.Begin();

            DrawRootRow();

            foreach (ModuleTreeRowEVO<ModulePickEVO> module in shown)
                DrawModuleRow(module);

            _treeScroll.End();
        }

        private void DrawRootRow()
        {
            Rect rect = _rows.RowInset();
            Color accent = _palette.Chrome(EditorGUIUtility.isProSkin);

            _rows.Paint(rect, accent, FlowRowPainter.QUIET_ALPHA);
            _tree.Hang(_rootKey, rect, 0, null);

            GUI.Label(new Rect(_tree.TextX(rect, 0), rect.y, rect.width, rect.height), ROOT_LABEL, _rows.Name(false));
            GUI.Label(new Rect(rect.xMax - BADGE_WIDTH - 6f, rect.y, BADGE_WIDTH, rect.height), ROOT_BADGE, _rows.BadgeIn(accent));
        }

        /// <summary>
        /// One module, pickable by the whole row. One of the three the project is built on shows why
        /// it cannot be picked where the pick would be, in Delete Module's words, and takes no click.
        /// </summary>
        private void DrawModuleRow(ModuleTreeRowEVO<ModulePickEVO> entry)
        {
            ModulePickEVO module = entry.Row;
            int depth = entry.Depth + 1;
            string kept = _coreModules.WhyKept(module.Name);
            bool selectable = kept == null;
            bool selected = entry == _picked;

            Rect rect = _rows.RowInset();
            Color accent = _palette.Chrome(EditorGUIUtility.isProSkin);
            bool hovered = selectable && _rows.IsHovered(rect);

            if (!selectable) _rows.PaintShaded(rect, accent);
            else if (selected) _rows.Paint(rect, accent, FlowRowPainter.HEADING_ALPHA);
            else _rows.Paint(rect, accent, hovered ? FlowRowPainter.FILL_ALPHA : FlowRowPainter.QUIET_ALPHA);

            _tree.Hang(entry, rect, depth, entry.Parent ?? _rootKey);

            if (selected)
            {
                Color previous = GUI.color;
                GUI.color = accent;
                GUI.Label(new Rect(_tree.LeadX(rect), rect.y, LEAD_WIDTH, rect.height), PICK_MARK, Mark);
                GUI.color = previous;
            }

            float x = _tree.TextX(rect, depth);
            float right = rect.xMax - BADGE_WIDTH - 6f;
            var name = new Rect(x, rect.y, right - 6f - x, rect.height);

            GUI.Label(name, module.Name, selectable ? _rows.Name(hovered || selected) : _rows.Muted());
            GUI.Label(new Rect(right, rect.y, BADGE_WIDTH, rect.height), _badge.Text(module), _badge.Style(module, _rows, hovered));

            if (!selectable)
            {
                GUI.Label(name, kept, new GUIStyle(_rows.Mini(false)) {alignment = TextAnchor.MiddleRight});

                return;
            }

            EditorGUIUtility.AddCursorRect(rect, MouseCursor.Link);

            if (GUI.Button(rect, GUIContent.none, GUIStyle.none)) Pick(entry);
        }

        private void Pick(ModuleTreeRowEVO<ModulePickEVO> entry)
        {
            _picked = entry;
            _typedStem = _stems.TypedStem(entry.Row.Name, entry.Row.Kind);
            _unticked.Clear();
            _plan = null;
            GUI.FocusControl(null);
        }

        /// <summary>The typed stem, with the suffix the kind adds drawn after it so the whole new name is on the line.</summary>
        /// <summary>
        /// The name, on a bar of its own in the green the list bars wear, with the warning sign at
        /// its left: this is the one field on the window that changes the project, and as a plain
        /// field under the tree it was lost between the list and the preview. The field sits on
        /// the bar, the suffix the kind adds after it, so the whole new name is read on one line.
        /// </summary>
        private void DrawName()
        {
            EditorGUILayout.Space(10);

            Color previous = GUI.backgroundColor;
            GUI.backgroundColor = new ModulePanelTheme().HeaderFor(FlowRole.Connector);

            EditorGUILayout.BeginHorizontal(new GUIStyle(EditorStyles.helpBox), GUILayout.Height(NAME_BAR_HEIGHT));

            GUILayout.Label(EditorGUIUtility.IconContent(WARN_ICON), GUILayout.Width(BAR_ICON_WIDTH), GUILayout.Height(NAME_BAR_HEIGHT));
            GUILayout.Label(NAME_LABEL, BarTitle, GUILayout.Width(NAME_LABEL_WIDTH), GUILayout.Height(NAME_BAR_HEIGHT));

            // The field in the editor's own colour, centred in a slot the bar's height, the way the
            // tree bar places its search: laid out, it would sit on the bar's top edge.
            GUI.backgroundColor = previous;

            Rect slot = GUILayoutUtility.GetRect(0f, NAME_BAR_HEIGHT, GUILayout.ExpandWidth(true), GUILayout.Height(NAME_BAR_HEIGHT));
            float height = EditorGUIUtility.singleLineHeight;
            var field = new Rect(slot.x, slot.y + (slot.height - height) * 0.5f, slot.width, height);

            EditorGUI.BeginChangeCheck();
            string typed = EditorGUI.TextField(field, _typedStem);

            if (EditorGUI.EndChangeCheck())
            {
                _typedStem = typed;
                _plan = null;
            }

            GUILayout.Label(_stems.KindSuffix(_picked.Row.Kind), BarTitle, GUILayout.Width(SUFFIX_WIDTH), GUILayout.Height(NAME_BAR_HEIGHT));

            EditorGUILayout.EndHorizontal();

            GUI.backgroundColor = previous;
        }

        private void DrawPreview(ModuleRenamePlanEVO plan)
        {
            ModuleRenameEVO picked = plan.Picked;

            if (picked == null)
            {
                Section("Blocked", plan.Blockers, _rows.Error);

                return;
            }

            Section("Folder", new[] {picked.OldName + " → " + picked.NewName + "   (" + ParentOf(picked) + ")"});
            DrawCarriers(plan);
            Section("Assemblies", plan.Assemblies.Select(a => a.OldName + " → " + a.NewName));
            Section("Settings files", plan.SettingsFiles);
            Section("Namespaces", plan.Modules.Select(m => m.OldNamespace + " → " + m.NewNamespace));
            Section("Classes", plan.Classes.SelectMany(file => file.Identifiers.Select(i => i.Old + " → " + i.New)));
            Section("Assets", plan.Assets.Select(a => Path.GetFileName(a.Path) + " → " + Path.GetFileName(a.NewPath))
                .Concat(plan.ScreenAddresses.Select(s =>
                    "Address " + s.OldAddress + " → " + s.NewAddress + ", group " + s.OldGroup + " → " + s.NewGroup)));
            Section("Flow Console channels", plan.Channels.Select(c => c.OldName + " → " + c.NewName));
            Section("Kept as it is", plan.Kept, null, true);
            Section("Warnings", plan.Warnings, _rows.Warn);
            Section("Blocked", plan.Blockers, _rows.Error);
        }

        /// <summary>Every carrier with its tick. A tick taken off rebuilds the plan, which takes the carrier's children with it.</summary>
        private void DrawCarriers(ModuleRenamePlanEVO plan)
        {
            if (plan.Carriers.Count == 0) return;

            Heading("Modules inside it that carry the name");

            foreach (FollowerRenameEVO carrier in plan.Carriers)
            {
                bool ticked = !_unticked.Contains(carrier.OldName);

                // A carrier under one that keeps its name cannot follow whatever its own tick says,
                // so its tick is drawn off and disabled rather than lying about what would happen.
                bool parentKeeps = !carrier.Follows && ticked;

                string line = carrier.Follows
                    ? carrier.OldName + " → " + carrier.NewName
                    : carrier.OldName + " " + carrier.Reason;

                Rect rect = WrappedRow(line, carrier.Follows ? _rows.NameWrapped(false) : _rows.MutedWrapped(),
                    Change, TICK_WIDTH + 4f, out Rect text);

                using (new EditorGUI.DisabledScope(parentKeeps))
                {
                    bool now = EditorGUI.Toggle(new Rect(text.x - TICK_WIDTH - 4f, rect.y, TICK_WIDTH, FlowRowPainter.ROW_HEIGHT),
                        ticked && !parentKeeps);

                    if (now != ticked && !parentKeeps)
                    {
                        if (now) _unticked.Remove(carrier.OldName);
                        else _unticked.Add(carrier.OldName);

                        _plan = null;
                        Repaint();
                    }
                }
            }
        }

        private void Section(string title, IEnumerable<string> lines, Color? tint = null, bool muted = false)
        {
            List<string> list = lines.ToList();

            if (list.Count == 0) return;

            Heading(title, tint);

            Color accent = tint ?? Change;

            for (var index = 0; index < list.Count && index < SHOWN; index++)
                WrappedRow(list[index], muted ? _rows.MutedWrapped() : _rows.NameWrapped(false), accent, 0f, out _);

            if (list.Count > SHOWN)
                WrappedRow("...and " + (list.Count - SHOWN) + " more", _rows.MutedWrapped(), accent, 0f, out _);
        }

        /// <summary>
        /// One preview line, as tall as its text needs at the window's width. The width is read off
        /// the window rather than the row, because during the Layout pass a row has no width yet
        /// and the two passes have to agree on the height. <paramref name="lead"/> is what sits
        /// before the text on the line - a carrier's tick - and <paramref name="text"/> is where
        /// the text was put, for whoever draws that lead.
        /// </summary>
        private Rect WrappedRow(string line, GUIStyle style, Color accent, float lead, out Rect text)
        {
            var content = new GUIContent(line);
            float offset = _tree.TextX(new Rect(0f, 0f, 0f, 0f), 0) + lead;
            float width = Mathf.Max(40f, position.width - offset - LINE_RESERVE);
            float height = Mathf.Max(FlowRowPainter.ROW_HEIGHT, style.CalcHeight(content, width) + 2f);

            Rect rect = _rows.RowInset(height);
            _rows.Paint(rect, accent, FlowRowPainter.QUIET_ALPHA);

            text = new Rect(rect.x + offset, rect.y + 1f, width, rect.height - 2f);
            GUI.Label(text, content, style);

            return rect;
        }

        private void Heading(string title, Color? tint = null)
        {
            Rect rect = _rows.RowInset();
            Color accent = tint ?? Change;

            _rows.Paint(rect, accent, FlowRowPainter.HEADING_ALPHA);

            float x = _tree.TextX(rect, 0);
            GUI.Label(new Rect(x, rect.y, rect.width - x - 6f, rect.height), title, _rows.Strong(false));
        }

        /// <summary>Why the button is off, when it is, said where the reader is looking: at the end of the preview.</summary>
        private void DrawWhyOff(ModuleRenamePlanEVO plan)
        {
            string why = WhyOff(plan);

            if (why == null) return;

            EditorGUILayout.Space();
            WrappedRow("Rename is off: " + why, _rows.MutedWrapped(), _rows.Error, 0f, out _);
        }

        private string WhyOff(ModuleRenamePlanEVO plan)
        {
            if (plan == null) return "Pick a module.";
            if (EditorApplication.isPlaying) return "Leave play mode first.";
            if (EditorApplication.isCompiling) return "Wait for the compile to finish.";
            if (!plan.CanRun) return "Fix what is listed under Blocked.";

            return null;
        }

        /// <summary>
        /// The action button, at the bottom of the window whatever the preview's length, in the
        /// frame the single-file generators use. Off while there is nothing to run or a blocker.
        /// </summary>
        private void DrawButton(ModuleRenamePlanEVO plan)
        {
            // The warning sign the name bar wears, on the button too: what it does cannot be undone.
            var label = new GUIContent(" Rename", EditorGUIUtility.IconContent(WARN_ICON_SMALL).image);

            if (_body.FooterButton(this, label, WhyOff(plan) != null, new ModulePanelTheme().Action))
                Rename(plan);
        }

        /// <summary>
        /// The scan the preview does not run, and the confirmation it feeds: which scenes and
        /// prefabs list the module's contexts, which of them are open and will be left unsaved, and
        /// that the only way back is this panel.
        /// </summary>
        private void Rename(ModuleRenamePlanEVO plan)
        {
            ModuleRenameEVO picked = plan.Picked;

            IReadOnlyList<string> listed = new ModuleAssetReferences()
                .AssetsPointingIntoOrInside(new ModuleAssetPathResolver().ToAssetPath(picked.OldPath));

            List<string> open = listed
                .Where(path => path.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
                .Where(path => SceneManager.GetSceneByPath(path).isLoaded)
                .ToList();

            string message =
                "Rename '" + picked.OldName + "' to '" + picked.NewName + "'?\n\n"
                + plan.Modules.Count + " module(s), " + plan.Assemblies.Count + " assembly definition(s), "
                + plan.Classes.Count + " class file(s), " + plan.Assets.Count + " asset(s).\n"
                + (listed.Count == 0
                    ? "No scene or prefab lists its contexts.\n"
                    : listed.Count + " scene(s) and prefab(s) list its contexts and are written:\n" + Listed(listed) + "\n")
                + (open.Count == 0 ? string.Empty : "\nOpen now, changed and left unsaved:\n" + Listed(open) + "\n")
                + "\nThis cannot be undone. Renaming it back through this panel restores everything.";

            if (!EditorUtility.DisplayDialog(TITLE, message, "Rename", "Cancel")) return;

            ModuleRenameReportEVO report = new ModuleRenamer().Run(plan);

            string unsaved = report.UnsavedScenes.Count == 0
                ? string.Empty
                : "\n\nAn open scene was changed and not saved:\n" + Listed(report.UnsavedScenes)
                                                                   + "\nSave it to keep the change, or close it without saving and open its Root once - the inspector repairs the entry from the script.";

            EditorUtility.DisplayDialog(
                report.Failure == null ? "Module Renamed" : "Rename stopped",
                (report.Failure == null
                    ? "'" + picked.OldName + "' is now '" + picked.NewName + "' at " + report.ModuleAssetPath + ".\n\n"
                    : report.Failure + "\n\n")
                + Listed(report.Lines) + unsaved + "\n\nThe full list is on the console.",
                "OK");

            _picked = null;
            _plan = null;
            ScanModules();
            GUIUtility.ExitGUI();
        }

        private string Listed(IReadOnlyList<string> lines)
        {
            const int shown = 6;

            string list = string.Join("\n", lines.Take(shown));

            return lines.Count > shown ? list + "\n...and " + (lines.Count - shown) + " more" : list;
        }

        private static string ParentOf(ModuleRenameEVO module) =>
            Path.GetFileName(Path.GetDirectoryName(module.OldPath) ?? string.Empty);

        /// <summary>Built on use: EditorStyles is not loaded when the fields are.</summary>
        private GUIStyle Mark => _mark ??= new GUIStyle(EditorStyles.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = PICK_MARK_SIZE
        };

        /// <summary>The bars' title style, as ModuleTreeBar and GeneratorListBar draw theirs.</summary>
        private GUIStyle BarTitle => _barTitle ??= new GUIStyle(EditorStyles.whiteLabel)
        {
            alignment = TextAnchor.MiddleLeft,
            fontSize = 12,
            richText = true
        };

        /// <summary>
        /// The green every line of the preview is tinted with - the Connector green the list bars
        /// wear - so what the press changes reads as one block under the name bar, apart from the
        /// violet of the tree the pick came from.
        /// </summary>
        private Color Change => _palette.Accent(FlowRole.Connector, EditorGUIUtility.isProSkin);

        private void ScanModules()
        {
            _modules = new ModuleTree().Build(new ModulePickFactory().From(new ModuleRegistryFactory().FromProject()));
            _picked = null;
        }
    }
}
#endif