#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using FlowIoC.Editor.Inspector;
using FlowIoC.Editor.Modules;
using UnityEditor;
using UnityEngine;

namespace FlowIoC.Editor.CodeGenerator.Menus.Module
{
    /// <summary>
    /// The list a generator picks a parent module from: the project's modules as a tree, a module
    /// under the module it lives in, drawn the way Module Scanner draws them so the two read as
    /// one thing. Every row is always shown - what the reader is here to see is which modules sit
    /// inside which, and a foldout would hide exactly that behind a click.
    ///
    /// The whole row is the button. A row the rules say cannot host what is being created is
    /// drawn a few tones darker, takes no click and does not light up under the pointer: a
    /// highlight promises a click that does something, and this one would not.
    /// </summary>
    internal class ModulePicker
    {
        private const string MODULE_SUFFIX = "Module";

        /// <summary>
        /// The row over the whole tree: the modules folder itself, which every top level module
        /// hangs from. Its path is what ModuleGenerator compares a pick against to tell a top
        /// level module from a nested one, so it is built the way the generator builds it.
        /// </summary>
        private const string ROOT_LABEL = "Modules";

        /// <summary>
        /// What the modules folder's badge says, where a module's says what its Root roots. The
        /// word Module Scanner puts on the row over every module, in FlowIoC's own colour: the
        /// folder is the project's, not any module's.
        /// </summary>
        private const string ROOT_BADGE = "PROJECT";

        private readonly object _rootKey = new object();
        private const float BADGE_WIDTH = 78f;

        /// <summary>The dot at the row's edge that says this is the pick.</summary>
        private const string PICK_MARK = "●";

        /// <summary>
        /// The column the pick mark sits in, before the tree, on every row alike - so a row without
        /// the mark has no empty slot between its guide line and its name. As narrow as the dot
        /// needs: wider read as a gap on every row that carries no dot.
        /// </summary>
        private const float LEAD_WIDTH = 8f;

        /// <summary>The dot at a size that fits the column: the label glyph is twice what the column has room for.</summary>
        private const int PICK_MARK_SIZE = 7;

        private readonly FlowRowPainter _rows = new FlowRowPainter();
        private readonly FlowTreePainter _tree;
        private readonly ModuleRoleBadge _badge = new ModuleRoleBadge();
        private readonly FlowPalette _palette = new FlowPalette();
        private readonly ModuleTreeSearch _search = new ModuleTreeSearch();
        private readonly ModuleTreeBar _bar = new ModuleTreeBar();
        private readonly ModuleTreeScroll _scroll;

        private readonly List<ModuleTreeRowEVO<ModulePickEVO>> _entries;

        private string _searchText = string.Empty;

        private GUIStyle _mark;

        /// <summary>
        /// What kind of module the pick is - what decides the folder layout the file is written
        /// into, and whether it is wrapped in UNITY_EDITOR. Main while the pick is the modules
        /// folder or nothing yet. Read back after Draw, which is where the pick is known.
        /// </summary>
        internal ModuleKind PickedKind { get; private set; } = ModuleKind.Main;

        /// <summary>See ROOT_LABEL. Read on use: Application.dataPath is not there when the fields are.</summary>
        private string RootPath => System.IO.Path.Combine(Application.dataPath, ROOT_LABEL);

        /// <summary>
        /// <paramref name="capped"/> puts the tree in a scroll of its own past a dozen rows. Off for
        /// the one window that draws the picker inside a box of its own height already - Create
        /// Module, whose panel stays level with the folder preview beside it.
        /// </summary>
        internal ModulePicker(ModuleRegistry registry, bool capped = true)
        {
            _tree = new FlowTreePainter(_rows, LEAD_WIDTH);
            _entries = new ModuleTree().Build(new ModulePickFactory().From(registry));
            _scroll = capped ? new ModuleTreeScroll() : null;
        }

        /// <summary>
        /// Draws the tree and takes the click. The pick lands in the two strings the generators
        /// already work from: the module folder, and the module's name without its suffix.
        /// </summary>
        /// <param name="parentModulePath">The folder of the pick, in the shape ModulePickEVO.Path describes.</param>
        /// <param name="selectedModuleName">The pick's name without its Module suffix; empty for the root.</param>
        /// <param name="canHost">Whether a module of this kind may hold what is being created.</param>
        /// <param name="rootSelectable">
        /// Whether the modules folder itself, the row the whole tree hangs from, is a pick. It is
        /// for the one generator that can put what it writes there - a top level module - and for
        /// the rest the row is there to be hung from and nothing else.
        /// </param>
        internal void Draw(
            ref string parentModulePath, ref string selectedModuleName, Func<ModuleKind, bool> canHost,
            bool rootSelectable)
        {
            List<ModuleTreeRowEVO<ModulePickEVO>> shown = _search.Filter(_entries, _searchText);

            _scroll?.Begin(1 + shown.Count);
            _tree.Begin();

            PickedKind = ModuleKind.Main;

            Rect rootRect = DrawRow(ROOT_LABEL, null, 0, rootSelectable, parentModulePath == RootPath, out bool rootPicked);

            _tree.Hang(_rootKey, rootRect, 0, null);

            if (rootPicked)
            {
                parentModulePath = RootPath;
                selectedModuleName = string.Empty;
            }

            foreach (ModuleTreeRowEVO<ModulePickEVO> entry in shown)
            {
                ModulePickEVO pick = entry.Row;
                int depth = entry.Depth + 1;

                bool selected = parentModulePath == pick.Path;

                if (selected) PickedKind = pick.Kind;

                Rect rect = DrawRow(pick.Name, pick, depth, canHost(pick.Kind), selected, out bool picked);

                _tree.Hang(entry, rect, depth, entry.Parent ?? _rootKey);

                if (!picked) continue;

                parentModulePath = pick.Path;
                selectedModuleName = TrimModuleSuffix(pick.Name);
                PickedKind = pick.Kind;
            }

            _scroll?.End();
        }

        /// <summary>
        /// One row: the pick mark at the edge, the name, and what kind of module it is. The tint is the
        /// window's own chrome rather than a status colour - nothing here is right or wrong, one
        /// row is simply the pick - and the pick is filled hardest so it reads as such from across
        /// the room.
        /// </summary>
        private Rect DrawRow(
            string label, ModulePickEVO pick, int depth, bool selectable, bool selected, out bool picked)
        {
            Rect rect = _rows.RowInset();
            Color accent = _palette.Chrome(EditorGUIUtility.isProSkin);
            bool hovered = selectable && _rows.IsHovered(rect);

            // A row that is not a pick - the modules folder while a module is being made inside one,
            // a module that cannot host what is being made - is shaded rather than blacked out: it
            // keeps its tint and its name at full size and reads as a row to be read, not a gap.
            if (!selectable) _rows.PaintShaded(rect, accent);
            else if (selected) _rows.Paint(rect, accent, FlowRowPainter.HEADING_ALPHA);
            else _rows.Paint(rect, accent, hovered ? FlowRowPainter.FILL_ALPHA : FlowRowPainter.QUIET_ALPHA);

            if (selected)
            {
                Color previous = GUI.color;
                GUI.color = accent;
                GUI.Label(new Rect(_tree.LeadX(rect), rect.y, LEAD_WIDTH, rect.height), PICK_MARK, Mark);
                GUI.color = previous;
            }

            float x = _tree.TextX(rect, depth);

            float room = rect.xMax - BADGE_WIDTH - 10f - x;

            GUI.Label(new Rect(x, rect.y, room, rect.height), label,
                selectable ? _rows.Name(hovered || selected) : _rows.Muted());

            var badge = new Rect(rect.xMax - BADGE_WIDTH - 6f, rect.y, BADGE_WIDTH, rect.height);

            if (pick != null) GUI.Label(badge, _badge.Text(pick), _badge.Style(pick, _rows, hovered));
            else GUI.Label(badge, ROOT_BADGE, _rows.BadgeIn(accent));

            picked = false;

            if (!selectable) return rect;

            EditorGUIUtility.AddCursorRect(rect, MouseCursor.Link);

            // Drawn last and painting nothing, so it takes the click without covering the row.
            picked = GUI.Button(rect, GUIContent.none, GUIStyle.none);

            return rect;
        }

        /// <summary>Built on use: EditorStyles is not loaded when the picker's fields are.</summary>
        private GUIStyle Mark => _mark ??= new GUIStyle(EditorStyles.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = PICK_MARK_SIZE
        };

        /// <summary>
        /// The bar over the list, with the search over the list at its right edge: its label alone
        /// until something is picked, then the pick's folder name after it - on the bar, where the
        /// eye already is, rather than on a line under a list it has to scroll past. What the
        /// search filters is the tree <see cref="Draw"/> paints: a match with everything inside
        /// it, and the modules above a match.
        /// </summary>
        internal void DrawBar(string label, string parentModulePath)
        {
            _searchText = _bar.Draw(label, Pick(parentModulePath), _searchText);
        }

        /// <summary>What the bar says after its label: the pick's folder name, or nothing yet.</summary>
        internal string Title(string label, string parentModulePath) => _bar.Title(label, Pick(parentModulePath));

        private static string Pick(string parentModulePath) =>
            string.IsNullOrEmpty(parentModulePath) ? null : System.IO.Path.GetFileName(parentModulePath);

        private string TrimModuleSuffix(string moduleName)
        {
            if (string.IsNullOrEmpty(moduleName) || !moduleName.EndsWith(MODULE_SUFFIX)) return moduleName;

            return moduleName.Substring(0, moduleName.Length - MODULE_SUFFIX.Length).Trim();
        }
    }
}

#endif